param(
    [Parameter(Mandatory = $true)][string]$Cs2Path,
    [string]$Addon = 'elysiumhud',
    [string]$CmsExportPath
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
$cmsStylePath = Join-Path $source 'panorama/styles/custom_game/elysium_knife_cms_v4.css'
$existingCmsCss = if (Test-Path -LiteralPath $cmsStylePath) { [System.IO.File]::ReadAllBytes($cmsStylePath) } else { $null }
Copy-Item -LiteralPath $resource -Destination $source -Recurse -Force
# Обычная пересборка HUD сохраняет ранее импортированное оформление CMS.
if (-not $CmsExportPath -and $null -ne $existingCmsCss) { [System.IO.File]::WriteAllBytes($cmsStylePath, $existingCmsCss) }
$images = @()
$builtInImages = Join-Path $source 'panorama/images/custom_game/elysium/knives'
if (Test-Path -LiteralPath $builtInImages) {
    $images += @(Get-ChildItem -LiteralPath $builtInImages -Recurse -File | Where-Object { $_.Extension -in '.png', '.svg' })
}
if ($CmsExportPath) {
    $manifestPath = Join-Path $CmsExportPath 'knife-hud-assets.json'
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    foreach ($path in @($manifest.icons) + @($manifest.previews)) {
        if ($path -cnotmatch '^panorama/images/(?:[a-z0-9_-]+/)*[a-z0-9_-]+(?:\.vsvg|_png\.vtex)$') {
            throw 'Некорректный путь в экспорте Flute CMS'
        }
        $relative = if ($path.EndsWith('.vsvg')) { $path.Substring(0, $path.Length - 5) + '.svg' } else { $path.Substring(0, $path.Length - 9) + '.png' }
        $assetInputPath = Join-Path $CmsExportPath ('content/' + $relative)
        $destination = Join-Path $source $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath $assetInputPath -Destination $destination -Force
        $images += Get-Item -LiteralPath $destination
    }
    Copy-Item -LiteralPath (Join-Path $CmsExportPath 'content/panorama/styles/custom_game/elysium_knife_cms_v4.css') -Destination (Join-Path $source 'panorama/styles/custom_game/elysium_knife_cms_v4.css') -Force
}
foreach ($image in $images | Sort-Object -Property FullName -Unique) {
        $relative = [System.IO.Path]::GetRelativePath($source, $image.FullName)
        $extension = if ($image.Extension -eq '.png') { '_png.vtex_c' } else { '.vsvg_c' }
        $output = Join-Path $compiled ($relative.Substring(0, $relative.Length - $image.Extension.Length) + $extension)
        if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output }
        & $compiler -i $image.FullName -r
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $output)) { throw "Не скомпилировано изображение: $($image.Name)" }
}
$inputs = @(
    @('panorama/styles/custom_game/elysium_knife_cms_v4.css', 'panorama/styles/custom_game/elysium_knife_cms_v4.vcss_c'),
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
