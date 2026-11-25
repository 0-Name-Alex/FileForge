using FileForge.Core.Interfaces;
using FileForge.Core.Models;
using FileForge.Core.Pipeline;
using FileForge.Core.Registry;

namespace FileForge.Tests;

public sealed class BatchPipelineTests : IDisposable
{
    private readonly TempWorkspace _ws = new();

    [Fact]
    public async Task ExecuteBatchAsync_EmptyInput_ReturnsEmptyResults()
    {
        var pipeline = new BatchPipeline(new ConverterRegistry());

        var results = await pipeline.ExecuteBatchAsync([]);

        Assert.Empty(results);
        Assert.False(pipeline.IsRunning);
    }

    [Fact]
    public async Task ExecuteBatchAsync_ConcurrentBatch_CompletesAllAndFiresProgressEvents()
    {
        var pipeline = new BatchPipeline(new ConverterRegistry());
        var csv = _ws.WriteText("metrics.csv", "k,v\na,1\n");
        var txt = _ws.WriteText("notes.txt", "FileForge batch");

        var started = 0;
        var completed = 0;
        BatchProgress? lastProgress = null;

        pipeline.ItemStarted += (_, _) => Interlocked.Increment(ref started);
        pipeline.ItemCompleted += (_, _) => Interlocked.Increment(ref completed);
        pipeline.BatchProgressChanged += (_, p) => lastProgress = p;

        List<ConversionItem> items =
        [
            new() { SourcePath = csv, SourceFormat = "csv", TargetFormat = "json", DestinationPath = _ws.PathFor("out/metrics.json") },
            new() { SourcePath = txt, SourceFormat = "txt", TargetFormat = "b64", DestinationPath = _ws.PathFor("out/notes.b64") },
        ];

        var results = await pipeline.ExecuteBatchAsync(items, maxConcurrency: 2);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
        Assert.Equal(2, started);
        Assert.Equal(2, completed);
        Assert.NotNull(lastProgress);
        Assert.Equal(2, lastProgress!.SucceededItems);
        Assert.Equal(0, lastProgress.FailedItems);
        Assert.Equal(100.0, lastProgress.OverallPercentage);
        Assert.False(pipeline.IsRunning);
    }

    [Fact]
    public async Task ExecuteBatchAsync_UnregisteredConversion_FailsGracefully()
    {
        var pipeline = new BatchPipeline(new ConverterRegistry());
        var bin = _ws.WriteText("unknown.xyz", "abc");

        var item = new ConversionItem
        {
            SourcePath = bin,
            SourceFormat = "xyz",
            TargetFormat = "nonexistent_target",
            DestinationPath = _ws.PathFor("out/unknown.nonexistent_target")
        };

        var results = await pipeline.ExecuteBatchAsync([item]);

        var res = Assert.Single(results);
        Assert.False(res.Success);
        Assert.Equal(ConversionStatus.Failed, item.Status);
        Assert.Contains("No converter registered", item.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteBatchAsync_PreCancelledToken_ResetsIsRunning()
    {
        var pipeline = new BatchPipeline(new ConverterRegistry());
        var txt = _ws.WriteText("a.txt", "data");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var item = new ConversionItem
        {
            SourcePath = txt,
            SourceFormat = "txt",
            TargetFormat = "b64",
            DestinationPath = _ws.PathFor("out/a.b64")
        };

        var results = await pipeline.ExecuteBatchAsync([item], cancellationToken: cts.Token);

        Assert.Empty(results);
        Assert.False(pipeline.IsRunning);
    }

    public void Dispose() => _ws.Dispose();
}
