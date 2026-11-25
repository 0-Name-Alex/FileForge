using FileForge.Core.Detection;
using FileForge.Core.Models;

namespace FileForge.Tests;

public sealed class FormatDetectorTests : IDisposable
{
    private readonly TempWorkspace _ws = new();

    public static IEnumerable<object[]> MagicByteCases()
    {
        yield return ["spoofed_png.txt", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "png", FileCategory.Image];
        yield return ["spoofed_jpg.bin", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "jpg", FileCategory.Image];
        yield return ["spoofed_gif.dat", new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, "gif", FileCategory.Image];
        yield return ["spoofed_webp.tmp", new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50 }, "webp", FileCategory.Image];
        yield return ["spoofed_bmp.txt", new byte[] { 0x42, 0x4D, 0x00, 0x00 }, "bmp", FileCategory.Image];
        yield return ["spoofed_tiff.bin", new byte[] { 0x49, 0x49, 0x2A, 0x00 }, "tiff", FileCategory.Image];
        yield return ["spoofed_pdf.jpg", new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E }, "pdf", FileCategory.Document];
        yield return ["spoofed_gz.txt", new byte[] { 0x1F, 0x8B, 0x08, 0x00 }, "gz", FileCategory.Archive];
        yield return ["spoofed_7z.bin", new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }, "7z", FileCategory.Archive];
        yield return ["plain_archive.dat", new byte[] { 0x50, 0x4B, 0x03, 0x04 }, "zip", FileCategory.Archive];
        yield return ["slides.pptx", new byte[] { 0x50, 0x4B, 0x03, 0x04 }, "pptx", FileCategory.Document];
        yield return ["doc.docx", new byte[] { 0x50, 0x4B, 0x03, 0x04 }, "docx", FileCategory.Document];
    }

    [Theory]
    [MemberData(nameof(MagicByteCases))]
    public void DetectFromFile_IdentifiesMagicBytesRegardlessOfExtension(
        string fileName,
        byte[] header,
        string expectedExt,
        FileCategory expectedCategory)
    {
        var path = _ws.WriteBytes(fileName, header);

        var info = FormatDetector.DetectFromFile(path);

        Assert.Equal(expectedExt, info.Extension);
        Assert.Equal(expectedCategory, info.Category);
    }

    [Fact]
    public void DetectFromFile_MissingFileFallsBackToExtension()
    {
        var missingPath = _ws.PathFor("nonexistent.csv");

        var info = FormatDetector.DetectFromFile(missingPath);

        Assert.Equal("csv", info.Extension);
        Assert.Equal(FileCategory.Data, info.Category);
    }

    [Fact]
    public void DetectFromFile_UnknownExtensionReturnsOtherCategory()
    {
        var path = _ws.WriteText("custom.xyz", "hello");

        var info = FormatDetector.DetectFromFile(path);

        Assert.Equal("xyz", info.Extension);
        Assert.Equal(FileCategory.Other, info.Category);
        Assert.Equal("application/octet-stream", info.MimeType);
    }

    [Theory]
    [InlineData("pdf", new[] { "split-pdf", "split-png", "docx" })]
    [InlineData("pptx", new[] { "pdf", "docx", "txt" })]
    [InlineData(".png", new[] { "webp", "jpg", "pdf" })]
    [InlineData("csv", new[] { "json", "xlsx", "xml" })]
    public void GetValidTargetFormats_ReturnsExpectedTargets(string sourceExt, string[] expectedSubset)
    {
        var targets = FormatDetector.GetValidTargetFormats(sourceExt);

        foreach (var item in expectedSubset)
        {
            Assert.Contains(item, targets);
        }
    }

    public void Dispose() => _ws.Dispose();
}
