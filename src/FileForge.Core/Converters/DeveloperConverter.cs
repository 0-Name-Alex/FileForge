using System.Diagnostics;
using System.Security.Cryptography;
using FileForge.Core.Interfaces;
using FileForge.Core.Models;

namespace FileForge.Core.Converters;

public sealed class DeveloperConverter : IFileConverter
{
    public string Name => "Developer & Encoding Converter";
    public FileCategory Category => FileCategory.Developer;

    public IReadOnlyList<string> SupportedInputFormats => ["*", "b64", "hex", "txt", "bin"];
    public IReadOnlyList<string> SupportedOutputFormats => ["b64", "hex", "txt", "sha256", "md5"];

    public bool CanConvert(string sourceFormat, string targetFormat)
    {
        var tgt = targetFormat.TrimStart('.').ToLowerInvariant();
        return SupportedOutputFormats.Contains(tgt);
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
            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            progress?.Report(10);
            cancellationToken.ThrowIfCancellationRequested();

            if (tgtExt == "b64")
            {
                byte[] bytes = await File.ReadAllBytesAsync(sourcePath, cancellationToken);
                progress?.Report(50);
                string b64 = Convert.ToBase64String(bytes);
                await File.WriteAllTextAsync(destinationPath, b64, cancellationToken);
            }
            else if (tgtExt == "hex")
            {
                byte[] bytes = await File.ReadAllBytesAsync(sourcePath, cancellationToken);
                progress?.Report(50);
                string hex = Convert.ToHexString(bytes);
                await File.WriteAllTextAsync(destinationPath, hex, cancellationToken);
            }
            else if (tgtExt is "sha256" or "md5")
            {
                using var stream = File.OpenRead(sourcePath);
                byte[] hashBytes = tgtExt == "sha256"
                    ? await SHA256.HashDataAsync(stream, cancellationToken)
                    : await MD5.HashDataAsync(stream, cancellationToken);

                string hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();
                string output = $"{hashString}  *{Path.GetFileName(sourcePath)}\n";
                await File.WriteAllTextAsync(destinationPath, output, cancellationToken);
            }
            else if (tgtExt is "txt" or "bin" && srcExt == "b64")
            {
                string b64 = await File.ReadAllTextAsync(sourcePath, cancellationToken);
                byte[] bytes = Convert.FromBase64String(b64.Trim());
                await File.WriteAllBytesAsync(destinationPath, bytes, cancellationToken);
            }
            else if (tgtExt is "txt" or "bin" && srcExt == "hex")
            {
                string hex = await File.ReadAllTextAsync(sourcePath, cancellationToken);
                byte[] bytes = Convert.FromHexString(hex.Trim().Replace(" ", "").Replace("\n", "").Replace("\r", ""));
                await File.WriteAllBytesAsync(destinationPath, bytes, cancellationToken);
            }

            progress?.Report(100);
            sw.Stop();
            var outSize = File.Exists(destinationPath) ? new FileInfo(destinationPath).Length : 0;
            return ConversionResult.Ok(sourcePath, destinationPath, sw.Elapsed, outSize);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return ConversionResult.Fail(sourcePath, destinationPath, ex.Message, sw.Elapsed);
        }
    }
}
