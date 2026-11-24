using System.Diagnostics;
using System.Text;
using FileForge.Core.Interfaces;
using FileForge.Core.Models;

namespace FileForge.Core.Converters;

public sealed class PdfToolsConverter : IFileConverter
{
    public string Name => "PDF Power Tools (Merge & Split)";
    public FileCategory Category => FileCategory.Document;

    public IReadOnlyList<string> SupportedInputFormats => ["pdf", "png", "jpg", "jpeg", "webp", "bmp", "pptx", "ppt"];
    public IReadOnlyList<string> SupportedOutputFormats => ["pdf", "split-pdf", "split-png", "split-jpg", "split-webp"];

    public bool CanConvert(string sourceFormat, string targetFormat)
    {
        var src = sourceFormat.TrimStart('.').ToLowerInvariant();
        var tgt = targetFormat.TrimStart('.').ToLowerInvariant();

        if (src == "pdf" && tgt is "split-pdf" or "split-png" or "split-jpg" or "split-webp")
            return true;

        if (tgt == "pdf" && SupportedInputFormats.Contains(src))
            return true;

        return false;
    }

    public async Task<ConversionResult> ConvertAsync(
        string sourcePath,
        string destinationPath,
        ConversionOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            progress?.Report(10);
            cancellationToken.ThrowIfCancellationRequested();

            var scriptFile = FindScriptPath();
            if (!File.Exists(scriptFile))
            {
                return ConversionResult.Fail(sourcePath, destinationPath, $"Python bridge script not found at '{scriptFile}'.", sw.Elapsed);
            }

            var targetExt = options.TargetFormat?.TrimStart('.').ToLowerInvariant();
            if (string.IsNullOrEmpty(targetExt))
            {
                var ext = Path.GetExtension(destinationPath).TrimStart('.').ToLowerInvariant();
                if (!string.IsNullOrEmpty(ext) && ext != "pdf")
                    targetExt = ext;
                else if (Directory.Exists(destinationPath) || !Path.HasExtension(destinationPath))
                    targetExt = "split-pdf";
                else
                    targetExt = "pdf";
            }

            if (targetExt.StartsWith("split"))
            {
                // Destination is a directory containing extracted page items
                var outFolder = destinationPath;
                if (Path.HasExtension(destinationPath))
                {
                    var parentDir = Path.GetDirectoryName(destinationPath) ?? "";
                    var baseName = Path.GetFileNameWithoutExtension(sourcePath);
                    outFolder = Path.Combine(parentDir, $"{baseName}_pages");
                }

                if (File.Exists(outFolder))
                {
                    File.Delete(outFolder);
                }
                Directory.CreateDirectory(outFolder);
                var splitFormat = targetExt switch
                {
                    "split-png" => "png",
                    "split-jpg" => "jpg",
                    "split-webp" => "webp",
                    _ => "pdf"
                };

                var args = new StringBuilder();
                args.Append($"\"{scriptFile}\" ");
                args.Append($"--action split-pdf ");
                args.Append($"--source \"{sourcePath}\" ");
                args.Append($"--target \"{outFolder}\" ");
                args.Append($"--split-format {splitFormat}");

                var psi = new ProcessStartInfo
                {
                    FileName = "py",
                    Arguments = $"-3.14 {args}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                progress?.Report(30);

                using var process = new Process { StartInfo = psi };
                var outputSb = new StringBuilder();
                var errorSb = new StringBuilder();

                process.OutputDataReceived += (_, e) => { if (e.Data != null) outputSb.AppendLine(e.Data); };
                process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorSb.AppendLine(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                progress?.Report(60);
                await process.WaitForExitAsync(cancellationToken);
                progress?.Report(90);

                sw.Stop();

                if (process.ExitCode == 0 && Directory.Exists(outFolder) && Directory.GetFiles(outFolder).Length > 0)
                {
                    progress?.Report(100);
                    var files = Directory.GetFiles(outFolder);
                    long totalBytes = files.Sum(f => new FileInfo(f).Length);
                    return ConversionResult.Ok(sourcePath, outFolder, sw.Elapsed, totalBytes);
                }

                var err = errorSb.ToString().Trim();
                if (string.IsNullOrEmpty(err)) err = $"Split failed with exit code {process.ExitCode}";
                return ConversionResult.Fail(sourcePath, destinationPath, err, sw.Elapsed);
            }
            else
            {
                // Single-file conversion into PDF
                var dir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                var args = new StringBuilder();
                args.Append($"\"{scriptFile}\" ");
                args.Append($"--action merge-pdf ");
                args.Append($"--target \"{destinationPath}\" ");
                args.Append($"--inputs \"{sourcePath}\"");

                var psi = new ProcessStartInfo
                {
                    FileName = "py",
                    Arguments = $"-3.14 {args}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using var process = new Process { StartInfo = psi };
                var errorSb = new StringBuilder();
                process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorSb.AppendLine(e.Data); };

                process.Start();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync(cancellationToken);

                sw.Stop();

                if (process.ExitCode == 0 && File.Exists(destinationPath))
                {
                    progress?.Report(100);
                    return ConversionResult.Ok(sourcePath, destinationPath, sw.Elapsed, new FileInfo(destinationPath).Length);
                }

                return ConversionResult.Fail(sourcePath, destinationPath, errorSb.ToString().Trim(), sw.Elapsed);
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            return ConversionResult.Fail(sourcePath, destinationPath, ex.Message, sw.Elapsed);
        }
    }

    public static async Task<ConversionResult> MergeFilesToPdfAsync(
        IEnumerable<string> sourceFiles,
        string destinationPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var filesList = sourceFiles.Where(File.Exists).ToList();
            if (filesList.Count == 0)
            {
                return ConversionResult.Fail(string.Empty, destinationPath, "No valid source files provided to merge.", TimeSpan.Zero);
            }

            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var scriptFile = FindScriptPath();
            if (!File.Exists(scriptFile))
            {
                return ConversionResult.Fail(string.Empty, destinationPath, $"Python bridge script not found at '{scriptFile}'.", sw.Elapsed);
            }

            progress?.Report(10);
            cancellationToken.ThrowIfCancellationRequested();

            var args = new StringBuilder();
            args.Append($"\"{scriptFile}\" ");
            args.Append($"--action merge-pdf ");
            args.Append($"--target \"{destinationPath}\" ");
            args.Append("--inputs ");
            foreach (var f in filesList)
            {
                args.Append($"\"{f}\" ");
            }

            var psi = new ProcessStartInfo
            {
                FileName = "py",
                Arguments = $"-3.14 {args}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            progress?.Report(30);

            using var process = new Process { StartInfo = psi };
            var outputSb = new StringBuilder();
            var errorSb = new StringBuilder();

            process.OutputDataReceived += (_, e) => { if (e.Data != null) outputSb.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorSb.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            progress?.Report(60);
            await process.WaitForExitAsync(cancellationToken);
            progress?.Report(90);

            sw.Stop();

            if (process.ExitCode == 0 && File.Exists(destinationPath))
            {
                progress?.Report(100);
                var outBytes = new FileInfo(destinationPath).Length;
                return ConversionResult.Ok(filesList[0], destinationPath, sw.Elapsed, outBytes);
            }
            else
            {
                var err = errorSb.ToString().Trim();
                if (string.IsNullOrEmpty(err)) err = $"Merge failed with exit code {process.ExitCode}";
                return ConversionResult.Fail(filesList[0], destinationPath, err, sw.Elapsed);
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            return ConversionResult.Fail(string.Empty, destinationPath, ex.Message, sw.Elapsed);
        }
    }

    private static string FindScriptPath()
    {
        string[] candidates =
        [
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "converter_cli.py")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "FileForge.Adapters.Python", "converter_cli.py")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "FileForge.Adapters.Python", "converter_cli.py")),
            @"D:\Projects\File editor\src\FileForge.Adapters.Python\converter_cli.py"
        ];

        foreach (var c in candidates)
        {
            if (File.Exists(c)) return c;
        }

        return candidates.Last();
    }
}
