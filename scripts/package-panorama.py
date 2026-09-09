#!/usr/bin/env python3
"""Собирает ресурсы всех исходных папок panorama в отдельный ZIP без компиляции Valve."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import sys
import tempfile
from typing import Any
from zipfile import ZIP_DEFLATED, ZipFile, ZipInfo

# Исключения действуют только при поиске корней, а не внутри самих ресурсов
EXCLUDED_DIRECTORIES = {
    ".git", ".vs", ".idea", ".venv", "venv", "__pycache__", ".pytest_cache",
    "node_modules", "bin", "obj", "artifacts", "dist", "output", "testresults",
}


def fail_walk(error: OSError) -> None:
    """Не позволяет упаковать неполный набор при ошибке чтения каталога."""
    raise error


def find_panorama_roots(root: Path) -> list[Path]:
    """Находит panorama вне каталогов сборки, не обходя символические ссылки."""
    result: list[Path] = []
    for directory, names, _ in os.walk(root, onerror=fail_walk, followlinks=False):
        current = Path(directory)
        if current.name == "panorama":
            result.append(current)
            names[:] = []
            continue
        retained = []
        for name in sorted(names):
            child = current / name
            if name.casefold() in EXCLUDED_DIRECTORIES:
                continue
            if child.is_symlink():
                if name == "panorama":
                    raise ValueError(f"Символическая ссылка вместо panorama: {child}")
                continue
            retained.append(name)
        names[:] = retained
    return sorted(result)


def collect_resources(root: Path) -> tuple[list[Path], dict[str, list[Path]]]:
    """Объединяет относительные пути и запрещает неоднозначный регистр имён."""
    roots = find_panorama_roots(root)
    resources: dict[str, list[Path]] = {}
    canonical_paths: dict[str, str] = {}
    for panorama in roots:
        for directory, names, files in os.walk(
            panorama, onerror=fail_walk, followlinks=False
        ):
            names.sort()
            current = Path(directory)
            for name in names + files:
                if (current / name).is_symlink():
                    raise ValueError(f"Символическая ссылка в ресурсах: {current / name}")
            for name in sorted(files):
                source = current / name
                if not source.is_file():
                    raise ValueError(f"Ресурс не является обычным файлом: {source}")
                relative = source.relative_to(panorama)
                # Обратный слеш в имени на Linux меняет смысл пути при распаковке на Windows
                if "\\" in relative.as_posix():
                    raise ValueError(f"Недопустимый путь ресурса: {source}")
                destination = (Path("panorama") / relative).as_posix()
                parts = destination.split("/")
                for count in range(1, len(parts) + 1):
                    prefix = "/".join(parts[:count])
                    canonical = canonical_paths.setdefault(prefix.casefold(), prefix)
                    if canonical != prefix:
                        raise ValueError(f"Конфликт регистра путей: {canonical} / {prefix}")
                resources.setdefault(destination, []).append(source)
    if not resources:
        raise ValueError("Не найдено файлов в исходных папках panorama")
    # Один корень может содержать файл, другой — каталог с тем же путём
    for destination in resources:
        for parent in Path(destination).parents:
            if parent.as_posix() in resources:
                raise ValueError(f"Конфликт файла и каталога: {parent}")
    return roots, dict(sorted(resources.items()))


def zip_entry(path: str) -> ZipInfo:
    """Фиксирует время и права, чтобы ZIP не зависел от времени checkout и ОС."""
    entry = ZipInfo(path, date_time=(1980, 1, 1, 0, 0, 0))
    entry.compress_type = ZIP_DEFLATED
    entry.create_system = 3
    entry.external_attr = 0o100644 << 16
    return entry


def package_panorama(root: Path, configuration: str = "Release") -> tuple[Path, dict[str, Any]]:
    """Создаёт архив только при полном успешном чтении и проверке всех ресурсов."""
    if configuration not in {"Debug", "Release"}:
        raise ValueError(f"Неизвестная конфигурация: {configuration}")
    root = root.resolve(strict=True)
    if not root.is_dir():
        raise ValueError(f"Корень репозитория не является каталогом: {root}")
    output = root / "dist" / configuration / "panorama" / f"Elysium.Panorama.{configuration}.zip"
    output.parent.mkdir(parents=True, exist_ok=True)
    # После неудачной повторной упаковки не должен оставаться старый успешный архив
    output.unlink(missing_ok=True)
    roots, resources = collect_resources(root)
    manifest: dict[str, Any] = {
        "schemaVersion": 1,
        "configuration": configuration,
        "compiledByPackager": False,
        "sourceRoots": [path.relative_to(root).as_posix() for path in roots],
        "fileCount": len(resources),
        "totalBytes": 0,
        "files": [],
    }
    with tempfile.NamedTemporaryFile(dir=output.parent, suffix=".tmp", delete=False) as temp:
        temporary = Path(temp.name)
    try:
        with ZipFile(temporary, "w", compression=ZIP_DEFLATED) as archive:
            for destination, sources in resources.items():
                data = sources[0].read_bytes()
                for duplicate in sources[1:]:
                    if duplicate.read_bytes() != data:
                        origins = ", ".join(path.relative_to(root).as_posix() for path in sources)
                        raise ValueError(f"Разное содержимое для {destination}: {origins}")
                archive.writestr(zip_entry(destination), data)
                manifest["totalBytes"] += len(data)
                manifest["files"].append({
                    "path": destination,
                    "bytes": len(data),
                    "sha256": hashlib.sha256(data).hexdigest(),
                    "sources": [path.relative_to(root).as_posix() for path in sources],
                })
            archive.writestr(
                zip_entry("panorama-manifest.json"),
                json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
            )
        with ZipFile(temporary) as archive:
            invalid = archive.testzip()
            if invalid is not None:
                raise ValueError(f"Ошибка проверки ZIP: {invalid}")
        temporary.replace(output)
    finally:
        temporary.unlink(missing_ok=True)
    return output, manifest


def main() -> int:
    """Запускает упаковку и публикует статистику в сводку GitHub Actions."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--configuration", choices=("Debug", "Release"), default="Release")
    args = parser.parse_args()
    try:
        output, manifest = package_panorama(args.root, args.configuration)
        summary = (
            f"## Panorama {args.configuration}\n\n"
            f"- Archive: `{output.name}`\n"
            f"- Panorama roots: {len(manifest['sourceRoots'])}\n"
            f"- Resource files: {manifest['fileCount']}\n"
            f"- Uncompressed size: {manifest['totalBytes']} bytes\n"
            "- Packaging only; Valve resources are not compiled\n\n"
        )
        print(summary, end="")
        print(output)
        if summary_path := os.environ.get("GITHUB_STEP_SUMMARY"):
            with Path(summary_path).open("a", encoding="utf-8") as summary_file:
                summary_file.write(summary)
        return 0
    except (OSError, ValueError) as error:
        print(f"Panorama packaging failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
