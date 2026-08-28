param(
    [switch] $Serve
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

dotnet tool restore
dotnet run --project tools/EventDocsGenerator/EventDocsGenerator.csproj --configuration Release -- generate
dotnet restore CS2ZombiePlague.sln

if ($Serve) {
    dotnet docfx docfx.json --serve
} else {
    dotnet docfx docfx.json
}
