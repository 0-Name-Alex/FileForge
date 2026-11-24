using System.Threading.Channels;
using FileForge.Core.Interfaces;
using FileForge.Core.Models;
using FileForge.Core.Registry;

namespace FileForge.Core.Pipeline;

public sealed class BatchPipeline : IBatchPipeline
{
    private readonly ConverterRegistry _registry;
    private CancellationTokenSource? _cts;

    public event EventHandler<ConversionItem>? ItemStarted;
    public event EventHandler<ConversionItem>? ItemCompleted;
    public event EventHandler<ConversionItem>? ItemProgress;
    public event EventHandler<BatchProgress>? BatchProgressChanged;

    public bool IsRunning { get; private set; }

    public BatchPipeline(ConverterRegistry registry)
    {
        _registry = registry;
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }

    public async Task<IReadOnlyList<ConversionResult>> ExecuteBatchAsync(
        IEnumerable<ConversionItem> items,
        int maxConcurrency = 0,
        CancellationToken cancellationToken = default)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0) return Array.Empty<ConversionResult>();

        if (maxConcurrency <= 0)
            maxConcurrency = Math.Max(1, Math.Min(Environment.ProcessorCount, 4));

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ct = _cts.Token;
        IsRunning = true;

        int total = itemList.Count;
        int processed = 0;
        int succeeded = 0;
        int failed = 0;
        var results = new List<ConversionResult>();
        var lockObj = new object();

        void ReportOverall()
        {
            lock (lockObj)
            {
                var percentage = total > 0 ? (double)processed / total * 100.0 : 0.0;
                BatchProgressChanged?.Invoke(this, new BatchProgress(total, processed, succeeded, failed, percentage));
            }
        }

        ReportOverall();

        var channel = Channel.CreateBounded<ConversionItem>(new BoundedChannelOptions(total)
        {
            SingleWriter = true,
            SingleReader = false
        });

        // Producer: Write all items to channel
        _ = Task.Run(async () =>
        {
            foreach (var item in itemList)
            {
                await channel.Writer.WriteAsync(item, ct);
            }
            channel.Writer.Complete();
        }, ct);

        // Consumers: Parallel workers bounded by maxConcurrency
        var workers = Enumerable.Range(0, maxConcurrency).Select(async _ =>
        {
            while (await channel.Reader.WaitToReadAsync(ct))
            {
                while (channel.Reader.TryRead(out var item))
                {
                    if (ct.IsCancellationRequested)
                    {
                        item.Status = ConversionStatus.Cancelled;
                        item.StatusMessage = "Cancelled";
                        continue;
                    }

                    item.Status = ConversionStatus.Processing;
                    item.Progress = 0;
                    item.StatusMessage = "Starting...";
                    ItemStarted?.Invoke(this, item);

                    var progress = new Progress<double>(p =>
                    {
                        item.Progress = p;
                        ItemProgress?.Invoke(this, item);
                    });

                    var srcExt = Directory.Exists(item.SourcePath)
                        ? "folder"
                        : Path.GetExtension(item.SourcePath).TrimStart('.').ToLowerInvariant();

                    if (string.IsNullOrEmpty(srcExt) && !string.IsNullOrEmpty(item.SourceFormat))
                    {
                        srcExt = item.SourceFormat.TrimStart('.').ToLowerInvariant();
                    }

                    var tgtExt = item.TargetFormat.TrimStart('.').ToLowerInvariant();
                    var converter = _registry.FindConverter(srcExt, tgtExt);

                    ConversionResult result;
                    if (converter == null)
                    {
                        result = ConversionResult.Fail(
                            item.SourcePath,
                            item.DestinationPath,
                            $"No converter registered for {srcExt} -> {tgtExt}.",
                            TimeSpan.Zero);
                    }
                    else
                    {
                        item.Options.TargetFormat ??= item.TargetFormat;
                        result = await converter.ConvertAsync(
                            item.SourcePath,
                            item.DestinationPath,
                            item.Options,
                            progress,
                            ct);
                    }

                    lock (lockObj)
                    {
                        results.Add(result);
                        processed++;
                        if (result.Success)
                        {
                            succeeded++;
                            item.Status = ConversionStatus.Completed;
                            item.Progress = 100;
                            item.StatusMessage = $"Done ({result.ElapsedTime.TotalSeconds:F1}s)";
                        }
                        else
                        {
                            failed++;
                            item.Status = ConversionStatus.Failed;
                            item.ErrorMessage = result.ErrorMessage;
                            item.StatusMessage = "Failed";
                        }
                        item.CompletedAt = DateTime.UtcNow;
                    }

                    ItemCompleted?.Invoke(this, item);
                    ReportOverall();
                }
            }
        });

        try
        {
            await Task.WhenAll(workers);
        }
        catch (OperationCanceledException)
        {
            // Batch cancelled
        }
        finally
        {
            IsRunning = false;
        }

        return results;
    }
}
