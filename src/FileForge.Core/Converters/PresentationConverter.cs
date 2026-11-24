using System.Diagnostics;
using FileForge.Core.Interfaces;
using FileForge.Core.Models;

namespace FileForge.Core.Converters;

public sealed class PresentationConverter : IFileConverter
{
    public string Name => "PowerPoint Presentation Converter (PPTX, PPT -> PDF, TXT)";
    public FileCategory Category => FileCategory.Document;

    public IReadOnlyList<string> SupportedInputFormats => ["pptx", "ppt", "ppsx"];
    public IReadOnlyList<string> SupportedOutputFormats => ["pdf", "txt", "md", "html"];

    public bool CanConvert(string sourceFormat, string targetFormat)
    {
        var src = sourceFormat.TrimStart('.').ToLowerInvariant();
        var tgt = targetFormat.TrimStart('.').ToLowerInvariant();
        return SupportedInputFormats.Contains(src) && SupportedOutputFormats.Contains(tgt);
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

            if (tgtExt == "pdf")
            {
                progress?.Report(25);
                // Perform PowerPoint COM export (ppSaveAsPDF = 32)
                var success = await ConvertViaPowerPointComAsync(sourcePath, destinationPath, cancellationToken);
                progress?.Report(90);

                if (!success || !File.Exists(destinationPath))
                {
                    // Fallback to Python bridge runner if COM direct failed
                    success = await ConvertViaPythonBridgeAsync(sourcePath, destinationPath, options, cancellationToken);
                }

                sw.Stop();
                if (success && File.Exists(destinationPath))
                {
                    progress?.Report(100);
                    var bytes = new FileInfo(destinationPath).Length;
                    return ConversionResult.Ok(sourcePath, destinationPath, sw.Elapsed, bytes);
                }
                else
                {
                    return ConversionResult.Fail(sourcePath, destinationPath, "PowerPoint export to PDF failed. Ensure PowerPoint or Microsoft Office is installed.", sw.Elapsed);
                }
            }
            else
            {
                // For TXT, MD, HTML slide text extraction, use Python bridge
                progress?.Report(30);
                var success = await ConvertViaPythonBridgeAsync(sourcePath, destinationPath, options, cancellationToken);
                sw.Stop();

                if (success && File.Exists(destinationPath))
                {
                    progress?.Report(100);
                    var bytes = new FileInfo(destinationPath).Length;
                    return ConversionResult.Ok(sourcePath, destinationPath, sw.Elapsed, bytes);
                }
                else
                {
                    return ConversionResult.Fail(sourcePath, destinationPath, $"Failed to convert presentation to {tgtExt}.", sw.Elapsed);
                }
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            return ConversionResult.Fail(sourcePath, destinationPath, ex.Message, sw.Elapsed);
        }
    }

    private static async Task<bool> ConvertViaPowerPointComAsync(string sourcePath, string destinationPath, CancellationToken ct)
    {
        try
        {
            var absSource = Path.GetFullPath(sourcePath).Replace("'", "''");
            var absTarget = Path.GetFullPath(destinationPath).Replace("'", "''");

            // PowerShell script executing PowerPoint COM headless
            var psScript = $@"
$src = '{absSource}'
$dst = '{absTarget}'
$ppt = New-Object -ComObject PowerPoint.Application
try {{
    $pres = $ppt.Presentations.Open($src, [Microsoft.Office.Core.MsoTriState]::msoTrue, [Microsoft.Office.Core.MsoTriState]::msoFalse, [Microsoft.Office.Core.MsoTriState]::msoFalse)
    $pres.SaveAs($dst, 32)
    $pres.Close()
}} finally {{
    $ppt.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($ppt) | Out-Null
}}
";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -NonInteractive -Command \"{psScript}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();
            await process.WaitForExitAsync(ct);

            return process.ExitCode == 0 && File.Exists(destinationPath);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> ConvertViaPythonBridgeAsync(string sourcePath, string destinationPath, ConversionOptions options, CancellationToken ct)
    {
        try
        {
            var scriptPath = Path.Combine(AppContext.BaseDirectory, "converter_cli.py");
            if (!File.Exists(scriptPath))
            {
                scriptPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "FileForge.Adapters.Python", "converter_cli.py"));
            }

            if (!File.Exists(scriptPath)) return false;

            var psi = new ProcessStartInfo
            {
                FileName = "py",
                Arguments = $"-3.14 \"{scriptPath}\" --source \"{sourcePath}\" --target \"{destinationPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var proc = new Process { StartInfo = psi };
            proc.Start();
            await proc.WaitForExitAsync(ct);

            return proc.ExitCode == 0 && File.Exists(destinationPath);
        }
        catch
        {
            return false;
        }
    }
}
