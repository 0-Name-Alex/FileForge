@echo off
echo ================================================
echo   Building FileForge Solution (.NET 10 x64)
echo ================================================
dotnet build FileForge.slnx -c Release
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Build failed!
    pause
    exit /b %ERRORLEVEL%
)
echo [SUCCESS] Build completed successfully.
pause
