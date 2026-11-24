# 🔄 FileForge
> **Ultra-Fast, Low-Memory Universal File Converter & Format Editor for Windows**  
> *Targeted for .NET 10 • C# 14 • Python 3.14 • Windows 11 Fluent Design*

[![OS - Windows 11](https://img.shields.io/badge/OS-Windows%2011%20x64-0078D4?logo=windows)](https://microsoft.com)
[![Runtime - .NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Language - C# 14](https://img.shields.io/badge/Language-C%23%2014-239120?logo=csharp)](https://docs.microsoft.com/dotnet/csharp/)
[![Python - 3.14](https://img.shields.io/badge/Python-3.14-3776AB?logo=python)](https://python.org)
[![Author - 0-Name-Alex](https://img.shields.io/badge/Author-0--Name--Alex-orange?logo=github)](https://github.com/0-Name-Alex)

---

## ⚡ Highlights & Key Capabilities

- **📁 High-Throughput Batch Processing**: Queue dozens or hundreds of files and entire directories at once. Uses parallel bounded worker channels (`System.Threading.Channels`) for non-blocking concurrent conversions.
- **🔍 Magic-Byte Format Identification**: Inspects file header signatures (magic bytes) to detect genuine file types even if extensions are missing, scrambled, or altered.
- **📑 Full PowerPoint Support**: Transcode `.pptx`, `.ppt`, and `.ppsx` presentations into high-fidelity vector `.pdf`, `.docx`, `.html`, or extract full slide notes and text into `.txt` and `.md`.
- **📑 PDF Power Tools (Merge & Split)**:
  - **Consolidated PDF Merger**: Combine mixed queues of PDFs, images (`PNG`, `JPG`, `WEBP`), and presentations (`PPTX`) into a single document with the dedicated `[ 📑 Merge to PDF ]` button.
  - **Page Extractor / Splitter**: Break multi-page PDFs into separate page-by-page PDF documents or render pages directly into crisp images (`PNG`, `JPG`, `WEBP`) using `split-pdf` or `split-png/jpg/webp`.
- **🔎 Searchable Format Browser**: Instant search and category chips (`[Documents & Slides]`, `[Images]`, `[Data & Sheets]`, `[Archives]`, `[Developer & Hashes]`) to find and assign output formats in seconds.
- **⌨️ Instant Global Hotkey (`Ctrl + Alt + F`)**: Summon a minimalist floating quick-drop target from anywhere in Windows to convert files without having the main window open.
- **🌱 Ultra-Low Memory Governor (< 1.5–25 MB)**: Integrated Win32 `SetProcessWorkingSetSize` optimizer and stream chunking automatically trims memory after batch runs and when minimized to the notification tray.
- **🎨 Windows 11 Fluent UI**: Clean dark/light adaptive aesthetics matching the TaskHub design language, using `Segoe Fluent Icons`, card containers, and rounded corners.

---

## 🔄 Supported Format Conversion Matrix

| Category | Input Formats | Target Output Formats | Processing Engine |
|---|---|---|---|
| **📄 Presentations** | `PPTX`, `PPT`, `PPSX` | `PDF`, `TXT`, `DOCX`, `HTML`, `MD` | PowerPoint Headless COM / `python-pptx` |
| **🖼️ Images** | `PNG`, `JPG`, `WEBP`, `BMP`, `ICO`, `TIFF`, `GIF` | `PNG`, `JPG`, `WEBP`, `BMP`, `ICO`, `PDF`, `TIFF` | Pillow / Native Stream Engine |
| **📑 Documents & Text**| `DOCX`, `PDF`, `TXT`, `MD`, `HTML` | `PDF`, `DOCX`, `TXT`, `MD`, `HTML`, `split-pdf`, `split-png`, `split-jpg`, `split-webp` | `pypdf`, `pypdfium2`, `pdfplumber`, `python-docx` |
| **📊 Spreadsheets** | `CSV`, `XLSX`, `TSV` | `CSV`, `XLSX`, `JSON`, `HTML Table` | `openpyxl` / Native Delimiter Engine |
| **💻 Data & Config** | `JSON`, `XML`, `YAML`, `CSV` | `JSON`, `XML`, `YAML`, `CSV`, `TXT` | Native .NET 10 System.Text.Json / LINQ to XML |
| **📦 Archives** | `ZIP`, `TAR`, `GZ`, Folders | `ZIP`, `TAR`, `GZ`, Unpacked Folder | Native System.IO.Compression / TarFile |
| **🔒 Developer / Hashes**| Any File, `Base64`, `Hex`, `Text` | `Base64`, `Hex`, `SHA-256`, `MD5` | Native System.Security.Cryptography |

---

## ⌨️ Shortcut Reference

| Shortcut | Scope | Action |
|---|---|---|
| `Ctrl + Alt + F` | **System-Wide (Global)** | Toggle floating quick-drop converter overlay |
| `Ctrl + O` | Application | Open file picker to add multiple files |
| `Ctrl + D` | Application | Open folder picker to add directory of files |
| `Esc` | Dialogs / Overlays | Dismiss search format picker or quick launcher |
| `Enter` | Format Picker | Confirm and apply selected format |

---

## 📂 Architecture Overview

```
FileForge/
├── src/
│   ├── FileForge.Core/                 # Pure conversion contracts, stream pipelines
│   │   ├── Detection/FormatDetector.cs # File signatures & magic-byte scanner
│   │   ├── Converters/                 # Data, Archive, Dev, Presentation & Python converters
│   │   ├── Registry/ConverterRegistry  # Dynamic type discovery & routing
│   │   └── Pipeline/BatchPipeline.cs   # Channel-based parallel worker engine
│   │
│   ├── FileForge.Platform.Windows/     # Reusable Win32 interop
│   │   ├── Hotkeys/WindowsHotkeyService# Win32 RegisterHotKey via HWND_MESSAGE
│   │   ├── Memory/MemoryOptimizer.cs   # SetProcessWorkingSetSize & GC compacting
│   │   └── Tray/SystemTrayService.cs   # Notification area icon & context menu
│   │
│   ├── FileForge.Adapters.Python/      # Lightweight on-demand Python CLI bridges
│   │   └── converter_cli.py            # Pillow, Docx, PDF, PPTX, OpenPyXL worker
│   │
│   ├── FileForge.Cli/                  # Automated verification test suite
│   │   └── Program.cs
│   │
│   └── FileForge.App/                  # Modern WPF Application (.NET 10)
│       ├── MainWindow.xaml             # Fluent batch queue interface
│       ├── QuickLauncherWindow.xaml    # Floating drop overlay (Ctrl+Alt+F)
│       ├── FormatPickerDialog.xaml     # Searchable format selector modal
│       └── ViewModels/                 # Decoupled MVVM bindings & commands
│
├── publish/                            # Standalone compiled release binaries
├── build.cmd                           # Release build script
├── run.cmd                             # Quick launch helper
└── publish_to_github.cmd               # One-click GitHub repository creator
```

---

## 🚀 Quick Start & Build

### 1. Run the Application
Double click **`run.cmd`** or launch via terminal:
```powershell
dotnet run --project src/FileForge.App/FileForge.App.csproj
```

### 2. Run the Verification Suite
Execute the headless test harness that verifies multi-file conversions, channels, and memory trimming:
```powershell
dotnet run --project src/FileForge.Cli/FileForge.Cli.csproj
```

### 3. Build Release Package
```powershell
build.cmd
```
Stand-alone binaries will be compiled to `publish\FileForge.App.exe`.

### 4. Publish to GitHub
Run **`publish_to_github.cmd`** to automatically create `0-Name-Alex/FileForge` on GitHub and push the repository.

---

## 👤 Author
**Dhayananth J (`0-Name-Alex`)**  
- Portfolio: [dhayananth.me](https://dhayananth.me)  
- GitHub: [@0-Name-Alex](https://github.com/0-Name-Alex)
