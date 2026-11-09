# Builds the Android APK into .\out\Android (run from the repository root).
# Without a keystore the APK is signed with the local debug key; use your own release key for store uploads.
param(
  [Parameter(Mandatory = $true)][string]$AndroidSdk,
  [Parameter(Mandatory = $true)][string]$JavaSdk,
  [string]$Output = "out/Android"
)
$ErrorActionPreference = "Stop"
dotnet publish Ghostbox.App/Ghostbox.App.csproj -f net10.0-android -c Release -p:GhostboxPlatform=Android `
  -p:AndroidSdkDirectory=$AndroidSdk -p:JavaSdkDirectory=$JavaSdk -o $Output
Get-ChildItem $Output -Filter *.apk | ForEach-Object { Write-Host "Android APK: $($_.FullName)" }
