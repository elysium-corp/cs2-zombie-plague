param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$BaseRef
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..")
)

$solutionPath = Join-Path `
    $root `
    "CS2ZombiePlague.sln"

$packageScript = Join-Path `
    $PSScriptRoot `
    "package.ps1"

if (-not (Test-Path -LiteralPath $solutionPath)) {
    throw "Solution не найдена: $solutionPath"
}

if (-not (Test-Path -LiteralPath $packageScript)) {
    throw "package.ps1 не найден: $packageScript"
}

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
            $relativePath = Convert-ToPlatformPath $Matches[1]

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

    return (
        (Get-ProjectName $ProjectPath).StartsWith(
            "Common.",
            [System.StringComparison]::OrdinalIgnoreCase
        )
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

    return @(
        $xml.SelectNodes(
            "//PackageReference[@Include='SwiftlyS2.CS2']"
        )
    ).Count -gt 0
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

        $relativePath = Convert-ToPlatformPath $include

        $absolutePath = [System.IO.Path]::GetFullPath(
            (
                Join-Path `
                    $projectDirectory `
                    $relativePath
            )
        )

        if (Test-Path -LiteralPath $absolutePath) {
            $references += $absolutePath
        }
    }

    return @(
        $references |
        Sort-Object -Unique
    )
}

function Get-ChangedFiles {
    $files = @{}

    function Add-GitPaths {
        param(
            [string[]]$Paths
        )

        foreach ($path in $Paths) {
            if ([string]::IsNullOrWhiteSpace($path)) {
                continue
            }

            $normalized = $path.Trim() -replace '\\', '/'

            $files[
                $normalized.ToLowerInvariant()
            ] = $normalized
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($BaseRef)) {
        Write-Host (
            "Сравниваю ветку с: $BaseRef"
        ) -ForegroundColor Cyan

        & git rev-parse `
            --verify `
            $BaseRef `
            *> $null

        if ($LASTEXITCODE -ne 0) {
            throw "Git ref не найден: $BaseRef"
        }

        Add-GitPaths @(
            & git diff `
                --name-only `
                --diff-filter=ACMRD `
                "$BaseRef...HEAD"
        )

        if ($LASTEXITCODE -ne 0) {
            throw "Не удалось получить git diff относительно $BaseRef"
        }
    }

    # Незакоммиченные изменения.
    Add-GitPaths @(
        & git diff `
            --name-only `
            --diff-filter=ACMRD
    )

    if ($LASTEXITCODE -ne 0) {
        throw "git diff завершился с ошибкой."
    }

    # Staged изменения.
    Add-GitPaths @(
        & git diff `
            --cached `
            --name-only `
            --diff-filter=ACMRD
    )

    if ($LASTEXITCODE -ne 0) {
        throw "git diff --cached завершился с ошибкой."
    }

    # Новые untracked файлы.
    Add-GitPaths @(
        & git ls-files `
            --others `
            --exclude-standard
    )

    if ($LASTEXITCODE -ne 0) {
        throw "git ls-files завершился с ошибкой."
    }

    return @(
        $files.Values |
        Sort-Object
    )
}

function Test-IsGlobalBuildFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $path = (
        $RelativePath -replace '\\', '/'
    ).ToLowerInvariant()

    $globalFiles = @(
        "directory.build.props",
        "directory.build.targets",
        "directory.packages.props",
        "global.json",
        "nuget.config",
        "cs2zombieplague.sln",
        "scripts/package.ps1",
        "scripts/build-package.ps1",
        "scripts/build-affected.ps1",
        "scripts/clean.ps1"
    )

    if ($globalFiles -contains $path) {
        return $true
    }

    # Любой общий props/targets вне конкретного проекта
    # потенциально способен изменить весь build-граф.
    if (
        $path.EndsWith(".props") -or
        $path.EndsWith(".targets")
    ) {
        return $true
    }

    return $false
}

function Find-OwnerProject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Projects
    )

    $absolutePath = [System.IO.Path]::GetFullPath(
        (
            Join-Path `
                $root `
                (Convert-ToPlatformPath $RelativePath)
        )
    )

    $bestProject = $null
    $bestLength = -1

    foreach ($project in $Projects) {
        $directory = (
            [System.IO.Path]::GetFullPath(
                (Split-Path -Parent $project)
            )
        ).TrimEnd(
            [System.IO.Path]::DirectorySeparatorChar
        )

        $prefix = (
            $directory +
            [System.IO.Path]::DirectorySeparatorChar
        )

        $projectFile = [System.IO.Path]::GetFullPath(
            $project
        )

        $isProjectFile = $absolutePath.Equals(
            $projectFile,
            [System.StringComparison]::OrdinalIgnoreCase
        )

        $isInsideProject = $absolutePath.StartsWith(
            $prefix,
            [System.StringComparison]::OrdinalIgnoreCase
        )

        if (
            ($isProjectFile -or $isInsideProject) -and
            $directory.Length -gt $bestLength
        ) {
            $bestProject = $project
            $bestLength = $directory.Length
        }
    }

    return $bestProject
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Elysium affected build" -ForegroundColor Cyan
Write-Host " Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Push-Location $root

try {
    & git rev-parse `
        --is-inside-work-tree `
        *> $null

    if ($LASTEXITCODE -ne 0) {
        throw "Текущая директория не является Git-репозиторием."
    }

    $solutionProjects = @(
        Get-SolutionProjectPaths $solutionPath
    )

    $runtimePluginProjects = @(
        $solutionProjects |
        Where-Object {
            Test-IsSwiftlyPlugin $_
        }
    )

    if ($runtimePluginProjects.Count -eq 0) {
        throw "Runtime-плагины не найдены."
    }

    $projectMap = @{}

    foreach ($project in $solutionProjects) {
        $projectMap[
            (Get-ProjectKey $project)
        ] = $project
    }

    # Строим обратный граф:
    #
    # dependency -> projects which depend on it
    $reverseDependencies = @{}

    foreach ($project in $solutionProjects) {
        foreach (
            $reference in Get-ProjectReferencePaths $project
        ) {
            $referenceKey = Get-ProjectKey $reference

            if (
                -not $reverseDependencies.ContainsKey(
                    $referenceKey
                )
            ) {
                $reverseDependencies[
                    $referenceKey
                ] = @()
            }

            $reverseDependencies[$referenceKey] +=
                $project
        }
    }

    $changedFiles = @(
        Get-ChangedFiles
    )

    if ($changedFiles.Count -eq 0) {
        Write-Host (
            "Изменений для affected-сборки не найдено."
        ) -ForegroundColor Green

        return
    }

    Write-Host "Изменённые файлы:" -ForegroundColor Cyan

    foreach ($file in $changedFiles) {
        Write-Host "  - $file"
    }

    Write-Host ""

    $forceAll = $false
    $changedProjects = @{}

    foreach ($file in $changedFiles) {
        if (Test-IsGlobalBuildFile $file) {
            Write-Host (
                "Глобальный build-файл изменён: $file"
            ) -ForegroundColor Yellow

            $forceAll = $true
            continue
        }

        $owner = Find-OwnerProject `
            -RelativePath $file `
            -Projects $solutionProjects

        if ($null -ne $owner) {
            $changedProjects[
                (Get-ProjectKey $owner)
            ] = $owner

            continue
        }

        # Если изменён неизвестный csproj,
        # лучше перестраховаться.
        if (
            $file.EndsWith(
                ".csproj",
                [System.StringComparison]::OrdinalIgnoreCase
            )
        ) {
            Write-Host (
                "Не удалось сопоставить изменённый csproj: $file"
            ) -ForegroundColor Yellow

            $forceAll = $true
        }
    }

    $runtimePluginMap = @{}

    foreach ($project in $runtimePluginProjects) {
        $runtimePluginMap[
            (Get-ProjectKey $project)
        ] = $project
    }

    $affectedRuntimePlugins = @{}

    if ($forceAll) {
        foreach ($project in $runtimePluginProjects) {
            $affectedRuntimePlugins[
                (Get-ProjectKey $project)
            ] = $project
        }
    }
    else {
        # Идём вверх по обратному графу от каждого
        # непосредственно изменённого проекта.
        $visited = @{}

        $queue = New-Object `
            "System.Collections.Generic.Queue[string]"

        foreach ($project in $changedProjects.Values) {
            $queue.Enqueue($project)
        }

        while ($queue.Count -gt 0) {
            $current = $queue.Dequeue()
            $currentKey = Get-ProjectKey $current

            if ($visited.ContainsKey($currentKey)) {
                continue
            }

            $visited[$currentKey] = $true

            if ($runtimePluginMap.ContainsKey($currentKey)) {
                $affectedRuntimePlugins[
                    $currentKey
                ] = $runtimePluginMap[$currentKey]
            }

            if (
                $reverseDependencies.ContainsKey(
                    $currentKey
                )
            ) {
                foreach (
                    $dependent in
                    $reverseDependencies[$currentKey]
                ) {
                    $queue.Enqueue($dependent)
                }
            }
        }
    }

    if ($changedProjects.Count -gt 0) {
        Write-Host "Изменённые проекты:" -ForegroundColor Cyan

        foreach (
            $project in @(
                $changedProjects.Values |
                Sort-Object
            )
        ) {
            Write-Host "  - $(Get-ProjectName $project)"
        }

        Write-Host ""
    }

    if ($affectedRuntimePlugins.Count -eq 0) {
        Write-Host (
            "Изменения не затрагивают runtime-плагины."
        ) -ForegroundColor Green

        return
    }

    # Проверяем, существует ли полная базовая runtime-сборка.
    $configurationPluginsRoot = Join-Path `
        (Join-Path (Join-Path $root "dist") $Configuration) `
        "plugins"

    $baselineComplete = $true

    foreach ($project in $runtimePluginProjects) {
        $name = Get-ProjectName $project

        $runtimeDirectory = Join-Path `
            $configurationPluginsRoot `
            $name

        if (-not (Test-Path -LiteralPath $runtimeDirectory)) {
            $baselineComplete = $false
            break
        }
    }

    if (-not $baselineComplete) {
        Write-Host (
            "Полная runtime-база $Configuration отсутствует. " +
            "Первый affected-build соберёт все плагины."
        ) -ForegroundColor Yellow

        $affectedRuntimePlugins = @{}

        foreach ($project in $runtimePluginProjects) {
            $affectedRuntimePlugins[
                (Get-ProjectKey $project)
            ] = $project
        }
    }

    $affectedProjectsSorted = @(
        $affectedRuntimePlugins.Values |
        Sort-Object {
            Get-ProjectName $_
        }
    )

    Write-Host ""
    Write-Host "Потенциально затронутые плагины:" -ForegroundColor Yellow

    foreach ($project in $affectedProjectsSorted) {
        Write-Host "  - $(Get-ProjectName $project)"
    }

    Write-Host ""

    Write-Host "Собираю affected-плагины..." -ForegroundColor Cyan
    Write-Host ""

    foreach ($project in $affectedProjectsSorted) {
        $name = Get-ProjectName $project

        Write-Host "Build: $name" -ForegroundColor Yellow

        & dotnet build `
            $project `
            --configuration $Configuration

        if ($LASTEXITCODE -ne 0) {
            throw "Build завершился с ошибкой: $name"
        }

        Write-Host ""
    }

    $affectedPluginNames = @(
        $affectedProjectsSorted |
        ForEach-Object {
            Get-ProjectName $_
        }
    )

    $allAffected =
        $affectedPluginNames.Count -eq
        $runtimePluginProjects.Count

    Write-Host "Формирую runtime..." -ForegroundColor Cyan
    Write-Host ""

    if ($allAffected -or -not $baselineComplete) {
        # Полный runtime package.
        & $packageScript `
            -Configuration $Configuration
    }
    else {
        # Обновляем только affected-плагины.
        & $packageScript `
            -Configuration $Configuration `
            -Plugins $affectedPluginNames
    }

    if (-not $?) {
        throw "package.ps1 завершился с ошибкой."
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host " Affected package готов" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green

    Write-Host ""

    Write-Host "Runtime:" -ForegroundColor Cyan
    Write-Host (
        Join-Path `
            (Join-Path (Join-Path $root "dist") $Configuration) `
            "plugins"
    )
}
finally {
    Pop-Location
}