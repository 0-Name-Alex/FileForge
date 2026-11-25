<#
.SYNOPSIS
    Uninstalls FileForge and cleans up shortcuts and Windows Explorer integration.
#>
$ErrorActionPreference = 'Continue'

$InstallDir = Join-Path $env:LOCALAPPDATA 'Programs\FileForge'

Write-Host 'Stopping FileForge if running...' -ForegroundColor Cyan
Get-Process -Name 'FileForge.App' -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host 'Removing shortcuts and Explorer context menu entries...' -ForegroundColor Cyan
$StartMenuDir = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\FileForge'
if (Test-Path $StartMenuDir) { Remove-Item $StartMenuDir -Recurse -Force }

$SendToLink = Join-Path $env:APPDATA 'Microsoft\Windows\SendTo\FileForge.lnk'
if (Test-Path $SendToLink) { Remove-Item $SendToLink -Force }

$DesktopLink = Join-Path ([Environment]::GetFolderPath('Desktop')) 'FileForge.lnk'
if (Test-Path $DesktopLink) { Remove-Item $DesktopLink -Force }

foreach ($key in @(
    'HKCU:\Software\Classes\*\shell\FileForge',
    'HKCU:\Software\Classes\Directory\shell\FileForge',
    'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\FileForge'
)) {
    if (Test-Path $key) { Remove-Item $key -Recurse -Force }
}

if (Test-Path $InstallDir) {
    Write-Host "Removing $InstallDir..." -ForegroundColor Cyan
    Start-Process -FilePath 'cmd.exe' -ArgumentList "/c timeout /t 2 /nobreak >nul & rmdir /s /q `"$InstallDir`"" -WindowStyle Hidden
}

Write-Host '[OK] FileForge uninstalled.' -ForegroundColor Green
