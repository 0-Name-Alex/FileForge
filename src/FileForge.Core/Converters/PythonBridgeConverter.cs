using System.Diagnostics;
using System.Text;
using FileForge.Core.Interfaces;
using FileForge.Core.Models;

namespace FileForge.Core.Converters;

public sealed class PythonBridgeConverter : IFileConverter
{
    private static readonly string ScriptPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "..", "..", "..", "..", "src", "FileForge.Adapters.Python", "converter_cli.py");

    public string Name => "Python Multi-Engine Bridge (Pillow, OpenPyXL, Docx, PDF)";
    public FileCategory Category => FileCategory.Image;

    private static readonly HashSet<string> ImageExts = ["png", "jpg", "jpeg", "webp", "bmp", "ico", "tiff", "tif", "gif", "pdf"];
    private static readonly HashSet<string> DocExts = ["docx", "pdf", "txt", "md", "html"];
    private static readonly HashSet<string> ExcelExts = ["xlsx", "csv", "tsv", "json"];

    public IReadOnlyList<string> SupportedInputFormats =>
        ImageExts.Concat(DocExts).Concat(ExcelExts).Distinct().ToList();

    public IReadOnlyList<string> SupportedOutputFormats =>
        ImageExts.Concat(DocExts).Concat(ExcelExts).Distinct().ToList();

    public bool CanConvert(string sourceFormat, string targetFormat)
    {
        var src = sourceFormat.TrimStart('.').ToLowerInvariant();
        var tgt = targetFormat.TrimStart('.').ToLowerInvariant();

        // Image to Image or Image to PDF
        if (ImageExts.Contains(src) && ImageExts.Contains(tgt)) return true;

        // Excel <-> CSV/JSON
        if ((src == "xlsx" && (tgt is "csv" or "tsv" or "json")) ||
            ((src is "csv" or "tsv") && tgt == "xlsx")) return true;

        // Docx / PDF / Markdown / HTML
        if (DocExts.Contains(src) && DocExts.Contains(tgt)) return true;

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
            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            progress?.Report(10);
            cancellationToken.ThrowIfCancellationRequested();

            var scriptFile = FindScriptPath();
            if (!File.Exists(scriptFile))
            {
                return ConversionResult.Fail(sourcePath, destinationPath, $"Python bridge script not found at '{scriptFile}'.", sw.Elapsed);
            }

            var args = new StringBuilder();
            args.Append($"\"{scriptFile}\" ");
            args.Append($"--source \"{sourcePath}\" ");
            args.Append($"--target \"{destinationPath}\" ");
            args.Append($"--quality {options.Quality} ");

            if (options.ResizeWidth.HasValue)
                args.Append($"--width {options.ResizeWidth.Value} ");
            if (options.ResizeHeight.HasValue)
                args.Append($"--height {options.ResizeHeight.Value} ");

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
                return ConversionResult.Ok(sourcePath, destinationPath, sw.Elapsed, outBytes);
            }
            else
            {
                var err = errorSb.ToString().Trim();
                if (string.IsNullOrEmpty(err)) err = $"Process exited with code {process.ExitCode}";
                return ConversionResult.Fail(sourcePath, destinationPath, err, sw.Elapsed);
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            return ConversionResult.Fail(sourcePath, destinationPath, ex.Message, sw.Elapsed);
        }
    }

    private static string FindScriptPath()
    {
        // 1. Direct relative from project root
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
