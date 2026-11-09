# Builds the Windows x64 app into .\out\Windows-x64 (run from the repository root).
param([string]$Output = "out/Windows-x64")
$ErrorActionPreference = "Stop"
dotnet build Ghostbox.App/Ghostbox.App.csproj -p:GhostboxPlatform=Windows -c Release -o $Output
Write-Host "Windows app: $Output/Ghostbox.App.exe"
