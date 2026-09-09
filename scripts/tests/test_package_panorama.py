"""Проверки упаковки Panorama без .NET, Steam и сторонних Python-пакетов."""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
from zipfile import ZipFile

SCRIPT = Path(__file__).resolve().parents[1] / "package-panorama.py"
SPEC = importlib.util.spec_from_file_location("package_panorama", SCRIPT)
packager = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(packager)


class PanoramaPackagingTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)

    def write(self, path, data=b"resource"):
        target = self.root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)
        return target

    def package(self, configuration="Release"):
        return packager.package_panorama(self.root, configuration)

    def test_merges_roots_and_preserves_nested_paths_and_binary_bytes(self):
        inputs = {
            "Shop.Core/resources/hud/shop/content/panorama/layout/custom_game/shop.xml": b"<root/>",
            "Other.Core/resources/content/panorama/styles/custom_game/hud.css": b".Hud {}",
            "FutureModule/panorama/images/icons/custom.vsvg_c": b"\x00\xff\x01",
            "FutureModule/panorama/scripts/custom.js": b"script",
        }
        for path, data in inputs.items():
            self.write(path, data)
        self.write("Shop.Core/resources/hud/shop/README.md")
        self.write("Shop.Core/Shop.Core.dll")
        output, manifest = self.package()
        self.assertEqual(manifest["fileCount"], 4)
        self.assertEqual(len(manifest["sourceRoots"]), 3)
        with ZipFile(output) as archive:
            self.assertEqual(len(archive.namelist()), 5)
            self.assertIsNone(archive.testzip())
            for path, data in inputs.items():
                self.assertEqual(archive.read("panorama/" + path.split("/panorama/", 1)[1]), data)
            self.assertEqual(json.loads(archive.read("panorama-manifest.json")), manifest)
        self.assertFalse(manifest["compiledByPackager"])

    def test_identical_duplicates_are_deduplicated_with_all_origins(self):
        for prefix in ("A", "B"):
            self.write(f"{prefix}/panorama/images/icon.svg", b"same")
        output, manifest = self.package()
        self.assertEqual(manifest["fileCount"], 1)
        self.assertEqual(len(manifest["files"][0]["sources"]), 2)
        self.assertEqual(manifest["totalBytes"], 4)
        self.assertEqual(manifest["files"][0]["sha256"], hashlib.sha256(b"same").hexdigest())
        with ZipFile(output) as archive:
            self.assertEqual(archive.namelist().count("panorama/images/icon.svg"), 1)

    def test_conflicting_duplicates_fail_and_remove_stale_output(self):
        self.write("A/panorama/icon.svg", b"one")
        output, _ = self.package()
        self.write("B/panorama/icon.svg", b"two")
        with self.assertRaisesRegex(ValueError, "Разное содержимое"):
            self.package()
        self.assertFalse(output.exists())
        self.assertEqual(list(output.parent.iterdir()), [])

    def test_case_only_file_collisions_fail(self):
        self.write("A/panorama/Icon.svg")
        self.write("B/panorama/icon.svg")
        with self.assertRaisesRegex(ValueError, "Конфликт регистра"):
            self.package()

    def test_case_only_directory_collisions_fail(self):
        self.write("A/panorama/Images/a.svg")
        self.write("B/panorama/images/b.svg")
        with self.assertRaisesRegex(ValueError, "Конфликт регистра"):
            self.package()

    def test_file_directory_collisions_fail(self):
        self.write("A/panorama/images")
        self.write("B/panorama/images/icon.svg")
        with self.assertRaisesRegex(ValueError, "Конфликт файла и каталога"):
            self.package()

    def test_ignores_build_outputs_and_dependency_directories(self):
        self.write("A/panorama/hud.xml", b"original")
        for name in packager.EXCLUDED_DIRECTORIES:
            self.write(f"{name}/copied/panorama/hud.xml", b"stale")
            self.write(f"Plugin/{name}/panorama/hud.xml", b"stale")
        _, manifest = self.package()
        self.assertEqual(manifest["fileCount"], 1)
        self.assertEqual(manifest["sourceRoots"], ["A/panorama"])

    def test_exclusions_do_not_drop_files_inside_panorama(self):
        self.write("A/panorama/bin/custom.dat")
        self.write("A/panorama/images/.hidden.svg")
        _, manifest = self.package()
        self.assertEqual(manifest["fileCount"], 2)

    def test_nested_panorama_is_not_discovered_twice(self):
        self.write("A/panorama/nested/panorama/image.svg")
        _, manifest = self.package()
        self.assertEqual(manifest["sourceRoots"], ["A/panorama"])
        self.assertEqual(manifest["files"][0]["path"], "panorama/nested/panorama/image.svg")

    def test_empty_repository_fails(self):
        with self.assertRaisesRegex(ValueError, "Не найдено файлов"):
            self.package()

    def test_empty_panorama_fails(self):
        (self.root / "A/panorama").mkdir(parents=True)
        with self.assertRaisesRegex(ValueError, "Не найдено файлов"):
            self.package()

    def test_repeated_packaging_is_reproducible_and_drops_deleted_files(self):
        source = self.write("A/panorama/hud.xml", b"unchanged")
        obsolete = self.write("B/panorama/obsolete.xml")
        output, _ = self.package()
        first = output.read_bytes()
        os.utime(source, (1800000000, 1800000000))
        self.package()
        self.assertEqual(first, output.read_bytes())
        obsolete.unlink()
        self.package()
        with ZipFile(output) as archive:
            self.assertNotIn("panorama/obsolete.xml", archive.namelist())
        self.assertEqual(source.read_bytes(), b"unchanged")

    def symlink(self, target, link, is_directory=False):
        link.parent.mkdir(parents=True, exist_ok=True)
        try:
            link.symlink_to(target, target_is_directory=is_directory)
        except OSError as error:
            self.skipTest(f"Symlinks unavailable: {error}")

    def test_symbolic_resource_file_is_rejected(self):
        target = self.write("outside/private.txt")
        self.symlink(target, self.root / "A/panorama/leak.txt")
        with self.assertRaisesRegex(ValueError, "Символическая ссылка"):
            self.package()

    def test_symbolic_resource_directory_is_rejected(self):
        target = self.write("outside/private.txt").parent
        self.symlink(target, self.root / "A/panorama/images", True)
        with self.assertRaisesRegex(ValueError, "Символическая ссылка"):
            self.package()

    def test_symbolic_panorama_root_is_rejected(self):
        target = self.write("outside/private.txt").parent
        self.symlink(target, self.root / "A/panorama", True)
        with self.assertRaisesRegex(ValueError, "Символическая ссылка"):
            self.package()

    def test_debug_configuration_uses_separate_output(self):
        self.write("A/panorama/hud.xml")
        output, manifest = self.package("Debug")
        self.assertEqual(output, self.root / "dist/Debug/panorama/Elysium.Panorama.Debug.zip")
        self.assertEqual(manifest["configuration"], "Debug")

    def test_invalid_configuration_is_rejected(self):
        with self.assertRaisesRegex(ValueError, "Неизвестная конфигурация"):
            self.package("../escape")

    def test_read_error_removes_partial_archive(self):
        self.write("A/panorama/hud.xml")
        with patch.object(Path, "read_bytes", side_effect=OSError("read failed")):
            with self.assertRaisesRegex(OSError, "read failed"):
                self.package()
        self.assertEqual(list((self.root / "dist/Release/panorama").iterdir()), [])

    def test_cli_reports_failure_and_success_with_summary(self):
        env = dict(os.environ, GITHUB_STEP_SUMMARY=str(self.root / "summary.md"))
        command = [sys.executable, str(SCRIPT), "--root", str(self.root)]
        result = subprocess.run(command, capture_output=True, text=True, env=env)
        self.assertEqual(result.returncode, 1)
        self.assertIn("Panorama packaging failed", result.stderr)
        self.write("A/panorama/hud.xml")
        result = subprocess.run(command, capture_output=True, text=True, env=env)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("Elysium.Panorama.Release.zip", result.stdout)
        self.assertIn("Resource files: 1", (self.root / "summary.md").read_text())


if __name__ == "__main__":
    unittest.main()
