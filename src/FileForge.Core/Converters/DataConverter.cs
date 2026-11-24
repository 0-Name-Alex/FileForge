using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using FileForge.Core.Interfaces;
using FileForge.Core.Models;

namespace FileForge.Core.Converters;

public sealed class DataConverter : IFileConverter
{
    public string Name => "Data & Spreadsheet Converter";
    public FileCategory Category => FileCategory.Data;

    public IReadOnlyList<string> SupportedInputFormats => ["json", "csv", "tsv", "xml"];
    public IReadOnlyList<string> SupportedOutputFormats => ["json", "csv", "tsv", "xml", "txt"];

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

            if (srcExt == "json")
            {
                var jsonText = await File.ReadAllTextAsync(sourcePath, cancellationToken);
                progress?.Report(40);

                if (tgtExt == "xml")
                {
                    var xml = JsonToXml(jsonText);
                    await File.WriteAllTextAsync(destinationPath, xml, cancellationToken);
                }
                else if (tgtExt is "csv" or "tsv")
                {
                    var delimiter = tgtExt == "tsv" ? "\t" : options.Delimiter;
                    var csv = JsonToCsv(jsonText, delimiter);
                    await File.WriteAllTextAsync(destinationPath, csv, cancellationToken);
                }
                else if (tgtExt is "json" or "txt")
                {
                    // Format or minify
                    using var doc = JsonDocument.Parse(jsonText);
                    var formatted = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = options.Indented });
                    await File.WriteAllTextAsync(destinationPath, formatted, cancellationToken);
                }
            }
            else if (srcExt is "csv" or "tsv")
            {
                var inDelimiter = srcExt == "tsv" ? "\t" : options.Delimiter;
                var lines = await File.ReadAllLinesAsync(sourcePath, cancellationToken);
                progress?.Report(40);

                if (tgtExt == "json")
                {
                    var json = CsvToJson(lines, inDelimiter, options.Indented);
                    await File.WriteAllTextAsync(destinationPath, json, cancellationToken);
                }
                else if (tgtExt == "xml")
                {
                    var xml = CsvToXml(lines, inDelimiter);
                    await File.WriteAllTextAsync(destinationPath, xml, cancellationToken);
                }
                else if (tgtExt is "csv" or "tsv" or "txt")
                {
                    var outDelimiter = tgtExt == "tsv" ? "\t" : options.Delimiter;
                    var converted = ConvertDelimiters(lines, inDelimiter, outDelimiter);
                    await File.WriteAllTextAsync(destinationPath, converted, cancellationToken);
                }
            }
            else if (srcExt == "xml")
            {
                var xmlText = await File.ReadAllTextAsync(sourcePath, cancellationToken);
                progress?.Report(40);

                if (tgtExt == "json")
                {
                    var json = XmlToJson(xmlText, options.Indented);
                    await File.WriteAllTextAsync(destinationPath, json, cancellationToken);
                }
                else if (tgtExt is "xml" or "txt")
                {
                    var xdoc = XDocument.Parse(xmlText);
                    await File.WriteAllTextAsync(destinationPath, xdoc.ToString(options.Indented ? SaveOptions.None : SaveOptions.DisableFormatting), cancellationToken);
                }
            }

            progress?.Report(100);
            sw.Stop();
            var bytes = new FileInfo(destinationPath).Length;
            return ConversionResult.Ok(sourcePath, destinationPath, sw.Elapsed, bytes);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return ConversionResult.Fail(sourcePath, destinationPath, ex.Message, sw.Elapsed);
        }
    }

    private static string JsonToCsv(string jsonText, string delimiter)
    {
        var node = JsonNode.Parse(jsonText);
        if (node is not JsonArray array || array.Count == 0)
        {
            return "Value\n" + node?.ToString();
        }

        var headers = new HashSet<string>();
        foreach (var item in array)
        {
            if (item is JsonObject obj)
            {
                foreach (var prop in obj)
                    headers.Add(prop.Key);
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(delimiter, headers.Select(EscapeCsv)));

        foreach (var item in array)
        {
            if (item is JsonObject obj)
            {
                var row = headers.Select(h => obj.TryGetPropertyValue(h, out var v) ? EscapeCsv(v?.ToString() ?? "") : "");
                sb.AppendLine(string.Join(delimiter, row));
            }
        }

        return sb.ToString();
    }

    private static string CsvToJson(string[] lines, string delimiter, bool indented)
    {
        if (lines.Length == 0) return "[]";
        var headers = lines[0].Split(delimiter).Select(h => h.Trim('"', ' ')).ToArray();

        var array = new JsonArray();
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var parts = lines[i].Split(delimiter).Select(p => p.Trim('"', ' ')).ToArray();
            var obj = new JsonObject();
            for (int h = 0; h < headers.Length; h++)
            {
                var val = h < parts.Length ? parts[h] : "";
                obj[headers[h]] = val;
            }
            array.Add(obj);
        }

        return array.ToJsonString(new JsonSerializerOptions { WriteIndented = indented });
    }

    private static string CsvToXml(string[] lines, string delimiter)
    {
        if (lines.Length == 0) return "<root />";
        var headers = lines[0].Split(delimiter).Select(h => CleanXmlTag(h.Trim('"', ' '))).ToArray();

        var root = new XElement("root");
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var parts = lines[i].Split(delimiter).Select(p => p.Trim('"', ' ')).ToArray();
            var row = new XElement("row");
            for (int h = 0; h < headers.Length; h++)
            {
                var val = h < parts.Length ? parts[h] : "";
                row.Add(new XElement(headers[h], val));
            }
            root.Add(row);
        }

        return root.ToString();
    }

    private static string JsonToXml(string jsonText)
    {
        var node = JsonNode.Parse(jsonText);
        var root = new XElement("root");
        BuildXmlFromNode(root, node, "item");
        return root.ToString();
    }

    private static void BuildXmlFromNode(XElement parent, JsonNode? node, string defaultKey)
    {
        if (node is JsonObject obj)
        {
            foreach (var prop in obj)
            {
                var elem = new XElement(CleanXmlTag(prop.Key));
                BuildXmlFromNode(elem, prop.Value, "item");
                parent.Add(elem);
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                var elem = new XElement(CleanXmlTag(defaultKey));
                BuildXmlFromNode(elem, item, "subitem");
                parent.Add(elem);
            }
        }
        else
        {
            parent.Value = node?.ToString() ?? "";
        }
    }

    private static string XmlToJson(string xmlText, bool indented)
    {
        var doc = XDocument.Parse(xmlText);
        var obj = NodeToJson(doc.Root);
        return obj.ToJsonString(new JsonSerializerOptions { WriteIndented = indented });
    }

    private static JsonNode NodeToJson(XElement? element)
    {
        if (element == null) return new JsonObject();
        var obj = new JsonObject();
        if (!element.HasElements)
            return JsonValue.Create(element.Value)!;

        foreach (var child in element.Elements())
        {
            obj[child.Name.LocalName] = NodeToJson(child);
        }
        return obj;
    }

    private static string ConvertDelimiters(string[] lines, string inDelim, string outDelim)
    {
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            var parts = line.Split(inDelim);
            sb.AppendLine(string.Join(outDelim, parts));
        }
        return sb.ToString();
    }

    private static string CleanXmlTag(string name)
    {
        var clean = new string(name.Where(char.IsLetterOrDigit).ToArray());
        return string.IsNullOrEmpty(clean) ? "field" : (char.IsDigit(clean[0]) ? "_" + clean : clean);
    }

    private static string EscapeCsv(string val) =>
        val.Contains(',') || val.Contains('"') || val.Contains('\n')
            ? $"\"{val.Replace("\"", "\"\"")}\""
            : val;
}
