<#
.SYNOPSIS
    Installs FileForge for the current Windows user without requiring administrator privileges.
.DESCRIPTION
    Copies or publishes FileForge binaries to %LOCALAPPDATA%\Programs\FileForge,
    creates Start Menu / Desktop / SendTo shortcuts, optionally registers the
    Windows Explorer right-click "Convert with FileForge" menu, and adds an
    entry to Windows Installed Apps (Add/Remove Programs).
#>
[CmdletBinding()]
param(
    [switch]$SkipDesktopIcon,
    [switch]$SkipContextMenu,
    [switch]$Rebuild
)

$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = if (Test-Path (Join-Path $ScriptDir '..\FileForge.slnx')) {
    Resolve-Path (Join-Path $ScriptDir '..')
} else {
    $ScriptDir
}

$PublishDir = if (Test-Path (Join-Path $RepoRoot 'publish\FileForge.App.exe')) {
    Join-Path $RepoRoot 'publish'
} elseif (Test-Path (Join-Path $ScriptDir 'FileForge.App.exe')) {
    $ScriptDir
} else {
    Join-Path $RepoRoot 'publish'
}

if ($Rebuild -or -not (Test-Path (Join-Path $PublishDir 'FileForge.App.exe'))) {
    $csproj = Join-Path $RepoRoot 'src\FileForge.App\FileForge.App.csproj'
    if (-not (Test-Path $csproj)) {
        throw "Could not find FileForge.App.exe in '$PublishDir' or project at '$csproj'."
    }
    Write-Host '[1/4] Publishing FileForge (win-x64)...' -ForegroundColor Cyan
    dotnet publish $csproj -c Release -r win-x64 --self-contained -o $PublishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }
}

$InstallDir = Join-Path $env:LOCALAPPDATA 'Programs\FileForge'
$ExePath = Join-Path $InstallDir 'FileForge.App.exe'
$IconPath = Join-Path $InstallDir 'assets\app_icon.ico'

Write-Host "[2/4] Installing files to $InstallDir..." -ForegroundColor Cyan
Get-Process -Name 'FileForge.App' -ErrorAction SilentlyContinue | Stop-Process -Force
New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
Copy-Item -Path (Join-Path $PublishDir '*') -Destination $InstallDir -Recurse -Force
Copy-Item -Path (Join-Path $ScriptDir 'uninstall.ps1') -Destination (Join-Path $InstallDir 'uninstall.ps1') -Force

function New-Shortcut([string]$ShortcutPath, [string]$TargetPath, [string]$WorkingDir, [string]$Icon) {
    $shell = New-Object -ComObject WScript.Shell
    $sc = $shell.CreateShortcut($ShortcutPath)
    $sc.TargetPath = $TargetPath
    $sc.WorkingDirectory = $WorkingDir
    if (Test-Path $Icon) { $sc.IconLocation = "$Icon,0" }
    $sc.Description = 'FileForge Universal File Converter'
    $sc.Save()
}

Write-Host '[3/4] Creating shortcuts...' -ForegroundColor Cyan
$StartMenuDir = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\FileForge'
New-Item -ItemType Directory -Path $StartMenuDir -Force | Out-Null
New-Shortcut (Join-Path $StartMenuDir 'FileForge.lnk') $ExePath $InstallDir $IconPath

$SendToLink = Join-Path $env:APPDATA 'Microsoft\Windows\SendTo\FileForge.lnk'
New-Shortcut $SendToLink $ExePath $InstallDir $IconPath

if (-not $SkipDesktopIcon) {
    $DesktopLink = Join-Path ([Environment]::GetFolderPath('Desktop')) 'FileForge.lnk'
    New-Shortcut $DesktopLink $ExePath $InstallDir $IconPath
}

Write-Host '[4/4] Registering Windows integration & uninstaller...' -ForegroundColor Cyan
if (-not $SkipContextMenu) {
    foreach ($rootKey in @('HKCU:\Software\Classes\*\shell\FileForge', 'HKCU:\Software\Classes\Directory\shell\FileForge')) {
        New-Item -Path $rootKey -Force | Out-Null
        Set-ItemProperty -Path $rootKey -Name '(default)' -Value 'Convert with FileForge'
        Set-ItemProperty -Path $rootKey -Name 'Icon' -Value "`"$ExePath`",0"
        $cmdKey = Join-Path $rootKey 'command'
        New-Item -Path $cmdKey -Force | Out-Null
        Set-ItemProperty -Path $cmdKey -Name '(default)' -Value "`"$ExePath`" `"%1`""
    }
}

$UninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\FileForge'
New-Item -Path $UninstallKey -Force | Out-Null
Set-ItemProperty -Path $UninstallKey -Name 'DisplayName' -Value 'FileForge'
Set-ItemProperty -Path $UninstallKey -Name 'DisplayVersion' -Value '1.1.0'
Set-ItemProperty -Path $UninstallKey -Name 'Publisher' -Value 'Dhayananth J (0-Name-Alex)'
Set-ItemProperty -Path $UninstallKey -Name 'InstallLocation' -Value $InstallDir
Set-ItemProperty -Path $UninstallKey -Name 'DisplayIcon' -Value $ExePath
Set-ItemProperty -Path $UninstallKey -Name 'UninstallString' -Value "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$InstallDir\uninstall.ps1`""
Set-ItemProperty -Path $UninstallKey -Name 'NoModify' -Value 1 -Type DWord
Set-ItemProperty -Path $UninstallKey -Name 'NoRepair' -Value 1 -Type DWord

Write-Host "`n[OK] FileForge installed cleanly!" -ForegroundColor Green
Write-Host "     Location : $InstallDir"
Write-Host "     Shortcut : Start Menu, SendTo, and Desktop"
Write-Host "     Explorer : Right-click any file or folder -> 'Convert with FileForge'"
