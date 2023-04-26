$publishDir = "$PSScriptRoot\publish"

if (Test-Path -Path $publishDir) {
	Remove-Item $publishDir\* -Recurse -Force
}

dotnet build -c Debug
dotnet pack --no-build -c Debug
dotnet publish --no-build -c Debug

dotnet build -c Release
dotnet pack --no-build -c Release
dotnet publish --no-build -c Release
