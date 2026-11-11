[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$packageRoot = Split-Path -Parent $PSScriptRoot
$sourceApp = Join-Path $packageRoot 'Application'
$sourceExe = Join-Path $sourceApp 'Ghostbox.App.exe'
$requiredFiles = @(
    'Ghostbox.App.exe',
    'Ghostbox.App.dll',
    'Ghostbox.App.runtimeconfig.json',
    'Ghostbox.App.deps.json',
    'Microsoft.WindowsAppRuntime.Bootstrap.dll',
    'Microsoft.UI.Xaml.dll',
    'WebView2Loader.dll'
)

function Stop-WithError {
    param([string]$Message)
    Write-Host ''
    Write-Host "ERROR: $Message" -ForegroundColor Red
    exit 1
}

try {
    Write-Host 'Checking the installation package...'
    foreach ($file in $requiredFiles) {
        if (-not (Test-Path -LiteralPath (Join-Path $sourceApp $file) -PathType Leaf)) {
            Stop-WithError "Required file is missing: Application\$file. Extract the complete ZIP and try again."
        }
    }

    if ($env:WOTS_INSTALL_DIR) {
        $installDir = $env:WOTS_INSTALL_DIR
    }
    else {
        $installBase = Join-Path $env:LOCALAPPDATA 'Whispers of the Spirit'
        $preferredDir = Join-Path $installBase 'App-2.3'
        $installDir = $preferredDir
        if (Test-Path -LiteralPath $preferredDir) {
            $installDir = Join-Path $installBase ('App-2.3-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
            Write-Host 'An existing installation was found and will be left unchanged.'
        }
    }

    Write-Host "Installing for the current user to: $installDir"
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null

    # Copy only. Deliberately do not mirror, purge, or delete destination files.
    $copyArgs = @(
        $sourceApp,
        $installDir,
        '/E',
        '/COPY:DAT',
        '/DCOPY:DAT',
        '/R:2',
        '/W:1',
        '/XJ',
        '/NP',
        '/NFL',
        '/NDL'
    )
    & robocopy.exe @copyArgs | Out-Host
    $copyCode = $LASTEXITCODE
    if ($copyCode -ge 8) {
        Stop-WithError "Windows reported a file-copy error (code $copyCode)."
    }

    $installedExe = Join-Path $installDir 'Ghostbox.App.exe'
    if (-not (Test-Path -LiteralPath $installedExe -PathType Leaf)) {
        Stop-WithError 'The application executable was not present after copying.'
    }

    if ($env:WOTS_SKIP_SHORTCUT -ne '1') {
        $desktop = [Environment]::GetFolderPath('DesktopDirectory')
        if ([string]::IsNullOrWhiteSpace($desktop)) {
            Stop-WithError 'Windows could not locate the current user desktop.'
        }
        $shortcutPath = Join-Path $desktop 'Whispers of the Spirit.lnk'
        $shell = New-Object -ComObject WScript.Shell
        $shortcut = $shell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = $installedExe
        $shortcut.WorkingDirectory = $installDir
        $shortcut.IconLocation = "$installedExe,0"
        $shortcut.Description = 'Whispers of the Spirit 2.3'
        $shortcut.Save()
        Write-Host "Desktop shortcut created: $shortcutPath"
    }
    else {
        Write-Host 'Shortcut creation skipped for package testing.'
    }

    Write-Host 'Launching Whispers of the Spirit...'
    Start-Process -FilePath $installedExe -WorkingDirectory $installDir
    Write-Host 'The installer never deletes existing recordings or user data.'
    exit 0
}
catch {
    Stop-WithError $_.Exception.Message
}
