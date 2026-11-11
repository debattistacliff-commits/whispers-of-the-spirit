# Builds the Windows x64 app into .\out\Windows-x64 (run from the repository root).
#   -Installer   also makes out\Whispers-of-the-Spirit-<version>-Windows-x64-Installer.zip
#                (Install Whispers of the Spirit.bat + Application\ + support\, from scripts\windows-installer).
# The optional Xbox 360 Kinect helper is built first when the Kinect for Windows SDK 1.8 is installed;
# without it the app is the same, only without the Kinect choice in the SLS camera.
param([string]$Output = "out/Windows-x64", [switch]$Installer, [string]$Version = "2.3")
$ErrorActionPreference = "Stop"

$kinectSdk = Join-Path $env:ProgramFiles "Microsoft SDKs\Kinect\v1.8\Assemblies\Microsoft.Kinect.dll"
if (Test-Path $kinectSdk) {
    dotnet build Ghostbox.Kinect/Ghostbox.Kinect.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw "Kinect helper build failed." }
} else {
    Write-Host "Kinect for Windows SDK 1.8 not found: building without the optional Kinect helper."
}

dotnet build Ghostbox.App/Ghostbox.App.csproj -p:GhostboxPlatform=Windows -c Release -o $Output
if ($LASTEXITCODE -ne 0) { throw "Windows app build failed." }
Write-Host "Windows app: $Output/Ghostbox.App.exe"

if ($Installer) {
    $name = "Whispers-of-the-Spirit-$Version-Installer"
    $stage = Join-Path ([IO.Path]::GetTempPath()) ("wots-" + [Guid]::NewGuid().ToString("N"))
    $root = Join-Path $stage $name
    New-Item -ItemType Directory -Force (Join-Path $root "Application") | Out-Null
    Copy-Item -Recurse -Force (Join-Path $Output "*") (Join-Path $root "Application")
    Get-ChildItem (Join-Path $root "Application") -Recurse -Include *.pdb | Remove-Item -Force
    Copy-Item -Recurse -Force "scripts/windows-installer/*" $root
    $zip = Join-Path (Split-Path $Output -Parent) "Whispers-of-the-Spirit-$Version-Windows-x64-Installer.zip"
    if (Test-Path $zip) { Remove-Item $zip }
    # Windows' own tar writes a standard ZIP (forward-slash paths), which every unzip tool reads correctly.
    & "$env:SystemRoot\System32\tar.exe" -a -c -f $zip -C $stage $name
    if ($LASTEXITCODE -ne 0) { throw "Making the installer ZIP failed." }
    Remove-Item -Recurse -Force $stage
    Write-Host "Windows installer: $zip"
}
