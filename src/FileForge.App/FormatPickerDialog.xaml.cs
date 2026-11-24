using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileForge.App;

public sealed record FormatDisplayItem(
    string Extension,
    string Name,
    string CategoryName,
    string CategoryKey,
    string Description)
{
    public string ExtensionUpper => Extension.ToUpperInvariant();
}

public partial class FormatPickerDialog : Window
{
    private static readonly List<FormatDisplayItem> AllFormats =
    [
        // Documents & Slides
        new("pdf", "Portable Document Format", "Documents & Slides", "Docs", "Universal document format for slides, printing and documents"),
        new("docx", "Microsoft Word Document", "Documents & Slides", "Docs", "Rich formatted text document"),
        new("txt", "Plain Text Document", "Documents & Slides", "Docs", "Raw unformatted text file"),
        new("md", "Markdown Document", "Documents & Slides", "Docs", "Lightweight formatted markup document"),
        new("html", "HTML Web Page", "Documents & Slides", "Docs", "Web markup viewable in any browser"),

        // Images
        new("webp", "WebP Image", "Images", "Images", "Modern compressed web image with transparency support"),
        new("png", "PNG Image", "Images", "Images", "Lossless bitmap graphic with alpha channel"),
        new("jpg", "JPEG Image", "Images", "Images", "Compressed photographic image"),
        new("bmp", "Windows Bitmap", "Images", "Images", "Uncompressed raster graphic image"),
        new("ico", "Windows Icon", "Images", "Images", "Multi-resolution Windows icon file"),
        new("tiff", "TIFF Image", "Images", "Images", "High-fidelity tagged image format"),

        // Data & Spreadsheets
        new("xlsx", "Excel Spreadsheet", "Data & Sheets", "Data", "Microsoft Excel workbook with formatted sheets"),
        new("csv", "Comma-Separated Values", "Data & Sheets", "Data", "Tabular data with comma separators"),
        new("tsv", "Tab-Separated Values", "Data & Sheets", "Data", "Tabular data with tab separators"),
        new("json", "JSON Data", "Data & Sheets", "Data", "Structured JavaScript Object Notation data"),
        new("xml", "XML Data", "Data & Sheets", "Data", "Hierarchical extensible markup data"),

        // Archives
        new("zip", "ZIP Archive", "Archives", "Archives", "Standard compressed archive container"),
        new("tar", "Tape Archive (TAR)", "Archives", "Archives", "Uncompressed Unix archive pack"),
        new("gz", "GZip Compressed Archive", "Archives", "Archives", "Single-file GNU zip compression"),

        // Developer & Hashes
        new("b64", "Base64 Encoded Text", "Developer & Hashes", "Dev", "Binary data converted to ASCII text string"),
        new("hex", "Hexadecimal Dump", "Developer & Hashes", "Dev", "Raw bytes represented in hexadecimal pairs"),
        new("sha256", "SHA-256 Checksum", "Developer & Hashes", "Dev", "Cryptographic 256-bit hash fingerprint"),
        new("md5", "MD5 Checksum", "Developer & Hashes", "Dev", "Legacy 128-bit file integrity hash")
    ];

    private readonly List<FormatDisplayItem> _sourceList;
    private readonly bool _isInitialized;
    public ObservableCollection<FormatDisplayItem> FilteredFormats { get; } = [];
    public string? SelectedFormat { get; private set; }

    public FormatPickerDialog(IEnumerable<string>? allowedFormats = null, string? initialSelection = null)
    {
        InitializeComponent();

        if (allowedFormats != null && allowedFormats.Any())
        {
            var allowedSet = new HashSet<string>(allowedFormats.Select(f => f.TrimStart('.').ToLowerInvariant()), StringComparer.OrdinalIgnoreCase);
            _sourceList = AllFormats.Where(f => allowedSet.Contains(f.Extension)).ToList();

            // Include any allowed formats not in the predefined catalog
            foreach (var f in allowedSet)
            {
                if (!_sourceList.Any(s => string.Equals(s.Extension, f, StringComparison.OrdinalIgnoreCase)))
                {
                    _sourceList.Add(new(f, f.ToUpperInvariant() + " File", "Custom", "All", $"Target format .{f}"));
                }
            }
        }
        else
        {
            _sourceList = [.. AllFormats];
        }

        FormatsList.ItemsSource = FilteredFormats;
        _isInitialized = true;
        ApplyFilter();

        if (!string.IsNullOrEmpty(initialSelection))
        {
            var clean = initialSelection.TrimStart('.').ToLowerInvariant();
            var match = FilteredFormats.FirstOrDefault(f => string.Equals(f.Extension, clean, StringComparison.OrdinalIgnoreCase));
            if (match != null) FormatsList.SelectedItem = match;
        }

        Loaded += (_, _) =>
        {
            SearchBox.Focus();
            if (FormatsList.SelectedItem == null && FilteredFormats.Count > 0)
            {
                FormatsList.SelectedIndex = 0;
            }
        };
    }

    private string GetActiveCategory()
    {
        if (CatDocs?.IsChecked == true) return "Docs";
        if (CatImages?.IsChecked == true) return "Images";
        if (CatData?.IsChecked == true) return "Data";
        if (CatArchives?.IsChecked == true) return "Archives";
        if (CatDev?.IsChecked == true) return "Dev";
        return "All";
    }

    private void ApplyFilter()
    {
        if (!_isInitialized || SearchBox == null || SearchPlaceholder == null || FormatsList == null)
            return;

        var query = SearchBox.Text.Trim();
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(query) ? Visibility.Visible : Visibility.Hidden;

        var cat = GetActiveCategory();

        FilteredFormats.Clear();
        foreach (var item in _sourceList)
        {
            if (cat != "All" && item.CategoryKey != cat) continue;

            if (!string.IsNullOrEmpty(query))
            {
                var match = item.Extension.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            item.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            item.CategoryName.Contains(query, StringComparison.OrdinalIgnoreCase);
                if (!match) continue;
            }

            FilteredFormats.Add(item);
        }

        if (FormatsList.SelectedItem == null && FilteredFormats.Count > 0)
        {
            FormatsList.SelectedIndex = 0;
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void OnCategoryChanged(object sender, RoutedEventArgs e) => ApplyFilter();

    private void OnFormatDoubleClick(object sender, MouseButtonEventArgs e) => SelectAndClose();

    private void OnSelectClick(object sender, RoutedEventArgs e) => SelectAndClose();

    private void SelectAndClose()
    {
        if (FormatsList.SelectedItem is FormatDisplayItem item)
        {
            SelectedFormat = item.Extension;
            DialogResult = true;
            Close();
        }
        else if (FilteredFormats.Count > 0)
        {
            SelectedFormat = FilteredFormats[0].Extension;
            DialogResult = true;
            Close();
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            SelectAndClose();
            e.Handled = true;
        }
        else if (e.Key == Key.Down && SearchBox.IsFocused)
        {
            FormatsList.Focus();
            if (FormatsList.SelectedIndex < 0 && FilteredFormats.Count > 0)
                FormatsList.SelectedIndex = 0;
            e.Handled = true;
        }
    }
}
