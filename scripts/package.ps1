param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string[]]$Plugins = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..")
)

$solutionPath = Join-Path `
    $root `
    "CS2ZombiePlague.sln"

$distRoot = Join-Path `
    $root `
    "dist"

$configurationRoot = Join-Path `
    $distRoot `
    $Configuration

$pluginsRoot = Join-Path `
    $configurationRoot `
    "plugins"

$packagesRoot = Join-Path `
    $configurationRoot `
    "packages"

$buildMetadataRoot = Join-Path `
    (Join-Path (Join-Path $root "artifacts") "metadata") `
    $Configuration

$runtimePolicyPath = Join-Path `
    (Join-Path $root "eng") `
    "runtime-package-policy.json"

$runtimeManifestPath = Join-Path `
    $configurationRoot `
    "runtime-manifest.json"

if (-not (Test-Path -LiteralPath $solutionPath)) {
    throw "Solution не найдена: $solutionPath"
}

if (-not (Test-Path -LiteralPath $runtimePolicyPath)) {
    throw "Runtime policy не найдена: $runtimePolicyPath"
}

$runtimePolicy = Get-Content `
    -LiteralPath $runtimePolicyPath `
    -Raw |
    ConvertFrom-Json

# ============================================================
# Runtime architecture
# ============================================================
#
# Shared Common:
#
#   ZombiePlague.Core/resources/exports/
#       Common.Di.dll
#       Common.Effects.dll
#       Common.Hooks.dll
#       Common.Math.dll
#
# Private Common:
#
#   Common.Database.dll
#
# должен находиться рядом с каждым Core, который его использует.
# Он НЕ является Swiftly export, потому что имеет собственный
# runtime dependency graph: EF Core, Npgsql и т.д.
#
# API:
#
#   Xxx.Core/resources/exports/Xxx.Api.dll
#
# API должен существовать в runtime РОВНО В ОДНОМ экземпляре,
# иначе разные Plugin AssemblyLoadContext получат разные Type
# identity для одного интерфейса.
# ============================================================

$privateCommonProjectNames = @(
    "Common.Database"
)

# Некоторые API являются контрактами, которые должны существовать
# даже при отсутствии плагина-поставщика.
#
# ZombiePlague.Core всегда является центральным runtime-плагином,
# поэтому Metrics.Api размещаем вместе с ним.
$apiHostOverrides = @{
    "Metrics.Api" = "ZombiePlague.Core"
}

$projectBuildMetadataCache = @{}
$assemblyNameCache = @{}
$projectXmlCache = @{}

function Convert-ToPlatformPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return $Path -replace `
        '[\\/]', `
        [System.IO.Path]::DirectorySeparatorChar
}

function Get-ProjectKey {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    return (
        [System.IO.Path]::GetFullPath($ProjectPath)
    ).ToLowerInvariant()
}

function Get-SolutionProjectPaths {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Solution
    )

    $projects = @()

    foreach ($line in Get-Content -LiteralPath $Solution) {
        if (
            $line -match
            'Project\(".*?"\)\s*=\s*".*?",\s*"([^"]+\.csproj)"'
        ) {
            $relativePath = Convert-ToPlatformPath `
                $Matches[1]

            $absolutePath = [System.IO.Path]::GetFullPath(
                (Join-Path $root $relativePath)
            )

            if (Test-Path -LiteralPath $absolutePath) {
                $projects += $absolutePath
            }
        }
    }

    return @(
        $projects |
        Sort-Object -Unique
    )
}

function Get-ProjectXml {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    $key = Get-ProjectKey $ProjectPath

    if ($projectXmlCache.ContainsKey($key)) {
        return $projectXmlCache[$key]
    }

    $xml = [xml](
        Get-Content `
            -LiteralPath $ProjectPath `
            -Raw
    )

    $projectXmlCache[$key] = $xml

    return $xml
}

function Get-ProjectName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    return [System.IO.Path]::GetFileNameWithoutExtension(
        $ProjectPath
    )
}

function Test-IsTestProject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    $xml = Get-ProjectXml $ProjectPath

    foreach (
        $node in @(
            $xml.SelectNodes("//IsTestProject")
        )
    ) {
        if (
            $node.InnerText.Equals(
                "true",
                [System.StringComparison]::OrdinalIgnoreCase
            )
        ) {
            return $true
        }
    }

    return $false
}

function Test-IsApiProject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    $name = Get-ProjectName $ProjectPath

    return $name.EndsWith(
        ".Api",
        [System.StringComparison]::OrdinalIgnoreCase
    )
}

function Test-IsCommonProject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    if (Test-IsTestProject $ProjectPath) {
        return $false
    }

    $name = Get-ProjectName $ProjectPath

    return $name.StartsWith(
        "Common.",
        [System.StringComparison]::OrdinalIgnoreCase
    )
}

function Test-IsPrivateCommonProject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    if (-not (Test-IsCommonProject $ProjectPath)) {
        return $false
    }

    $name = Get-ProjectName $ProjectPath

    return $privateCommonProjectNames -contains $name
}

function Test-IsSharedCommonProject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    return (
        (Test-IsCommonProject $ProjectPath) -and
        -not (Test-IsPrivateCommonProject $ProjectPath)
    )
}

function Test-IsSwiftlyPlugin {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    if (Test-IsTestProject $ProjectPath) {
        return $false
    }

    if (Test-IsApiProject $ProjectPath) {
        return $false
    }

    if (Test-IsCommonProject $ProjectPath) {
        return $false
    }

    $xml = Get-ProjectXml $ProjectPath

    $swiftlyReferences = @(
        $xml.SelectNodes(
            "//PackageReference[@Include='SwiftlyS2.CS2']"
        )
    )

    return $swiftlyReferences.Count -gt 0
}

function Get-ApiHostPluginName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ApiProjectPath
    )

    $apiProjectName = Get-ProjectName $ApiProjectPath

    if (
        -not $apiProjectName.EndsWith(
            ".Api",
            [System.StringComparison]::OrdinalIgnoreCase
        )
    ) {
        throw "Проект не является API: $apiProjectName"
    }

    if ($apiHostOverrides.ContainsKey($apiProjectName)) {
        return $apiHostOverrides[$apiProjectName]
    }

    $prefix = $apiProjectName.Substring(
        0,
        $apiProjectName.Length - ".Api".Length
    )

    return "$prefix.Core"
}

function Get-ProjectReferencePaths {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    $xml = Get-ProjectXml $ProjectPath

    $projectDirectory = Split-Path `
        -Parent `
        $ProjectPath

    $references = @()

    foreach (
        $node in @(
            $xml.SelectNodes("//ProjectReference")
        )
    ) {
        $include = [string]$node.Include

        if ([string]::IsNullOrWhiteSpace($include)) {
            continue
        }

        $relativePath = Convert-ToPlatformPath `
            $include

        $absolutePath = [System.IO.Path]::GetFullPath(
            (
                Join-Path `
                    $projectDirectory `
                    $relativePath
            )
        )

        if (-not (Test-Path -LiteralPath $absolutePath)) {
            throw @"
ProjectReference не найден.

Проект:
$ProjectPath

Reference:
$absolutePath
"@
        }

        $references += $absolutePath
    }

    return @(
        $references |
        Sort-Object -Unique
    )
}

function Get-ReferencedProjectClosure {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    $result = @{}

    $queue = New-Object `
        "System.Collections.Generic.Queue[string]"

    foreach (
        $reference in Get-ProjectReferencePaths $ProjectPath
    ) {
        $queue.Enqueue($reference)
    }

    while ($queue.Count -gt 0) {
        $current = $queue.Dequeue()
        $key = Get-ProjectKey $current

        if ($result.ContainsKey($key)) {
            continue
        }

        $result[$key] = $current

        foreach (
            $nestedReference in
            Get-ProjectReferencePaths $current
        ) {
            $queue.Enqueue($nestedReference)
        }
    }

    return @(
        $result.Values
    )
}

function Get-ProjectBuildMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    $key = Get-ProjectKey $ProjectPath

    if ($projectBuildMetadataCache.ContainsKey($key)) {
        return $projectBuildMetadataCache[$key]
    }

    $projectName = Get-ProjectName $ProjectPath
    $metadataPath = Join-Path `
        $buildMetadataRoot `
        "$projectName.txt"

    if (-not (Test-Path -LiteralPath $metadataPath)) {
        throw @"
Не найдены build-метаданные проекта.

Project:
$ProjectPath

Ожидались:
$metadataPath

Сначала выполните dotnet build для конфигурации $Configuration.
"@
    }

    $lines = @(
        Get-Content -LiteralPath $metadataPath |
        ForEach-Object {
            $_.ToString().Trim()
        } |
        Where-Object {
            -not [string]::IsNullOrWhiteSpace($_)
        }
    )

    if ($lines.Count -lt 2) {
        throw @"
Некорректные build-метаданные проекта.

Project:
$ProjectPath

File:
$metadataPath
"@
    }

    $assemblyName = $lines[0]
    $candidateTargetPath = Convert-ToPlatformPath $lines[1]

    if ([System.IO.Path]::IsPathRooted($candidateTargetPath)) {
        $targetPath = [System.IO.Path]::GetFullPath(
            $candidateTargetPath
        )
    }
    else {
        $targetPath = [System.IO.Path]::GetFullPath(
            (Join-Path $root $candidateTargetPath)
        )
    }

    $metadata = [PSCustomObject]@{
        AssemblyName = $assemblyName
        TargetPath = $targetPath
    }

    $projectBuildMetadataCache[$key] = $metadata

    return $metadata
}

function Get-ProjectAssemblyName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    $key = Get-ProjectKey $ProjectPath

    if ($assemblyNameCache.ContainsKey($key)) {
        return $assemblyNameCache[$key]
    }

    $projectName = Get-ProjectName $ProjectPath
    $metadataPath = Join-Path `
        $buildMetadataRoot `
        "$projectName.txt"

    if (Test-Path -LiteralPath $metadataPath) {
        $assemblyName = (
            Get-ProjectBuildMetadata $ProjectPath
        ).AssemblyName
    }
    else {
        $xml = Get-ProjectXml $ProjectPath
        $assemblyNameNode = $xml.SelectSingleNode(
            "//AssemblyName"
        )

        $assemblyName = if (
            $null -ne $assemblyNameNode -and
            -not [string]::IsNullOrWhiteSpace(
                $assemblyNameNode.InnerText
            )
        ) {
            $assemblyNameNode.InnerText.Trim()
        }
        else {
            $projectName
        }
    }

    $assemblyNameCache[$key] = $assemblyName

    return $assemblyName
}

function Get-ProjectTargetPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,

        [switch]$RequireExists
    )

    $targetPath = (
        Get-ProjectBuildMetadata $ProjectPath
    ).TargetPath

    if (
        $RequireExists -and
        -not (Test-Path -LiteralPath $targetPath)
    ) {
        throw @"
Проект ещё не собран.

Project:
$ProjectPath

Ожидался:
$targetPath
"@
    }

    return $targetPath
}

function Copy-DirectoryContent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,

        [Parameter(Mandatory = $true)]
        [string]$Destination
    )

    New-Item `
        -ItemType Directory `
        -Path $Destination `
        -Force |
        Out-Null

    Get-ChildItem `
        -LiteralPath $Source `
        -Force |
        ForEach-Object {
            Copy-Item `
                -LiteralPath $_.FullName `
                -Destination $Destination `
                -Recurse `
                -Force
        }
}

function Remove-FilesByName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Directory,

        [Parameter(Mandatory = $true)]
        [string[]]$Names
    )

    if ($Names.Count -eq 0) {
        return
    }

    $nameSet = @{}

    foreach ($name in $Names) {
        $nameSet[
            $name.ToLowerInvariant()
        ] = $true
    }

    Get-ChildItem `
        -LiteralPath $Directory `
        -Recurse `
        -File `
        -ErrorAction SilentlyContinue |
        ForEach-Object {
            if (
                $nameSet.ContainsKey(
                    $_.Name.ToLowerInvariant()
                )
            ) {
                Remove-Item `
                    -LiteralPath $_.FullName `
                    -Force
            }
        }
}

function Remove-ForbiddenRuntimeFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Directory
    )

    Get-ChildItem `
        -LiteralPath $Directory `
        -Recurse `
        -File `
        -ErrorAction SilentlyContinue |
        ForEach-Object {
            $name = $_.Name

            $isPdb =
                $name.EndsWith(
                    ".pdb",
                    [System.StringComparison]::OrdinalIgnoreCase
                )

            $isTestFile =
                $name -match '\.Tests\.(dll|pdb)$' -or
                $name -match '^xunit.*\.(dll|pdb)$' -or
                $name -match '^testhost.*\.(dll|pdb)$'

            $shouldRemove =
                $isTestFile -or
                (
                    $Configuration -eq "Release" -and
                    $isPdb
                )

            if ($shouldRemove) {
                Remove-Item `
                    -LiteralPath $_.FullName `
                    -Force
            }
        }
}

function Copy-AssemblyWithSymbols {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceDll,

        [Parameter(Mandatory = $true)]
        [string]$DestinationDirectory
    )

    if (-not (Test-Path -LiteralPath $SourceDll)) {
        throw "Assembly не найдена: $SourceDll"
    }

    New-Item `
        -ItemType Directory `
        -Path $DestinationDirectory `
        -Force |
        Out-Null

    Copy-Item `
        -LiteralPath $SourceDll `
        -Destination $DestinationDirectory `
        -Force

    $sourcePdb = [System.IO.Path]::ChangeExtension(
        $SourceDll,
        ".pdb"
    )

    $destinationPdb = Join-Path `
        $DestinationDirectory `
        ([System.IO.Path]::GetFileName($sourcePdb))

    if (
        $Configuration -eq "Debug" -and
        (Test-Path -LiteralPath $sourcePdb)
    ) {
        Copy-Item `
            -LiteralPath $sourcePdb `
            -Destination $destinationPdb `
            -Force
    }
    elseif (
        $Configuration -eq "Release" -and
        (Test-Path -LiteralPath $destinationPdb)
    ) {
        Remove-Item `
            -LiteralPath $destinationPdb `
            -Force
    }
}

function New-PluginZip {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PluginName
    )

    $pluginDirectory = Join-Path `
        $pluginsRoot `
        $PluginName

    if (-not (Test-Path -LiteralPath $pluginDirectory)) {
        throw "Runtime-папка плагина не найдена: $PluginName"
    }

    $zipPath = Join-Path `
        $packagesRoot `
        "$PluginName.zip"

    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item `
            -LiteralPath $zipPath `
            -Force
    }

    Compress-Archive `
        -Path $pluginDirectory `
        -DestinationPath $zipPath `
        -CompressionLevel Optimal

    $zip = Get-Item `
        -LiteralPath $zipPath

    $sizeMb = [Math]::Round(
        $zip.Length / 1MB,
        2
    )

    Write-Host (
        "  {0,-45} {1,8} MB" -f
        $zip.Name,
        $sizeMb
    )
}

function Get-RelativeRuntimePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BasePath,

        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return [System.IO.Path]::GetRelativePath(
        $BasePath,
        $Path
    ) -replace '\\', '/'
}

function Test-IsForbiddenRuntimeFile {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.FileInfo]$File
    )

    foreach ($forbiddenName in @($runtimePolicy.forbiddenFileNames)) {
        if (
            $File.Name.Equals(
                $forbiddenName,
                [System.StringComparison]::OrdinalIgnoreCase
            )
        ) {
            return $true
        }
    }

    foreach ($forbiddenPrefix in @($runtimePolicy.forbiddenFilePrefixes)) {
        if (
            $File.Name.StartsWith(
                $forbiddenPrefix,
                [System.StringComparison]::OrdinalIgnoreCase
            )
        ) {
            return $true
        }
    }

    $relativePath = "/" + (
        Get-RelativeRuntimePath `
            -BasePath $pluginsRoot `
            -Path $File.FullName
    ).ToLowerInvariant()

    foreach (
        $forbiddenFragment in
        @($runtimePolicy.forbiddenPathFragments)
    ) {
        if (
            $relativePath.Contains(
                $forbiddenFragment.ToLowerInvariant(),
                [System.StringComparison]::Ordinal
            )
        ) {
            return $true
        }
    }

    return $false
}

function New-RuntimeManifest {
    $pluginEntries = @()
    [long]$totalBytes = 0
    [int]$totalFileCount = 0

    foreach (
        $pluginDirectory in @(
            Get-ChildItem `
                -LiteralPath $pluginsRoot `
                -Directory |
            Sort-Object Name
        )
    ) {
        $entryAssembly = Join-Path `
            $pluginDirectory.FullName `
            "$($pluginDirectory.Name).dll"

        if (-not (Test-Path -LiteralPath $entryAssembly)) {
            throw @"
В runtime-папке отсутствует entry assembly.

Plugin:
$($pluginDirectory.Name)

Ожидалась:
$entryAssembly
"@
        }

        $fileEntries = @()
        [long]$pluginBytes = 0

        foreach (
            $file in @(
                Get-ChildItem `
                    -LiteralPath $pluginDirectory.FullName `
                    -Recurse `
                    -File |
                Sort-Object FullName
            )
        ) {
            $relativePath = Get-RelativeRuntimePath `
                -BasePath $pluginDirectory.FullName `
                -Path $file.FullName

            $hash = (
                Get-FileHash `
                    -LiteralPath $file.FullName `
                    -Algorithm SHA256
            ).Hash.ToLowerInvariant()

            $fileEntries += [ordered]@{
                path = $relativePath
                sizeBytes = [long]$file.Length
                sha256 = $hash
            }

            $pluginBytes += [long]$file.Length
        }

        $pluginEntries += [ordered]@{
            name = $pluginDirectory.Name
            entryAssembly = "$($pluginDirectory.Name).dll"
            fileCount = $fileEntries.Count
            totalBytes = $pluginBytes
            files = $fileEntries
        }

        $totalBytes += $pluginBytes
        $totalFileCount += $fileEntries.Count
    }

    $commit = $null
    $gitOutput = @(
        & git `
            -C $root `
            rev-parse HEAD `
            2> $null
    )

    if ($LASTEXITCODE -eq 0 -and $gitOutput.Count -gt 0) {
        $commit = $gitOutput[0].Trim()
    }

    $manifest = [ordered]@{
        schemaVersion = 1
        generatedAt = [DateTimeOffset]::UtcNow.ToString("O")
        configuration = $Configuration
        targetRuntime = $runtimePolicy.targetRuntime
        commit = $commit
        pluginCount = $pluginEntries.Count
        fileCount = $totalFileCount
        totalBytes = $totalBytes
        plugins = $pluginEntries
    }

    $manifest |
        ConvertTo-Json -Depth 8 |
        Set-Content `
            -LiteralPath $runtimeManifestPath `
            -Encoding utf8

    return $manifest
}

function New-FullRuntimeZip {
    $zipPath = Join-Path `
        $packagesRoot `
        "Elysium.Runtime.$Configuration.zip"

    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item `
            -LiteralPath $zipPath `
            -Force
    }

    $sources = @(
        $pluginsRoot,
        $runtimeManifestPath
    )

    Compress-Archive `
        -Path $sources `
        -DestinationPath $zipPath `
        -CompressionLevel Optimal

    return Get-Item -LiteralPath $zipPath
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Elysium runtime packaging" -ForegroundColor Cyan
Write-Host " Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ============================================================
# Projects
# ============================================================

$solutionProjects = @(
    Get-SolutionProjectPaths $solutionPath
)

if ($solutionProjects.Count -eq 0) {
    throw "В solution не найдено ни одного csproj."
}

$allRuntimePluginProjects = @(
    $solutionProjects |
    Where-Object {
        Test-IsSwiftlyPlugin $_
    }
)

$allApiProjects = @(
    $solutionProjects |
    Where-Object {
        Test-IsApiProject $_
    }
)

$allCommonProjects = @(
    $solutionProjects |
    Where-Object {
        Test-IsCommonProject $_
    }
)

$allSharedCommonProjects = @(
    $allCommonProjects |
    Where-Object {
        Test-IsSharedCommonProject $_
    }
)

$allPrivateCommonProjects = @(
    $allCommonProjects |
    Where-Object {
        Test-IsPrivateCommonProject $_
    }
)

$runtimePluginMap = @{}

foreach ($project in $allRuntimePluginProjects) {
    $name = Get-ProjectName $project

    $runtimePluginMap[
        $name.ToLowerInvariant()
    ] = $project
}

# ============================================================
# Validate API hosts
# ============================================================

foreach ($apiProject in $allApiProjects) {
    $apiName = Get-ProjectName $apiProject

    $hostPluginName = Get-ApiHostPluginName `
        $apiProject

    if (
        -not $runtimePluginMap.ContainsKey(
            $hostPluginName.ToLowerInvariant()
        )
    ) {
        throw @"
Для API не найден runtime host.

API:
$apiName

Ожидался plugin:
$hostPluginName
"@
    }
}

# ============================================================
# Full / partial mode
# ============================================================

$requestedPluginNames = @(
    $Plugins |
    Where-Object {
        -not [string]::IsNullOrWhiteSpace($_)
    } |
    ForEach-Object {
        $_.Trim()
    } |
    Sort-Object -Unique
)

$partialMode =
    $requestedPluginNames.Count -gt 0

if ($partialMode) {
    foreach ($requestedPluginName in $requestedPluginNames) {
        if (
            -not $runtimePluginMap.ContainsKey(
                $requestedPluginName.ToLowerInvariant()
            )
        ) {
            throw "Неизвестный runtime-плагин: $requestedPluginName"
        }
    }
}

# Частичная упаковка возможна только поверх уже
# существующей полной runtime-сборки.
if ($partialMode) {
    $baselineMissing = $false

    foreach ($project in $allRuntimePluginProjects) {
        $name = Get-ProjectName $project

        $directory = Join-Path `
            $pluginsRoot `
            $name

        if (-not (Test-Path -LiteralPath $directory)) {
            $baselineMissing = $true
            break
        }
    }

    if ($baselineMissing) {
        Write-Host (
            "Полная $Configuration runtime-сборка отсутствует. " +
            "Перехожу в полный режим."
        ) -ForegroundColor Yellow

        $partialMode = $false
        $requestedPluginNames = @()
    }
}

if ($partialMode) {
    $selectedPluginProjects = @(
        $requestedPluginNames |
        ForEach-Object {
            $runtimePluginMap[
                $_.ToLowerInvariant()
            ]
        }
    )
}
else {
    $selectedPluginProjects = @(
        $allRuntimePluginProjects
    )

    if (Test-Path -LiteralPath $configurationRoot) {
        Remove-Item `
            -LiteralPath $configurationRoot `
            -Recurse `
            -Force
    }
}

New-Item `
    -ItemType Directory `
    -Path $pluginsRoot `
    -Force |
    Out-Null

New-Item `
    -ItemType Directory `
    -Path $packagesRoot `
    -Force |
    Out-Null

Write-Host (
    "Режим: " +
    $(if ($partialMode) { "частичный" } else { "полный" })
) -ForegroundColor Cyan

Write-Host ""
Write-Host "Runtime plugins:" -ForegroundColor Cyan

foreach ($project in $selectedPluginProjects) {
    Write-Host "  - $(Get-ProjectName $project)"
}

Write-Host ""
Write-Host "Swiftly shared Common exports:" -ForegroundColor Cyan

foreach ($project in $allSharedCommonProjects) {
    Write-Host "  - $(Get-ProjectName $project)"
}

Write-Host ""
Write-Host "Private Common dependencies:" -ForegroundColor Cyan

foreach ($project in $allPrivateCommonProjects) {
    Write-Host "  - $(Get-ProjectName $project)"
}

Write-Host ""
Write-Host "Shared API hosts:" -ForegroundColor Cyan

foreach (
    $apiProject in @(
        $allApiProjects |
        Sort-Object {
            Get-ProjectName $_
        }
    )
) {
    $apiName = Get-ProjectName $apiProject

    $apiHostPluginName = Get-ApiHostPluginName `
        $apiProject

    Write-Host (
        "  - $apiName -> $apiHostPluginName"
    )
}

Write-Host ""

# ============================================================
# Known assembly names
# ============================================================

$apiRuntimeNames = @()
$allCommonRuntimeNames = @()

foreach ($project in $allApiProjects) {
    $assemblyName = Get-ProjectAssemblyName `
        $project

    $apiRuntimeNames += "$assemblyName.dll"
    $apiRuntimeNames += "$assemblyName.pdb"
}

foreach ($project in $allCommonProjects) {
    $assemblyName = Get-ProjectAssemblyName `
        $project

    $allCommonRuntimeNames += "$assemblyName.dll"
    $allCommonRuntimeNames += "$assemblyName.pdb"
}

# ============================================================
# Determine used shared Common
# ============================================================

$allUsedSharedCommonProjects = @{}

foreach ($pluginProject in $allRuntimePluginProjects) {
    foreach (
        $reference in
        Get-ReferencedProjectClosure $pluginProject
    ) {
        if (Test-IsSharedCommonProject $reference) {
            $allUsedSharedCommonProjects[
                (Get-ProjectKey $reference)
            ] = $reference
        }
    }
}

$sharedCommonProjectsToRefresh = @{}

foreach ($pluginProject in $selectedPluginProjects) {
    foreach (
        $reference in
        Get-ReferencedProjectClosure $pluginProject
    ) {
        if (Test-IsSharedCommonProject $reference) {
            $sharedCommonProjectsToRefresh[
                (Get-ProjectKey $reference)
            ] = $reference
        }
    }
}

if (-not $partialMode) {
    $sharedCommonProjectsToRefresh = @{}

    foreach ($key in $allUsedSharedCommonProjects.Keys) {
        $sharedCommonProjectsToRefresh[$key] =
            $allUsedSharedCommonProjects[$key]
    }
}

# ============================================================
# Package selected plugins
# ============================================================

$pluginPrivateCommonRequirements = @{}

foreach ($pluginProject in $selectedPluginProjects) {
    $pluginName = Get-ProjectName `
        $pluginProject

    Write-Host (
        "Формирую $pluginName..."
    ) -ForegroundColor Yellow

    $pluginTarget = Get-ProjectTargetPath `
        -ProjectPath $pluginProject `
        -RequireExists

    $pluginOutputDirectory = Split-Path `
        -Parent `
        $pluginTarget

    $pluginDirectory = Join-Path `
        $pluginsRoot `
        $pluginName

    if (Test-Path -LiteralPath $pluginDirectory) {
        Remove-Item `
            -LiteralPath $pluginDirectory `
            -Recurse `
            -Force
    }

    # Копируем полный output Core-проекта.
    #
    # Он содержит NuGet runtime dependencies конкретного плагина:
    # EF Core, Npgsql и т.д.
    Copy-DirectoryContent `
        -Source $pluginOutputDirectory `
        -Destination $pluginDirectory

    # Удаляем ВСЕ API/Common project assemblies,
    # которые могли попасть сюда через MSBuild/старые targets.
    #
    # После этого сами раскладываем их по правильным местам.
    Remove-FilesByName `
        -Directory $pluginDirectory `
        -Names $apiRuntimeNames

    Remove-FilesByName `
        -Directory $pluginDirectory `
        -Names $allCommonRuntimeNames

    Remove-ForbiddenRuntimeFiles `
        -Directory $pluginDirectory

    $referencedProjects = @(
        Get-ReferencedProjectClosure `
            $pluginProject
    )

    # --------------------------------------------------------
    # Private Common
    # --------------------------------------------------------

    $requiredPrivateCommonProjects = @(
        $referencedProjects |
        Where-Object {
            Test-IsPrivateCommonProject $_
        }
    )

    $pluginPrivateCommonRequirements[$pluginName] = @()

    foreach (
        $privateCommonProject in
        $requiredPrivateCommonProjects
    ) {
        $privateCommonTarget = Get-ProjectTargetPath `
            -ProjectPath $privateCommonProject `
            -RequireExists

        $privateCommonFileName =
            [System.IO.Path]::GetFileName(
                $privateCommonTarget
            )

        Copy-AssemblyWithSymbols `
            -SourceDll $privateCommonTarget `
            -DestinationDirectory $pluginDirectory

        $pluginPrivateCommonRequirements[$pluginName] +=
            $privateCommonFileName

        Write-Host (
            "    private common: $privateCommonFileName"
        )
    }

    # --------------------------------------------------------
    # Shared API hosted by this plugin
    # --------------------------------------------------------

    $hostedApiProjects = @(
        $allApiProjects |
        Where-Object {
            (Get-ApiHostPluginName $_).Equals(
                $pluginName,
                [System.StringComparison]::OrdinalIgnoreCase
            )
        }
    )

    if ($hostedApiProjects.Count -gt 0) {
        $pluginExportsDirectory = Join-Path `
            $pluginDirectory `
            "resources\exports"

        New-Item `
            -ItemType Directory `
            -Path $pluginExportsDirectory `
            -Force |
            Out-Null

        foreach ($apiProject in $hostedApiProjects) {
            $apiTarget = Get-ProjectTargetPath `
                -ProjectPath $apiProject `
                -RequireExists

            $apiFileName =
                [System.IO.Path]::GetFileName(
                    $apiTarget
                )

            Copy-AssemblyWithSymbols `
                -SourceDll $apiTarget `
                -DestinationDirectory $pluginExportsDirectory

            Write-Host (
                "    export API: $apiFileName"
            )
        }
    }
}

# ============================================================
# Shared Common exports
# ============================================================

$zombiePlagueDirectory = Join-Path `
    $pluginsRoot `
    "ZombiePlague.Core"

if (-not (Test-Path -LiteralPath $zombiePlagueDirectory)) {
    throw @"
ZombiePlague.Core отсутствует в runtime.

Shared Common exports невозможно разместить
в ZombiePlague.Core/resources/exports.
"@
}

$exportsDirectory = Join-Path `
    $zombiePlagueDirectory `
    "resources\exports"

New-Item `
    -ItemType Directory `
    -Path $exportsDirectory `
    -Force |
    Out-Null

# Ожидаемый набор shared Common.
$expectedSharedCommonFiles = @{}

foreach (
    $project in $allUsedSharedCommonProjects.Values
) {
    $assemblyName = Get-ProjectAssemblyName `
        $project

    $expectedSharedCommonFiles[
        "$assemblyName.dll".ToLowerInvariant()
    ] = $true

    if ($Configuration -eq "Debug") {
        $expectedSharedCommonFiles[
            "$assemblyName.pdb".ToLowerInvariant()
        ] = $true
    }
}

# Удаляем устаревшие Common exports.
#
# В частности это гарантирует, что Common.Database
# никогда не останется в exports от старой сборки.
Get-ChildItem `
    -LiteralPath $exportsDirectory `
    -File `
    -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Name -like "Common.*.dll" -or
        $_.Name -like "Common.*.pdb"
    } |
    ForEach-Object {
        if (
            -not $expectedSharedCommonFiles.ContainsKey(
                $_.Name.ToLowerInvariant()
            )
        ) {
            Write-Host (
                "Удаляю устаревший export: $($_.Name)"
            ) -ForegroundColor DarkGray

            Remove-Item `
                -LiteralPath $_.FullName `
                -Force
        }
    }

Write-Host ""
Write-Host (
    "ZombiePlague shared Common exports:"
) -ForegroundColor Yellow

foreach (
    $sharedCommonProject in @(
        $sharedCommonProjectsToRefresh.Values |
        Sort-Object
    )
) {
    $sharedCommonTarget = Get-ProjectTargetPath `
        -ProjectPath $sharedCommonProject `
        -RequireExists

    $sharedCommonFileName =
        [System.IO.Path]::GetFileName(
            $sharedCommonTarget
        )

    Copy-AssemblyWithSymbols `
        -SourceDll $sharedCommonTarget `
        -DestinationDirectory $exportsDirectory

    Write-Host (
        "    export Common: $sharedCommonFileName"
    )
}

if ($Configuration -eq "Release") {
    Get-ChildItem `
        -LiteralPath $exportsDirectory `
        -Filter "*.pdb" `
        -File `
        -ErrorAction SilentlyContinue |
        Remove-Item -Force
}

# ============================================================
# Runtime validation
# ============================================================

Write-Host ""
Write-Host "Проверяю runtime..." -ForegroundColor Cyan

# ------------------------------------------------------------
# Tests
# ------------------------------------------------------------

$forbiddenTestFiles = @(
    Get-ChildItem `
        -LiteralPath $pluginsRoot `
        -Recurse `
        -File |
        Where-Object {
            $_.Name -match '\.Tests\.(dll|pdb)$' -or
            $_.Name -match '^xunit.*\.(dll|pdb)$' -or
            $_.Name -match '^testhost.*\.(dll|pdb)$'
        }
)

if ($forbiddenTestFiles.Count -gt 0) {
    Write-Host ""
    Write-Host (
        "В runtime попали тестовые зависимости:"
    ) -ForegroundColor Red

    foreach ($file in $forbiddenTestFiles) {
        Write-Host (
            "  $($file.FullName)"
        ) -ForegroundColor Red
    }

    throw "Runtime validation failed."
}

# ------------------------------------------------------------
# Private Common
# ------------------------------------------------------------

foreach (
    $pluginName in
    $pluginPrivateCommonRequirements.Keys
) {
    $pluginDirectory = Join-Path `
        $pluginsRoot `
        $pluginName

    foreach (
        $privateCommonFileName in
        $pluginPrivateCommonRequirements[$pluginName]
    ) {
        $expectedPrivateCommon = Join-Path `
            $pluginDirectory `
            $privateCommonFileName

        if (
            -not (
                Test-Path `
                    -LiteralPath $expectedPrivateCommon
            )
        ) {
            throw @"
Не найдена обязательная private Common assembly.

Plugin:
$pluginName

Assembly:
$privateCommonFileName
"@
        }
    }
}

# ------------------------------------------------------------
# Common.Database runtime dependencies
# ------------------------------------------------------------

$databaseRuntimeDependencies = @(
    "Microsoft.EntityFrameworkCore.dll",
    "Microsoft.EntityFrameworkCore.Abstractions.dll",
    "Microsoft.EntityFrameworkCore.Relational.dll",
    "Npgsql.dll",
    "Npgsql.EntityFrameworkCore.PostgreSQL.dll"
)

foreach (
    $pluginName in
    $pluginPrivateCommonRequirements.Keys
) {
    $requiresCommonDatabase =
        $pluginPrivateCommonRequirements[$pluginName] -contains
        "Common.Database.dll"

    if (-not $requiresCommonDatabase) {
        continue
    }

    $pluginDirectory = Join-Path `
        $pluginsRoot `
        $pluginName

    foreach (
        $dependencyName in
        $databaseRuntimeDependencies
    ) {
        $dependencyPath = Join-Path `
            $pluginDirectory `
            $dependencyName

        if (
            -not (
                Test-Path `
                    -LiteralPath $dependencyPath
            )
        ) {
            throw @"
Runtime dependency Common.Database отсутствует.

Plugin:
$pluginName

Dependency:
$dependencyName

Проверь NuGet runtime dependencies
и CopyLocalLockFileAssemblies проекта.
"@
        }
    }
}

# ------------------------------------------------------------
# Common.Database forbidden from exports
# ------------------------------------------------------------

$databaseExports = @(
    Get-ChildItem `
        -LiteralPath $pluginsRoot `
        -Recurse `
        -File `
        -Filter "Common.Database.dll" |
        Where-Object {
            $_.DirectoryName -match
            '[\\/]resources[\\/]exports$'
        }
)

if ($databaseExports.Count -gt 0) {
    Write-Host ""
    Write-Host (
        "Common.Database обнаружен в exports:"
    ) -ForegroundColor Red

    foreach ($file in $databaseExports) {
        Write-Host (
            "  $($file.FullName)"
        ) -ForegroundColor Red
    }

    throw @"
Common.Database.dll не может быть Swiftly export.

Он должен находиться рядом с Core-плагином,
который использует базу данных.
"@
}

# EF/Npgsql также не являются Swiftly exports.
$forbiddenThirdPartyExports = @(
    Get-ChildItem `
        -LiteralPath $pluginsRoot `
        -Recurse `
        -File |
        Where-Object {
            (
                $_.Name -like
                "Microsoft.EntityFrameworkCore*.dll"
            ) -or
            (
                $_.Name -like
                "Npgsql*.dll"
            )
        } |
        Where-Object {
            $_.DirectoryName -match
            '[\\/]resources[\\/]exports$'
        }
)

if ($forbiddenThirdPartyExports.Count -gt 0) {
    Write-Host ""
    Write-Host (
        "Third-party DLL обнаружены в exports:"
    ) -ForegroundColor Red

    foreach ($file in $forbiddenThirdPartyExports) {
        Write-Host (
            "  $($file.FullName)"
        ) -ForegroundColor Red
    }

    throw @"
EF Core / Npgsql не должны находиться
в resources/exports.
"@
}

# ------------------------------------------------------------
# Shared Common
# ------------------------------------------------------------

foreach (
    $sharedCommonProject in
    $allUsedSharedCommonProjects.Values
) {
    $assemblyName = Get-ProjectAssemblyName `
        $sharedCommonProject

    $expectedCommon = Join-Path `
        $exportsDirectory `
        "$assemblyName.dll"

    if (
        -not (
            Test-Path `
                -LiteralPath $expectedCommon
        )
    ) {
        throw @"
Не найден shared Common export.

Assembly:
$assemblyName.dll

Ожидался:
$expectedCommon
"@
    }
}

$sharedCommonNameSet = @{}

foreach (
    $sharedCommonProject in
    $allSharedCommonProjects
) {
    $assemblyName = Get-ProjectAssemblyName `
        $sharedCommonProject

    $sharedCommonNameSet[
        "$assemblyName.dll".ToLowerInvariant()
    ] = $true

    $sharedCommonNameSet[
        "$assemblyName.pdb".ToLowerInvariant()
    ] = $true
}

$exportsFullPath = (
    [System.IO.Path]::GetFullPath(
        $exportsDirectory
    )
).TrimEnd(
    [System.IO.Path]::DirectorySeparatorChar
)

$exportsPrefix = (
    $exportsFullPath +
    [System.IO.Path]::DirectorySeparatorChar
)

Get-ChildItem `
    -LiteralPath $pluginsRoot `
    -Recurse `
    -File |
    ForEach-Object {
        if (
            -not $sharedCommonNameSet.ContainsKey(
                $_.Name.ToLowerInvariant()
            )
        ) {
            return
        }

        $fullPath = [System.IO.Path]::GetFullPath(
            $_.FullName
        )

        if (
            -not $fullPath.StartsWith(
                $exportsPrefix,
                [System.StringComparison]::OrdinalIgnoreCase
            )
        ) {
            throw @"
Shared Common assembly находится вне
ZombiePlague.Core/resources/exports.

Assembly:
$($_.Name)

Path:
$fullPath
"@
        }
    }

# ------------------------------------------------------------
# Shared APIs
# ------------------------------------------------------------

Write-Host ""
Write-Host (
    "Проверяю shared API exports..."
) -ForegroundColor Cyan

foreach ($apiProject in $allApiProjects) {
    $apiAssemblyName = Get-ProjectAssemblyName `
        $apiProject

    $apiFileName = "$apiAssemblyName.dll"

    $hostPluginName = Get-ApiHostPluginName `
        $apiProject

    $hostPluginDirectory = Join-Path `
        $pluginsRoot `
        $hostPluginName

    $expectedApiPath = Join-Path `
        (Join-Path `
            $hostPluginDirectory `
            "resources\exports") `
        $apiFileName

    if (-not (Test-Path -LiteralPath $expectedApiPath)) {
        throw @"
Shared API отсутствует в exports своего host-плагина.

API:
$apiFileName

Host:
$hostPluginName

Ожидался:
$expectedApiPath
"@
    }

    # Самое важное правило:
    # одна API assembly = одна физическая DLL в runtime.
    $apiCopies = @(
        Get-ChildItem `
            -LiteralPath $pluginsRoot `
            -Recurse `
            -File `
            -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Name.Equals(
                $apiFileName,
                [System.StringComparison]::OrdinalIgnoreCase
            )
        }
    )

    if ($apiCopies.Count -ne 1) {
        Write-Host ""
        Write-Host (
            "Некорректное количество копий API: " +
            $apiFileName
        ) -ForegroundColor Red

        foreach ($copy in $apiCopies) {
            Write-Host (
                "  $($copy.FullName)"
            ) -ForegroundColor Red
        }

        throw @"
API assembly должна существовать
в runtime ровно один раз.

API:
$apiFileName

Найдено:
$($apiCopies.Count)
"@
    }

    $actualApiPath =
        [System.IO.Path]::GetFullPath(
            $apiCopies[0].FullName
        )

    $expectedApiFullPath =
        [System.IO.Path]::GetFullPath(
            $expectedApiPath
        )

    if (
        -not $actualApiPath.Equals(
            $expectedApiFullPath,
            [System.StringComparison]::OrdinalIgnoreCase
        )
    ) {
        throw @"
API находится не в exports своего host-плагина.

API:
$apiFileName

Найден:
$actualApiPath

Ожидался:
$expectedApiFullPath
"@
    }

    Write-Host (
        "    OK: $apiFileName -> $hostPluginName"
    )
}

# ------------------------------------------------------------
# Release must not contain symbols
# ------------------------------------------------------------

if ($Configuration -eq "Release") {
    $releasePdb = @(
        Get-ChildItem `
            -LiteralPath $pluginsRoot `
            -Recurse `
            -Filter "*.pdb" `
            -File
    )

    if ($releasePdb.Count -gt 0) {
        Write-Host ""
        Write-Host (
            "В Release runtime обнаружены PDB:"
        ) -ForegroundColor Red

        foreach ($file in $releasePdb) {
            Write-Host (
                "  $($file.FullName)"
            ) -ForegroundColor Red
        }

        throw "В Release runtime обнаружены PDB-файлы."
    }
}

# ------------------------------------------------------------
# Runtime package policy
# ------------------------------------------------------------

$allRuntimeFiles = @(
    Get-ChildItem `
        -LiteralPath $pluginsRoot `
        -Recurse `
        -File
)

$forbiddenRuntimeFiles = @(
    $allRuntimeFiles |
    Where-Object {
        Test-IsForbiddenRuntimeFile $_
    }
)

if ($forbiddenRuntimeFiles.Count -gt 0) {
    Write-Host ""
    Write-Host (
        "В runtime обнаружены build-only или host-provided файлы:"
    ) -ForegroundColor Red

    foreach ($file in $forbiddenRuntimeFiles) {
        Write-Host (
            "  " + (
                Get-RelativeRuntimePath `
                    -BasePath $pluginsRoot `
                    -Path $file.FullName
            )
        ) -ForegroundColor Red
    }

    throw @"
Runtime package policy failed.

Проверьте PackageReference metadata:
PrivateAssets / ExcludeAssets и runtime dependency graph.
"@
}

$runtimeManifest = New-RuntimeManifest

if (
    $Configuration -eq "Release" -and
    [long]$runtimeManifest.totalBytes -gt
    [long]$runtimePolicy.maxTotalBytes
) {
    throw @"
Runtime превышает допустимый размер.

Фактически:
$($runtimeManifest.totalBytes) bytes

Лимит:
$($runtimePolicy.maxTotalBytes) bytes
"@
}

if (
    $Configuration -eq "Release" -and
    [int]$runtimeManifest.fileCount -gt
    [int]$runtimePolicy.maxFileCount
) {
    throw @"
Runtime содержит слишком много файлов.

Фактически:
$($runtimeManifest.fileCount)

Лимит:
$($runtimePolicy.maxFileCount)
"@
}

Write-Host ""
Write-Host (
    "Runtime validation: OK"
) -ForegroundColor Green

Write-Host (
    "Runtime: {0} plugins, {1} files, {2:N2} MB" -f
    $runtimeManifest.pluginCount,
    $runtimeManifest.fileCount,
    ([double]$runtimeManifest.totalBytes / 1MB)
) -ForegroundColor Green

# ============================================================
# ZIP: отдельные плагины и полный атомарный runtime
# ============================================================

Write-Host ""
Write-Host "Создаю ZIP-пакеты..." -ForegroundColor Cyan

$zipPluginNames = @{}

foreach ($pluginProject in $selectedPluginProjects) {
    $name = Get-ProjectName `
        $pluginProject

    $zipPluginNames[
        $name.ToLowerInvariant()
    ] = $name
}

# Shared Common физически находятся внутри ZombiePlague.Core.
# При их обновлении runtime ZombiePlague.Core также изменяется.
if (
    $sharedCommonProjectsToRefresh.Count -gt 0 -and
    (Test-Path -LiteralPath $zombiePlagueDirectory)
) {
    $zipPluginNames[
        "zombieplague.core"
    ] = "ZombiePlague.Core"
}

# Если обновляется API, размещённый в другом host-плагине
# через override, ZIP host-плагина также должен обновиться.
foreach ($selectedPluginProject in $selectedPluginProjects) {
    $selectedProjectName = Get-ProjectName `
        $selectedPluginProject

    foreach ($apiProject in $allApiProjects) {
        $hostPluginName = Get-ApiHostPluginName `
            $apiProject

        if (
            $hostPluginName.Equals(
                $selectedProjectName,
                [System.StringComparison]::OrdinalIgnoreCase
            )
        ) {
            $zipPluginNames[
                $hostPluginName.ToLowerInvariant()
            ] = $hostPluginName
        }
    }
}

foreach (
    $pluginName in @(
        $zipPluginNames.Values |
        Sort-Object
    )
) {
    New-PluginZip `
        -PluginName $pluginName
}

$fullRuntimeZip = New-FullRuntimeZip

Write-Host (
    "  {0,-45} {1,8:N2} MB" -f
    $fullRuntimeZip.Name,
    ([double]$fullRuntimeZip.Length / 1MB)
)

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Упаковка завершена" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "Готовые папки для сервера:" -ForegroundColor Cyan
Write-Host $pluginsRoot

Write-Host ""
Write-Host "ZIP-пакеты:" -ForegroundColor Cyan
Write-Host $packagesRoot
