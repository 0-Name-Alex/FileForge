using System.Windows;
using System.Windows.Input;
using FileForge.App.ViewModels;
using FileForge.Core.Pipeline;
using FileForge.Core.Registry;

namespace FileForge.App;

public partial class QuickLauncherWindow : Window
{
    private readonly QuickLauncherViewModel _vm;

    public QuickLauncherWindow(MainViewModel mainVm)
    {
        InitializeComponent();

        var registry = new ConverterRegistry();
        var pipeline = new BatchPipeline(registry);
        _vm = new QuickLauncherViewModel(registry, pipeline);
        DataContext = _vm;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private async void OnQuickDrop(object sender, DragEventArgs e)
    {
        QuickDropZone.BorderBrush = FindResource("ControlStrokeColorDefaultBrush") as System.Windows.Media.Brush;

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            await _vm.ConvertQuickFilesAsync(paths);
        }
    }

    private void OnQuickDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            QuickDropZone.BorderBrush = FindResource("AccentFillColorDefaultBrush") as System.Windows.Media.Brush;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnQuickDropZoneClick(object sender, MouseButtonEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Title = "Select Files to Quick Convert"
        };
        if (dlg.ShowDialog() == true)
        {
            _ = _vm.ConvertQuickFilesAsync(dlg.FileNames);
        }
    }
}
