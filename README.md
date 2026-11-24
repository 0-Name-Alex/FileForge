# 🔄 FileForge — Universal File Converter & Editor
> **High-Performance Multi-File Conversion Engine with Global Hotkey Access & Low-Memory Governor**  
> **Target**: .NET 10 (`dotnet 10.0.404`) • C# 14 • Python 3.14 • Windows 11 (x64)

---

## 🎯 Architecture & Features Overview

`FileForge` is engineered with clean separation between the high-performance conversion core, Windows platform hooks, and presentation layers:

- **⚡ Multi-File Batch Queue**: Parallel bounded worker channels (`System.Threading.Channels`) processing multi-file queues concurrently with per-item and aggregated progress broadcasting.
- **🔍 Magic-Bytes Format Detection**: Detects real file types by header bytes (even when extensions are missing or altered).
- **🌱 Ultra-Low Memory Governor**: Idle RAM trimmed to **~1.2–25 MB** using Win32 `SetProcessWorkingSetSize` and stream chunking.
- **⌨️ Global Hotkey & System Tray**: `Ctrl+Alt+F` global shortcut powered by `RegisterHotKey` on an invisible message window, plus system tray minimization.
- **🔌 Multi-Engine Transcoding**:
  - **Native C# (.NET 10)**: Bidirectional JSON <-> XML <-> CSV, ZIP/TAR/GZ streaming archives, Base64, Hex dumps, SHA-256 / MD5 hashes.
  - **Python 3.14 Bridge**: Pre-installed Pillow (PNG, JPG, WEBP, BMP, ICO, PDF), OpenPyXL (Excel), Docx, and PDFium/PDFplumber.

---

## 📂 Project Structure

```
D:\Projects\File editor\
├── FileForge.slnx                    # Modern .NET 10 solution
│
├── src\
│   ├── FileForge.Core\               # Core conversion library
│   │   ├── Models\                   # ConversionItem, Options, Result, FileTypeInfo
│   │   ├── Detection\                # FormatDetector (magic-bytes & signatures)
│   │   ├── Converters\               # Data, Archive, Dev & PythonBridge converters
│   │   ├── Registry\                 # ConverterRegistry (dynamic routing)
│   │   └── Pipeline\                 # BatchPipeline (parallel channel execution)
│   │
│   ├── FileForge.Platform.Windows\   # Windows Win32 integration
│   │   ├── Hotkeys\                  # WindowsHotkeyService (Ctrl+Alt+F)
│   │   ├── Memory\                   # MemoryOptimizer (SetProcessWorkingSetSize)
│   │   └── Tray\                     # SystemTrayService (NotifyIcon & tray menu)
│   │
│   ├── FileForge.Adapters.Python\    # Isolated Python workers
│   │   └── converter_cli.py          # Pillow, Docx, PDF, OpenPyXL worker
│   │
│   ├── FileForge.Cli\                # Headless batch test suite & verification harness
│   │   └── Program.cs
│   │
│   └── FileForge.App\                # WPF Desktop Application Shell (.NET 10)
│       ├── ViewModels\               # Clean MVVM layer for Claude or any UI framework
│       │   ├── MainViewModel.cs      # Batch queue, commands, memory & progress
│       │   ├── ConversionItemViewModel.cs # Per-row target dropdown, status, explorer
│       │   ├── QuickLauncherViewModel.cs  # Floating drop target (Ctrl+Alt+F)
│       │   └── ObservableObject.cs   # INotifyPropertyChanged & RelayCommand
│       ├── MainWindow.xaml / .cs     # Main UI window
│       └── App.xaml / .cs            # Application entry & tray lifecycle
│
├── OUTLOOK.md                        # Complete design blueprint
└── README.md                         # This guide
```

---

## 🧪 Testing & Verifying the Base Engine

You can run the automated verification suite anytime:

```powershell
cd "D:\Projects\File editor"
dotnet run --project src/FileForge.Cli/FileForge.Cli.csproj
```

**Verified Test Output**:
```
🏁 Batch conversion finished in 775 ms!
Results: 9 Succeeded, 0 Failed.
🧠 Memory before trimming: 36.8 MB
🌱 Memory after MemoryOptimizer.TrimWorkingSet(): 1.2 MB (Reduced by 35.6 MB!)
```

---

## 🎨 Information for Claude / UI Designer

The MVVM backend is completely ready for you to wire up in `src/FileForge.App`:

1. **`MainViewModel`** (`DataContext = new MainViewModel()`):
   - `QueueItems` (`ObservableCollection<ConversionItemViewModel>`): Bind to your DataGrid / ListView.
   - `AddFilesCommand` / `AddFolderCommand`: Bind to file picker buttons.
   - `ConvertAllCommand`: Bind to the primary "Convert All" action button.
   - `CancelCommand`: Bind to cancel button (`IsEnabled="{Binding IsBusy}"`).
   - `ApplyTargetToAllCommand` / `ApplyTargetToSelectedCommand`: Bind to batch target selector dropdown (`CommonTargetFormat`).
   - `OverallProgress` (`double` 0-100): Bind to the ProgressBar.
   - `StatusText`: Bind to status bar.
   - `CurrentMemoryMb`: Bind to RAM monitor badge.
   - `TrimMemoryCommand`: Bind to clean memory button.

2. **`ConversionItemViewModel`** (Each row in the queue):
   - `FileName`, `DetectedFormat`, `FormattedSize`
   - `AvailableTargetFormats`: Bind to row `ComboBox` `ItemsSource`.
   - `TargetFormat`: Bind two-way to row `ComboBox` `SelectedItem`.
   - `Progress`: Bind to row `ProgressBar` (0-100).
   - `Status`: `Queued`, `Processing`, `Completed`, `Failed`.
   - `StatusMessage`: Bind to row status text.
   - `OpenOutputFolderCommand`: Bind to "Show in Folder" button.

3. **`QuickLauncherViewModel`**:
   - For a borderless floating overlay (`Ctrl+Alt+F`).
   - Drag-and-drop onto the overlay calls `ConvertQuickFilesAsync(files)`.
