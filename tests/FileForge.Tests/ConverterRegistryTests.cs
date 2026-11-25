using FileForge.Core.Converters;
using FileForge.Core.Registry;

namespace FileForge.Tests;

public sealed class ConverterRegistryTests
{
    private readonly ConverterRegistry _registry = new();

    [Fact]
    public void FindConverter_PrefersNativeCSharpConvertersOverPythonBridge()
    {
        Assert.IsType<DataConverter>(_registry.FindConverter("csv", "json"));
        Assert.IsType<ArchiveConverter>(_registry.FindConverter("zip", "tar"));
        Assert.IsType<DeveloperConverter>(_registry.FindConverter("txt", "sha256"));
    }

    [Fact]
    public void FindConverter_NormalizesLeadingDotsAndCasing()
    {
        Assert.True(_registry.CanConvert(".CSV", ".JSON"));
        Assert.True(_registry.CanConvert(".PNG", ".WEBP"));
        Assert.True(_registry.CanConvert(".PDF", ".SPLIT-PDF"));
    }

    [Fact]
    public void GetAvailableTargetFormats_ReturnsSortedUniqueTargets()
    {
        var targets = _registry.GetAvailableTargetFormats("csv");

        Assert.NotEmpty(targets);
        Assert.Contains("json", targets);
        Assert.Contains("xml", targets);
        Assert.Equal(targets.OrderBy(t => t, StringComparer.Ordinal).ToList(), targets.ToList());
    }
}
