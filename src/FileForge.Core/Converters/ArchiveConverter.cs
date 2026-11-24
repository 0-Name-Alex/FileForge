using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using FileForge.Core.Interfaces;
using FileForge.Core.Models;

namespace FileForge.Core.Converters;

public sealed class ArchiveConverter : IFileConverter
{
    public string Name => "Archive & Compression Converter";
    public FileCategory Category => FileCategory.Archive;

    public IReadOnlyList<string> SupportedInputFormats => ["zip", "tar", "gz", "folder", "dir"];
    public IReadOnlyList<string> SupportedOutputFormats => ["zip", "tar", "gz"];

    public bool CanConvert(string sourceFormat, string targetFormat)
    {
        var src = sourceFormat.TrimStart('.').ToLowerInvariant();
        var tgt = targetFormat.TrimStart('.').ToLowerInvariant();
        return (SupportedInputFormats.Contains(src) || Directory.Exists(sourceFormat) || File.Exists(sourceFormat)) &&
               SupportedOutputFormats.Contains(tgt);
    }

    public async Task<ConversionResult> ConvertAsync(
        string sourcePath,
        string destinationPath,
        ConversionOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var srcExt = Path.GetExtension(sourcePath).TrimStart('.').ToLowerInvariant();
        var tgtExt = Path.GetExtension(destinationPath).TrimStart('.').ToLowerInvariant();

        try
        {
            var destDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destDir)) Directory.CreateDirectory(destDir);

            progress?.Report(10);
            cancellationToken.ThrowIfCancellationRequested();

            // Case 1: Compressing a file or folder into an archive
            if (Directory.Exists(sourcePath))
            {
                if (tgtExt == "zip")
                {
                    if (File.Exists(destinationPath) && options.Overwrite) File.Delete(destinationPath);
                    ZipFile.CreateFromDirectory(sourcePath, destinationPath, CompressionLevel.Optimal, false);
                }
                else if (tgtExt == "tar")
                {
                    if (File.Exists(destinationPath) && options.Overwrite) File.Delete(destinationPath);
                    TarFile.CreateFromDirectory(sourcePath, destinationPath, false);
                }
            }
            // Case 2: Converting between archive formats (e.g. tar to zip, gz to zip)
            else if (File.Exists(sourcePath))
            {
                // Create a temporary staging extraction dir
                var tempExtractDir = Path.Combine(Path.GetTempPath(), "FileForge_Extract_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempExtractDir);

                try
                {
                    progress?.Report(30);
                    if (srcExt == "zip")
                    {
                        ZipFile.ExtractToDirectory(sourcePath, tempExtractDir, true);
                    }
                    else if (srcExt == "tar")
                    {
                        TarFile.ExtractToDirectory(sourcePath, tempExtractDir, true);
                    }
                    else if (srcExt == "gz")
                    {
                        var decompressedFile = Path.Combine(tempExtractDir, Path.GetFileNameWithoutExtension(sourcePath));
                        using var inStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
                        using var gzStream = new GZipStream(inStream, CompressionMode.Decompress);
                        using var outStream = new FileStream(decompressedFile, FileMode.Create, FileAccess.Write);
                        await gzStream.CopyToAsync(outStream, cancellationToken);
                    }

                    progress?.Report(70);
                    cancellationToken.ThrowIfCancellationRequested();

                    // Now pack to target format
                    if (File.Exists(destinationPath) && options.Overwrite) File.Delete(destinationPath);

                    if (tgtExt == "zip")
                    {
                        ZipFile.CreateFromDirectory(tempExtractDir, destinationPath, CompressionLevel.Optimal, false);
                    }
                    else if (tgtExt == "tar")
                    {
                        TarFile.CreateFromDirectory(tempExtractDir, destinationPath, false);
                    }
                    else if (tgtExt == "gz")
                    {
                        var files = Directory.GetFiles(tempExtractDir);
                        var sourceToGz = files.Length > 0 ? files[0] : sourcePath;
                        using var inStream = new FileStream(sourceToGz, FileMode.Open, FileAccess.Read);
                        using var outStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
                        using var gzStream = new GZipStream(outStream, CompressionLevel.Optimal);
                        await inStream.CopyToAsync(gzStream, cancellationToken);
                    }
                }
                finally
                {
                    if (Directory.Exists(tempExtractDir))
                    {
                        try { Directory.Delete(tempExtractDir, true); } catch { }
                    }
                }
            }

            progress?.Report(100);
            sw.Stop();
            var bytes = File.Exists(destinationPath) ? new FileInfo(destinationPath).Length : 0;
            return ConversionResult.Ok(sourcePath, destinationPath, sw.Elapsed, bytes);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return ConversionResult.Fail(sourcePath, destinationPath, ex.Message, sw.Elapsed);
        }
    }
}
