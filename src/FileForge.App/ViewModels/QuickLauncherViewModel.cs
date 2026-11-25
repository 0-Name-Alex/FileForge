using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using FileForge.Core.Models;
using FileForge.Core.Pipeline;
using FileForge.Core.Registry;
using FileForge.Platform.Windows.Memory;

namespace FileForge.App.ViewModels;

public sealed class QuickLauncherViewModel : ObservableObject
{
    private readonly ConverterRegistry _registry;
    private readonly BatchPipeline _pipeline;
    private readonly MainViewModel _mainVm;
    private readonly Action? _openMainApp;
    private string _selectedTargetFormat = "webp";
    private string _statusMessage = "Drop files here to quickly convert";
    private bool _isConverting;

    public ObservableCollection<string> TargetFormats { get; } = ["webp", "png", "jpg", "pdf", "docx", "xlsx", "csv", "json", "xml", "zip", "b64"];

    public string SelectedTargetFormat
    {
        get => _selectedTargetFormat;
        set => SetProperty(ref _selectedTargetFormat, value);
    }

    public string OutputDirectory => _mainVm.OutputDirectory;

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsConverting
    {
        get => _isConverting;
        set => SetProperty(ref _isConverting, value);
    }

    public ICommand QuickConvertFilesCommand { get; }
    public ICommand OpenMainAppCommand { get; }
    public ICommand ChangeOutputFolderCommand { get; }

    public QuickLauncherViewModel(ConverterRegistry registry, BatchPipeline pipeline, MainViewModel mainVm, Action? openMainApp = null)
    {
        _registry = registry;
        _pipeline = pipeline;
        _mainVm = mainVm;
        _openMainApp = openMainApp;

        _mainVm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.OutputDirectory))
            {
                OnPropertyChanged(nameof(OutputDirectory));
            }
        };

        QuickConvertFilesCommand = new RelayCommand(async obj =>
        {
            if (obj is IEnumerable<string> filePaths)
            {
                await ConvertQuickFilesAsync(filePaths);
            }
        });

        OpenMainAppCommand = new RelayCommand(() => _openMainApp?.Invoke());
        ChangeOutputFolderCommand = new RelayCommand(() =>
        {
            if (_mainVm.ChangeOutputFolderCommand.CanExecute(null))
                _mainVm.ChangeOutputFolderCommand.Execute(null);
        });
    }

    public async Task ConvertQuickFilesAsync(IEnumerable<string> filePaths)
    {
        var files = filePaths.Where(File.Exists).ToList();
        if (files.Count == 0) return;

        IsConverting = true;
        StatusMessage = $"Converting {files.Count} files to .{SelectedTargetFormat}...";

        var outDir = _mainVm.OutputDirectory;
        Directory.CreateDirectory(outDir);

        var items = files.Select(f =>
        {
            var srcExt = Path.GetExtension(f).TrimStart('.').ToLowerInvariant();
            var nameWithoutExt = Path.GetFileNameWithoutExtension(f);
            return new ConversionItem
            {
                SourcePath = f,
                SourceFormat = srcExt,
                TargetFormat = SelectedTargetFormat,
                DestinationPath = Path.Combine(outDir, $"{nameWithoutExt}.{SelectedTargetFormat.TrimStart('.')}")
            };
        }).ToList();

        var results = await _pipeline.ExecuteBatchAsync(items);
        var successCount = results.Count(r => r.Success);
        var failedCount = results.Count - successCount;

        IsConverting = false;
        if (failedCount > 0)
        {
            var firstError = results.FirstOrDefault(r => !r.Success && !string.IsNullOrWhiteSpace(r.ErrorMessage))?.ErrorMessage
                             ?? "Conversion failed";
            StatusMessage = $"Failed ({failedCount}/{files.Count}): {firstError}";
        }
        else
        {
            StatusMessage = $"Converted {successCount}/{files.Count} files to: {outDir}";
        }

        MemoryOptimizer.TrimWorkingSet();
    }
}
