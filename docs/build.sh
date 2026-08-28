#!/usr/bin/env bash

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

dotnet tool restore
dotnet run --project tools/EventDocsGenerator/EventDocsGenerator.csproj --configuration Release -- generate
dotnet restore CS2ZombiePlague.sln

if [[ "${1:-}" == "--serve" ]]; then
    dotnet docfx docfx.json --serve
else
    dotnet docfx docfx.json
fi
