using FileForge.Core.Converters;
using FileForge.Core.Detection;
using FileForge.Core.Interfaces;

namespace FileForge.Core.Registry;

public sealed class ConverterRegistry
{
    private readonly List<IFileConverter> _converters = [];

    public ConverterRegistry()
    {
        // Register default converters (order matters: native C# first for speed/low memory, then python bridge)
        _converters.Add(new DataConverter());
        _converters.Add(new ArchiveConverter());
        _converters.Add(new DeveloperConverter());
        _converters.Add(new PresentationConverter());
        _converters.Add(new PdfToolsConverter());
        _converters.Add(new PythonBridgeConverter());
    }

    public void RegisterConverter(IFileConverter converter) => _converters.Add(converter);

    public IFileConverter? FindConverter(string sourceExt, string targetExt)
    {
        var src = sourceExt.TrimStart('.').ToLowerInvariant();
        var tgt = targetExt.TrimStart('.').ToLowerInvariant();

        return _converters.FirstOrDefault(c => c.CanConvert(src, tgt));
    }

    public bool CanConvert(string sourceExt, string targetExt) =>
        FindConverter(sourceExt, targetExt) != null;

    public IReadOnlyList<string> GetAvailableTargetFormats(string sourceExt)
    {
        var src = sourceExt.TrimStart('.').ToLowerInvariant();
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var conv in _converters)
        {
            foreach (var tgt in conv.SupportedOutputFormats)
            {
                if (conv.CanConvert(src, tgt))
                    targets.Add(tgt);
            }
        }

        if (targets.Count == 0)
        {
            // Fallback from detector
            return FormatDetector.GetValidTargetFormats(sourceExt);
        }

        return targets.OrderBy(t => t).ToList();
    }
}
