using FileForge.Core.Models;

namespace FileForge.Core.Interfaces;

public interface IFileConverter
{
    string Name { get; }
    FileCategory Category { get; }
    IReadOnlyList<string> SupportedInputFormats { get; }
    IReadOnlyList<string> SupportedOutputFormats { get; }

    bool CanConvert(string sourceFormat, string targetFormat);

    Task<ConversionResult> ConvertAsync(
        string sourcePath,
        string destinationPath,
        ConversionOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}

public interface IBatchPipeline
{
    event EventHandler<ConversionItem>? ItemStarted;
    event EventHandler<ConversionItem>? ItemCompleted;
    event EventHandler<ConversionItem>? ItemProgress;
    event EventHandler<BatchProgress>? BatchProgressChanged;

    bool IsRunning { get; }
    Task<IReadOnlyList<ConversionResult>> ExecuteBatchAsync(
        IEnumerable<ConversionItem> items,
        int maxConcurrency = 0,
        CancellationToken cancellationToken = default);

    void Cancel();
}

public sealed record BatchProgress(
    int TotalItems,
    int ProcessedItems,
    int SucceededItems,
    int FailedItems,
    double OverallPercentage);
