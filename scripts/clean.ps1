param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..")
)

Write-Host ""
Write-Host "=== Очистка generated build output ===" -ForegroundColor Cyan
Write-Host ""

$generatedDirectories = @(
    (Join-Path $root "artifacts"),
    (Join-Path $root "output"),
    (Join-Path $root "publish"),
    (Join-Path $root "MovementUnlocker\build")
)

foreach ($directory in $generatedDirectories) {
    if (-not (Test-Path -LiteralPath $directory)) {
        continue
    }

    Write-Host "Удаляю: $directory"

    Remove-Item `
        -LiteralPath $directory `
        -Recurse `
        -Force
}

$distRoot = Join-Path $root "dist"

if (-not [string]::IsNullOrWhiteSpace($Configuration)) {
    $configurationDist = Join-Path `
        $distRoot `
        $Configuration

    if (Test-Path -LiteralPath $configurationDist) {
        Write-Host "Удаляю: $configurationDist"

        Remove-Item `
            -LiteralPath $configurationDist `
            -Recurse `
            -Force
    }
}
else {
    if (Test-Path -LiteralPath $distRoot) {
        Write-Host "Удаляю: $distRoot"

        Remove-Item `
            -LiteralPath $distRoot `
            -Recurse `
            -Force
    }
}

# В старой системе DLL могли физически оставаться
# в исходной директории resources/exports.
$legacyExports = Join-Path `
    $root `
    "ZombiePlague.Core\resources\exports"

if (Test-Path -LiteralPath $legacyExports) {
    Get-ChildItem `
        -LiteralPath $legacyExports `
        -File `
        -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Extension -eq ".dll" -or
            $_.Extension -eq ".pdb" -or
            $_.Name.EndsWith(
                ".deps.json",
                [System.StringComparison]::OrdinalIgnoreCase
            ) -or
            $_.Name.EndsWith(
                ".runtimeconfig.json",
                [System.StringComparison]::OrdinalIgnoreCase
            )
        } |
        ForEach-Object {
            Write-Host "Удаляю старый export: $($_.Name)"

            Remove-Item `
                -LiteralPath $_.FullName `
                -Force
        }
}

Write-Host ""
Write-Host "Очистка завершена." -ForegroundColor Green