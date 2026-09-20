param(
    [Parameter(Mandatory = $true)][string]$Cs2Path,
    [string]$Addon = 'elysiumhud'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($Addon -notmatch '^[a-z0-9_]+$') { throw 'Имя addon должно содержать только a-z, 0-9 и _' }
$compiler = Join-Path $Cs2Path 'game/bin/win64/resourcecompiler.exe'
$source = Join-Path $Cs2Path "content/csgo_addons/$Addon"
$compiled = Join-Path $Cs2Path "game/csgo_addons/$Addon"
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Не найден resourcecompiler.exe из CS2 Workshop Tools' }
if (-not (Test-Path -LiteralPath $source)) { throw 'Сначала создайте addon в CS2 Workshop Tools' }
$resource = Join-Path $PSScriptRoot '../CustomKnife.Core/resources/hud/knife-selector/content/panorama'
Copy-Item -LiteralPath $resource -Destination $source -Recurse -Force
$images = Join-Path $source 'panorama/images/custom_game/elysium/knives'
if (Test-Path -LiteralPath $images) {
    foreach ($image in Get-ChildItem -LiteralPath $images -Recurse -File | Where-Object { $_.Extension -in '.png', '.svg' }) {
        & $compiler -i $image.FullName -r
        if ($LASTEXITCODE -ne 0) { throw "Ошибка компиляции изображения: $($image.Name)" }
    }
}
$inputs = @(
    @('panorama/styles/custom_game/elysium_knife_images_v4.css', 'panorama/styles/custom_game/elysium_knife_images_v4.vcss_c'),
    @('panorama/styles/custom_game/elysium_knife_selector_v4.css', 'panorama/styles/custom_game/elysium_knife_selector_v4.vcss_c'),
    @('panorama/layout/custom_game/elysium_knife_selector_v4.xml', 'panorama/layout/custom_game/elysium_knife_selector_v4.vxml_c')
)
foreach ($pair in $inputs) {
    $output = Join-Path $compiled $pair[1]
    # Старый скомпилированный файл не должен маскировать ошибку новой сборки.
    if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output }
    $messages = & $compiler -i (Join-Path $source $pair[0]) -r 2>&1
    $messages | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $output)) {
        throw "Не скомпилирован ресурс: $($pair[0])"
    }
    if (($messages -join "`n") -match '(?im)(unknown|unsupported|unrecognized|invalid)\s+(css\s+)?(property|attribute|panel|selector)|parse\s+error') {
        throw "Компилятор сообщил о неподдерживаемом синтаксисе: $($pair[0])"
    }
}
Write-Host 'Knife HUD v4 скомпилирован. Проверьте интерфейс в игре и включите эти ресурсы в VPK.'
