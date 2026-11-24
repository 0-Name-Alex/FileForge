using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using FileForge.Core.Interfaces;
using FileForge.Core.Models;
using FileForge.Core.Pipeline;
using FileForge.Core.Registry;
using FileForge.Platform.Windows.Memory;

namespace FileForge.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ConverterRegistry _registry;
    private readonly BatchPipeline _pipeline;

    private string _outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "FileForge_Output");
    private string _commonTargetFormat = "webp";
    private double _overallProgress;
    private string _statusText = "Ready to convert files. Drag and drop files or folders here.";
    private bool _isBusy;
    private double _currentMemoryMb;
    private int _totalItems;
    private int _processedItems;
    private int _succeededItems;
    private int _failedItems;

    public ObservableCollection<ConversionItemViewModel> QueueItems { get; } = [];
    public ObservableCollection<string> CommonTargetPresets { get; } = ["pdf", "webp", "png", "jpg", "docx", "xlsx", "csv", "json", "xml", "zip", "txt", "b64"];

    public string OutputDirectory
    {
        get => _outputDirectory;
        set => SetProperty(ref _outputDirectory, value);
    }

    public string CommonTargetFormat
    {
        get => _commonTargetFormat;
        set => SetProperty(ref _commonTargetFormat, value);
    }

    public double OverallProgress
    {
        get => _overallProgress;
        set => SetProperty(ref _overallProgress, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                (ConvertAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (CancelCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public double CurrentMemoryMb
    {
        get => _currentMemoryMb;
        set => SetProperty(ref _currentMemoryMb, value);
    }

    public int TotalItems
    {
        get => _totalItems;
        set => SetProperty(ref _totalItems, value);
    }

    public int ProcessedItems
    {
        get => _processedItems;
        set => SetProperty(ref _processedItems, value);
    }

    public int SucceededItems
    {
        get => _succeededItems;
        set => SetProperty(ref _succeededItems, value);
    }

    public int FailedItems
    {
        get => _failedItems;
        set => SetProperty(ref _failedItems, value);
    }

    public ICommand AddFilesCommand { get; }
    public ICommand AddFolderCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand ClearCompletedCommand { get; }
    public ICommand ConvertAllCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ApplyTargetToAllCommand { get; }
    public ICommand ApplyTargetToSelectedCommand { get; }
    public ICommand TrimMemoryCommand { get; }
    public ICommand OpenOutputFolderCommand { get; }

    public MainViewModel()
    {
        _registry = new ConverterRegistry();
        _pipeline = new BatchPipeline(_registry);

        _pipeline.ItemStarted += (_, item) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var vm = QueueItems.FirstOrDefault(q => q.Model.Id == item.Id);
                if (vm != null)
                {
                    vm.Status = ConversionStatus.Processing;
                    vm.StatusMessage = "Processing...";
                }
            });
        };

        _pipeline.ItemProgress += (_, item) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var vm = QueueItems.FirstOrDefault(q => q.Model.Id == item.Id);
                if (vm != null)
                {
                    vm.Progress = item.Progress;
                }
            });
        };

        _pipeline.ItemCompleted += (_, item) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var vm = QueueItems.FirstOrDefault(q => q.Model.Id == item.Id);
                if (vm != null)
                {
                    vm.Status = item.Status;
                    vm.Progress = item.Progress;
                    vm.StatusMessage = item.StatusMessage ?? "Done";
                    vm.ErrorMessage = item.ErrorMessage;
                }
            });
        };

        _pipeline.BatchProgressChanged += (_, progress) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                TotalItems = progress.TotalItems;
                ProcessedItems = progress.ProcessedItems;
                SucceededItems = progress.SucceededItems;
                FailedItems = progress.FailedItems;
                OverallProgress = progress.OverallPercentage;
                StatusText = $"Converting: {progress.ProcessedItems}/{progress.TotalItems} completed ({progress.OverallPercentage:F0}%)";
            });
        };

        AddFilesCommand = new RelayCommand(ExecuteAddFiles);
        AddFolderCommand = new RelayCommand(ExecuteAddFolder);
        ClearAllCommand = new RelayCommand(() =>
        {
            QueueItems.Clear();
            UpdateCounts();
            StatusText = "Queue cleared.";
        });
        ClearCompletedCommand = new RelayCommand(() =>
        {
            var completed = QueueItems.Where(i => i.Status == ConversionStatus.Completed).ToList();
            foreach (var item in completed) QueueItems.Remove(item);
            UpdateCounts();
            StatusText = $"Removed {completed.Count} completed items.";
        });

        ConvertAllCommand = new RelayCommand(async () => await ExecuteConvertAllAsync(), () => !IsBusy && QueueItems.Count > 0);
        CancelCommand = new RelayCommand(() => _pipeline.Cancel(), () => IsBusy);

        ApplyTargetToAllCommand = new RelayCommand(() =>
        {
            foreach (var item in QueueItems)
            {
                if (item.AvailableTargetFormats.Contains(CommonTargetFormat, StringComparer.OrdinalIgnoreCase))
                {
                    item.TargetFormat = CommonTargetFormat;
                }
            }
        });

        ApplyTargetToSelectedCommand = new RelayCommand(() =>
        {
            foreach (var item in QueueItems.Where(i => i.IsSelected))
            {
                if (item.AvailableTargetFormats.Contains(CommonTargetFormat, StringComparer.OrdinalIgnoreCase))
                {
                    item.TargetFormat = CommonTargetFormat;
                }
            }
        });

        TrimMemoryCommand = new RelayCommand(() =>
        {
            MemoryOptimizer.TrimWorkingSet();
            RefreshMemory();
            StatusText = $"Memory trimmed to {CurrentMemoryMb:F1} MB.";
        });

        OpenOutputFolderCommand = new RelayCommand(() =>
        {
            Directory.CreateDirectory(OutputDirectory);
            Process.Start("explorer.exe", $"\"{OutputDirectory}\"");
        });

        RefreshMemory();
    }

    public void AddPaths(IEnumerable<string> paths)
    {
        Directory.CreateDirectory(OutputDirectory);
        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                if (QueueItems.Any(q => string.Equals(q.SourcePath, path, StringComparison.OrdinalIgnoreCase))) continue;
                var vm = new ConversionItemViewModel(path, _registry, OutputDirectory);
                QueueItems.Add(vm);
            }
            else if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
                foreach (var f in files)
                {
                    if (QueueItems.Any(q => string.Equals(q.SourcePath, f, StringComparison.OrdinalIgnoreCase))) continue;
                    QueueItems.Add(new ConversionItemViewModel(f, _registry, OutputDirectory));
                }
            }
        }
        UpdateCounts();
        (ConvertAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void ExecuteAddFiles()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Title = "Select Files to Convert"
        };
        if (dlg.ShowDialog() == true)
        {
            AddPaths(dlg.FileNames);
        }
    }

    private void ExecuteAddFolder()
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Folder of Files to Convert",
            Multiselect = false
        };
        if (dlg.ShowDialog() == true)
        {
            AddPaths([dlg.FolderName]);
        }
    }

    private async Task ExecuteConvertAllAsync()
    {
        if (QueueItems.Count == 0) return;

        IsBusy = true;
        Directory.CreateDirectory(OutputDirectory);

        var itemsToProcess = QueueItems
            .Where(q => q.Status != ConversionStatus.Completed)
            .Select(q =>
            {
                q.SetOutputFolder(OutputDirectory);
                return q.Model;
            })
            .ToList();

        if (itemsToProcess.Count == 0)
        {
            IsBusy = false;
            StatusText = "All items in queue are already converted.";
            return;
        }

        StatusText = $"Starting conversion of {itemsToProcess.Count} items...";
        var sw = Stopwatch.StartNew();

        try
        {
            var results = await _pipeline.ExecuteBatchAsync(itemsToProcess);
            sw.Stop();

            var succeeded = results.Count(r => r.Success);
            var failed = results.Count(r => !r.Success);
            StatusText = $"Batch complete in {sw.Elapsed.TotalSeconds:F1}s! Succeeded: {succeeded}, Failed: {failed}";

            // Trim memory automatically after finishing batch
            MemoryOptimizer.TrimWorkingSet();
            RefreshMemory();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateCounts()
    {
        TotalItems = QueueItems.Count;
        ProcessedItems = QueueItems.Count(q => q.Status == ConversionStatus.Completed || q.Status == ConversionStatus.Failed);
        SucceededItems = QueueItems.Count(q => q.Status == ConversionStatus.Completed);
        FailedItems = QueueItems.Count(q => q.Status == ConversionStatus.Failed);
    }

    public void RefreshMemory()
    {
        CurrentMemoryMb = MemoryOptimizer.GetCurrentMemoryMb();
    }
}
