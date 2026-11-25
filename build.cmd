@echo off
echo ================================================
echo   Building & Publishing FileForge (.NET 10 x64)
echo ================================================
dotnet test FileForge.slnx -c Release
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Build or unit tests failed!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo Publishing self-contained standalone binary to publish\ ...
dotnet publish src\FileForge.App\FileForge.App.csproj -c Release -r win-x64 --self-contained ^
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Self-contained publish failed!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [SUCCESS] Standalone binary ready at publish\FileForge.App.exe (no .NET install required).
echo Tip: For a tiny ~320 KB build (requires .NET 10 Desktop Runtime), run:
echo      dotnet publish src\FileForge.App\FileForge.App.csproj -c Release -o publish-lite
pause
