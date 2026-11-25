# FileForge — AI Agent Guide

High-performance, low-memory universal file converter and document tool for Windows 11 (**.NET 10 / C# 14 / WPF + lazy Python 3.14 worker**).

## 1. Solution Structure & Routing Order
- **`FileForge.Core`** (`net10.0`): Core contracts (`IFileConverter`, `IBatchPipeline`), models (`ConversionModels.cs`, `FileTypeInfo.cs`), [`FormatDetector.cs`](src/FileForge.Core/Detection/FormatDetector.cs), [`BatchPipeline.cs`](src/FileForge.Core/Pipeline/BatchPipeline.cs) (`System.Threading.Channels`), and [`ConverterRegistry.cs`](src/FileForge.Core/Registry/ConverterRegistry.cs).
- **`ConverterRegistry` Priority (IMPORTANT)**: Native C# converters are registered **before** Python fallback (`DataConverter` -> `ArchiveConverter` -> `DeveloperConverter` -> `PresentationConverter` -> `PdfToolsConverter` -> `PythonBridgeConverter`). Always keep zero-dependency C# converters first so CSV/JSON/XML/ZIP/TAR/Base64/Hex/SHA256 stay fast and low-memory.
- **`FileForge.Adapters.Python`** ([`converter_cli.py`](src/FileForge.Adapters.Python/converter_cli.py)): Invoked on-demand via `py -3.14` for Pillow image processing, `pypdf` / `pypdfium2` PDF Merge & Split, Excel (`openpyxl`), Word (`python-docx`), and PowerPoint COM conversion—and exits immediately after completion to release RAM.
- **`FileForge.Platform.Windows`** (`net10.0-windows`): Win32 hotkey (`Ctrl+Alt+F` via `WindowsHotkeyService`), system tray (`SystemTrayService`), [`MemoryOptimizer.cs`](src/FileForge.Platform.Windows/Memory/MemoryOptimizer.cs), and [`SingleInstanceService.cs`](src/FileForge.Platform.Windows/SingleInstance/SingleInstanceService.cs) (mutex + named-pipe forwarding so duplicate launches bring the active window forward and queue passed paths).
- **`FileForge.App`** (`net10.0-windows`): Fluent WPF UI (`MainWindow`, `QuickLauncherWindow`, `FormatPickerDialog`). [`App.xaml.cs`](src/FileForge.App/App.xaml.cs) inspects `e.Args` at startup and queues any passed file/folder paths via `MainViewModel.AddPaths(...)` (used by Explorer right-click / Send To integration).

## 2. Build, Test & Packaging Commands
- **Unit tests (xUnit, pure .NET)**: `dotnet test FileForge.slnx -c Release` (runs `tests/FileForge.Tests`, 33 tests covering magic-byte detection, registry priority, data/developer/archive converters, and batch pipeline cancellation/errors).
- **End-to-end CLI harness (requires Python 3.14 deps)**: `dotnet run --project src/FileForge.Cli/FileForge.Cli.csproj`
- **Release build**: `.\build.cmd` or `dotnet build FileForge.slnx -c Release`.
- **Publish standalone bundle**: `dotnet publish src/FileForge.App/FileForge.App.csproj -c Release -o publish`
- **Windows per-user installer**: [`installer/install.cmd`](installer/install.cmd) / [`installer/install.ps1`](installer/install.ps1) installs to `%LOCALAPPDATA%\Programs\FileForge`, creates shortcuts, registers Explorer context menu (`HKCU\Software\Classes\*\shell\FileForge`), and writes Windows uninstall metadata.
- **Inno Setup script**: [`installer/FileForge.iss`](installer/FileForge.iss) (compiled automatically in CI on `v*` tags).

## 3. Key Invariants & Gotchas
- **Magic-byte detection**: `FormatDetector.DetectFromFile` reads file headers (`PNG`, `JPG`, `GIF`, `WEBP`, `BMP`, `TIFF`, `PDF`, `GZ`, `7Z`, `ZIP`/OOXML) before falling back to extensions. For `PK..` headers, `.docx`/`.xlsx`/`.pptx`/`.ppsx` extensions disambiguate OOXML from plain `.zip`.
- **Memory discipline**: Call `MemoryOptimizer.TrimWorkingSet()` after batch jobs complete and when hiding to tray; never keep persistent background Python processes alive.
- **Design blueprint**: See [`OUTLOOK.md`](OUTLOOK.md) for original UI/architecture design context.
- **Git & CI**: `0-Name-Alex`, branch `main`; CI ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)) runs `FileForge.Tests` on `windows-latest` and builds release ZIP + `Setup.exe` on `v*` tags.
