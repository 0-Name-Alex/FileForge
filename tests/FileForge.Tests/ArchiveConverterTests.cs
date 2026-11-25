using System.IO.Compression;
using FileForge.Core.Converters;
using FileForge.Core.Models;

namespace FileForge.Tests;

public sealed class ArchiveConverterTests : IDisposable
{
    private readonly TempWorkspace _ws = new();
    private readonly ArchiveConverter _converter = new();

    [Fact]
    public async Task ConvertAsync_FolderToZipAndZipToTar_CreatesValidArchives()
    {
        var folder = Path.Combine(_ws.Root, "bundle");
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "readme.txt"), "hello archive");

        var zipPath = _ws.PathFor("bundle.zip");
        var tarPath = _ws.PathFor("bundle.tar");
        var zipRoundTrip = _ws.PathFor("bundle_repacked.zip");

        var toZip = await _converter.ConvertAsync(folder, zipPath, ConversionOptions.Default);
        Assert.True(toZip.Success);
        Assert.True(File.Exists(zipPath));

        var toTar = await _converter.ConvertAsync(zipPath, tarPath, ConversionOptions.Default);
        Assert.True(toTar.Success);
        Assert.True(new FileInfo(tarPath).Length > 0);

        var backToZip = await _converter.ConvertAsync(tarPath, zipRoundTrip, ConversionOptions.Default);
        Assert.True(backToZip.Success);

        using var archive = ZipFile.OpenRead(zipRoundTrip);
        Assert.Contains(archive.Entries, e => e.Name == "readme.txt");
    }

    [Fact]
    public async Task ConvertAsync_GzipToZip_DecompressesAndRepacks()
    {
        var gzPath = _ws.PathFor("sample.txt.gz");
        using (var fs = File.Create(gzPath))
        using (var gz = new GZipStream(fs, CompressionLevel.Optimal))
        using (var writer = new StreamWriter(gz))
        {
            await writer.WriteAsync("compressed gz payload");
        }

        var zipPath = _ws.PathFor("sample.zip");
        var res = await _converter.ConvertAsync(gzPath, zipPath, ConversionOptions.Default);

        Assert.True(res.Success);
        using var archive = ZipFile.OpenRead(zipPath);
        Assert.Contains(archive.Entries, e => e.Name == "sample.txt");
    }

    public void Dispose() => _ws.Dispose();
}
