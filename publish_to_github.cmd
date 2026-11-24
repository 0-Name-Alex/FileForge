@echo off
echo ========================================================
echo   Publishing FileForge to GitHub (0-Name-Alex)
echo ========================================================
gh repo create 0-Name-Alex/FileForge --public --source=. --remote=origin --push --description "High-performance Windows file converter with batch processing, low memory governor, and global hotkey access."
if %ERRORLEVEL% EQU 0 (
    echo.
    echo [SUCCESS] Repository created and pushed to https://github.com/0-Name-Alex/FileForge
) else (
    echo.
    echo [INFO] Attempting git push to origin main...
    git push -u origin main
)
pause
