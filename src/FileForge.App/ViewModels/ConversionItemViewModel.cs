using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using FileForge.Core.Detection;
using FileForge.Core.Models;
using FileForge.Core.Registry;

namespace FileForge.App.ViewModels;

public sealed class ConversionItemViewModel : ObservableObject
{
    private string _targetFormat = string.Empty;
    private ConversionStatus _status = ConversionStatus.Queued;
    private double _progress;
    private string _statusMessage = "Ready";
    private string? _errorMessage;
    private bool _isSelected;

    public ConversionItem Model { get; }
    public string SourcePath => Model.SourcePath;
    public string FileName => Model.FileName;
    public string DetectedFormat { get; }
    public string FormattedSize { get; }
    public ObservableCollection<string> AvailableTargetFormats { get; } = [];

    public string TargetFormat
    {
        get => _targetFormat;
        set
        {
            if (SetProperty(ref _targetFormat, value))
            {
                Model.TargetFormat = value;
                UpdateDestinationPath();
            }
        }
    }

    public ConversionStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string DestinationPath => Model.DestinationPath;

    public ICommand OpenOutputFolderCommand { get; }

    public ConversionItemViewModel(string sourcePath, ConverterRegistry registry, string? defaultOutputFolder = null)
    {
        var info = FormatDetector.DetectFromFile(sourcePath);
        DetectedFormat = info.Name;

        long bytes = 0;
        if (File.Exists(sourcePath))
        {
            bytes = new FileInfo(sourcePath).Length;
        }
        FormattedSize = FormatBytes(bytes);

        Model = new ConversionItem
        {
            SourcePath = sourcePath,
            SourceFormat = info.Extension,
            FileSizeBytes = bytes
        };

        var available = registry.GetAvailableTargetFormats(info.Extension);
        foreach (var fmt in available)
        {
            AvailableTargetFormats.Add(fmt);
        }

        _targetFormat = AvailableTargetFormats.FirstOrDefault() ?? "txt";
        Model.TargetFormat = _targetFormat;

        SetOutputFolder(defaultOutputFolder);

        OpenOutputFolderCommand = new RelayCommand(() =>
        {
            if (File.Exists(Model.DestinationPath))
            {
                Process.Start("explorer.exe", $"/select,\"{Model.DestinationPath}\"");
            }
            else
            {
                var dir = Path.GetDirectoryName(Model.DestinationPath);
                if (Directory.Exists(dir)) Process.Start("explorer.exe", $"\"{dir}\"");
            }
        });
    }

    public void SetOutputFolder(string? folder)
    {
        var outDir = !string.IsNullOrEmpty(folder) && Directory.Exists(folder)
            ? folder
            : Path.GetDirectoryName(SourcePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        var nameWithoutExt = Path.GetFileNameWithoutExtension(SourcePath);
        Model.DestinationPath = Path.Combine(outDir, $"{nameWithoutExt}.{TargetFormat.TrimStart('.')}");
        OnPropertyChanged(nameof(DestinationPath));
    }

    private void UpdateDestinationPath()
    {
        var dir = Path.GetDirectoryName(Model.DestinationPath);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(SourcePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Model.DestinationPath = Path.Combine(dir, $"{nameWithoutExt}.{TargetFormat.TrimStart('.')}");
            OnPropertyChanged(nameof(DestinationPath));
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB"];
        int i = 0;
        double d = bytes;
        while (d >= 1024 && i < suffixes.Length - 1)
        {
            d /= 1024;
            i++;
        }
        return $"{d:0.##} {suffixes[i]}";
    }
}
