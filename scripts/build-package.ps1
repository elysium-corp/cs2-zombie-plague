param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..")
)

$solutionPath = Join-Path `
    $root `
    "CS2ZombiePlague.sln"

$cleanScript = Join-Path `
    $PSScriptRoot `
    "clean.ps1"

$packageScript = Join-Path `
    $PSScriptRoot `
    "package.ps1"

if (-not (Test-Path -LiteralPath $solutionPath)) {
    throw "Solution не найдена: $solutionPath"
}

if (-not (Test-Path -LiteralPath $cleanScript)) {
    throw "clean.ps1 не найден: $cleanScript"
}

if (-not (Test-Path -LiteralPath $packageScript)) {
    throw "package.ps1 не найден: $packageScript"
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Elysium CS2 Zombie Plague" -ForegroundColor Cyan
Write-Host " Full Package $Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Push-Location $root

try {
    Write-Host "[1/4] Clean..." -ForegroundColor Yellow

    & $cleanScript `
        -Configuration $Configuration

    if (-not $?) {
        throw "Очистка завершилась с ошибкой."
    }

    Write-Host ""
    Write-Host "[2/4] Restore..." -ForegroundColor Yellow

    & dotnet restore $solutionPath

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore завершился с ошибкой."
    }

    Write-Host ""
    Write-Host "[3/4] Build $Configuration..." -ForegroundColor Yellow

    & dotnet build `
        $solutionPath `
        --configuration $Configuration `
        --no-restore

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build завершился с ошибкой."
    }

    Write-Host ""
    Write-Host "[4/4] Package $Configuration..." -ForegroundColor Yellow

    & $packageScript `
        -Configuration $Configuration

    if (-not $?) {
        throw "package.ps1 завершился с ошибкой."
    }

    $configurationRoot = Join-Path `
        (Join-Path $root "dist") `
        $Configuration

    $pluginsRoot = Join-Path `
        $configurationRoot `
        "plugins"

    $packagesRoot = Join-Path `
        $configurationRoot `
        "packages"

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host " Full Package $Configuration готов" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green

    Write-Host ""
    Write-Host "Папки для сервера:" -ForegroundColor Cyan
    Write-Host $pluginsRoot

    Write-Host ""
    Write-Host "ZIP:" -ForegroundColor Cyan
    Write-Host $packagesRoot

    Write-Host ""

    if (Test-Path -LiteralPath $packagesRoot) {
        Get-ChildItem `
            -LiteralPath $packagesRoot `
            -Filter "*.zip" |
            Sort-Object Name |
            ForEach-Object {
                $sizeMb = [Math]::Round(
                    $_.Length / 1MB,
                    2
                )

                Write-Host (
                    "  {0,-45} {1,8} MB" -f
                    $_.Name,
                    $sizeMb
                )
            }
    }
}
finally {
    Pop-Location
}