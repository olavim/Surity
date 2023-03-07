$releaseDir = "$PSScriptRoot\release"

if (Test-Path -Path $releaseDir) {
	Remove-Item $releaseDir\* -Recurse -Force
}

dotnet build -c Release
dotnet pack --no-build -c Release
dotnet publish --no-build -c Release
