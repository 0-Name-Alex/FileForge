using System.Windows;
using System.Windows.Input;
using FileForge.App.ViewModels;

namespace FileForge.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnFileDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) && DataContext is MainViewModel vm)
        {
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            vm.AddPaths(paths);
        }
        DropZone.BorderBrush = FindResource("ControlStrokeColorDefaultBrush") as System.Windows.Media.Brush;
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            DropZone.BorderBrush = FindResource("AccentFillColorDefaultBrush") as System.Windows.Media.Brush;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnDropZoneClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.AddFilesCommand.Execute(null);
        }
    }

    private void OnOpenBatchFormatPickerClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            var dialog = new FormatPickerDialog(initialSelection: vm.CommonTargetFormat)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.SelectedFormat))
            {
                vm.CommonTargetFormat = dialog.SelectedFormat;
                vm.ApplyTargetToAllCommand.Execute(null);
            }
        }
    }

    private void OnOpenRowFormatPickerClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.DataContext is ConversionItemViewModel itemVm)
        {
            var dialog = new FormatPickerDialog(itemVm.AvailableTargetFormats, itemVm.TargetFormat)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.SelectedFormat))
            {
                itemVm.TargetFormat = dialog.SelectedFormat;
            }
        }
    }
}
