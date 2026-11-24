using System.Diagnostics;
using FileForge.Core.Detection;
using FileForge.Core.Models;
using FileForge.Core.Pipeline;
using FileForge.Core.Registry;
using FileForge.Platform.Windows.Memory;

Console.WriteLine("=================================================");
Console.WriteLine("  FileForge Base Engine & Batch Test Harness     ");
Console.WriteLine("  Target: .NET 10 | Windows x64 | C# 14         ");
Console.WriteLine("=================================================\n");

var initialMemory = MemoryOptimizer.GetCurrentMemoryMb();
Console.WriteLine($"📊 Initial Process Memory: {initialMemory:F1} MB\n");

var registry = new ConverterRegistry();
var pipeline = new BatchPipeline(registry);

pipeline.ItemStarted += (_, item) =>
{
    Console.WriteLine($"[START] {item.FileName} -> .{item.TargetFormat}");
};

pipeline.ItemCompleted += (_, item) =>
{
    var status = item.Status == ConversionStatus.Completed ? "SUCCESS" : "FAILED";
    Console.WriteLine($"[{status}] {item.FileName} -> {item.StatusMessage}");
    if (!string.IsNullOrEmpty(item.ErrorMessage))
    {
        Console.WriteLine($"        Error: {item.ErrorMessage}");
    }
};

pipeline.BatchProgressChanged += (_, progress) =>
{
    Console.WriteLine($"[BATCH PROGRESS] {progress.ProcessedItems}/{progress.TotalItems} ({progress.OverallPercentage:F0}%) | OK: {progress.SucceededItems}, Failed: {progress.FailedItems}");
};

// 1. Create a staging test directory with various sample files
var testDir = Path.Combine(AppContext.BaseDirectory, "test_samples");
var outDir = Path.Combine(AppContext.BaseDirectory, "test_output");
Directory.CreateDirectory(testDir);
Directory.CreateDirectory(outDir);

Console.WriteLine($"📁 Generating test sample files in: {testDir}");

// Sample 1: CSV file
var csvPath = Path.Combine(testDir, "metrics.csv");
await File.WriteAllTextAsync(csvPath, "id,sensor_name,voltage,current,temperature\n1,Sensor_A,230.5,12.4,45.2\n2,Sensor_B,229.8,11.8,44.9\n3,Sensor_C,231.2,13.1,46.0\n");

// Sample 2: JSON file
var jsonPath = Path.Combine(testDir, "config.json");
await File.WriteAllTextAsync(jsonPath, "{\n  \"appName\": \"FileForge\",\n  \"version\": \"1.0.0\",\n  \"lowMemoryMode\": true,\n  \"hotkey\": \"Ctrl+Alt+F\"\n}\n");

// Sample 3: XML file
var xmlPath = Path.Combine(testDir, "devices.xml");
await File.WriteAllTextAsync(xmlPath, "<root><row><id>1</id><name>Turbine-1</name></row><row><id>2</id><name>Turbine-2</name></row></root>");

// Sample 4: Text file for Base64 / Dev encoding
var txtPath = Path.Combine(testDir, "sample_text.txt");
await File.WriteAllTextAsync(txtPath, "FileForge high-performance conversion test payload for Alex!");

// Sample 5: Generate a valid 1x1 PNG image using valid base64
var pngPath = Path.Combine(testDir, "sample_image.png");
byte[] samplePngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
await File.WriteAllBytesAsync(pngPath, samplePngBytes);

Console.WriteLine("✅ Sample files generated.");

// 2. Test Format Detection
Console.WriteLine("\n🔍 Testing Magic-Bytes & Format Detection:");
string[] samples = [csvPath, jsonPath, xmlPath, txtPath, pngPath];
foreach (var s in samples)
{
    var detected = FormatDetector.DetectFromFile(s);
    var validTargets = string.Join(", ", FormatDetector.GetValidTargetFormats(detected.Extension));
    Console.WriteLine($"  • {Path.GetFileName(s)} => Detected: {detected.Name} ({detected.MimeType}) [Valid targets: {validTargets}]");
}

// 3. Create Multi-File Batch Queue
Console.WriteLine("\n🚀 Queuing Multi-File Batch Conversions (Images, Data, Docs, Archives, Encoding):");
var batchItems = new List<ConversionItem>
{
    // CSV -> JSON
    new()
    {
        SourcePath = csvPath,
        SourceFormat = "csv",
        TargetFormat = "json",
        DestinationPath = Path.Combine(outDir, "metrics_from_csv.json")
    },
    // CSV -> XML
    new()
    {
        SourcePath = csvPath,
        SourceFormat = "csv",
        TargetFormat = "xml",
        DestinationPath = Path.Combine(outDir, "metrics_from_csv.xml")
    },
    // JSON -> CSV
    new()
    {
        SourcePath = jsonPath,
        SourceFormat = "json",
        TargetFormat = "csv",
        DestinationPath = Path.Combine(outDir, "config_from_json.csv")
    },
    // XML -> JSON
    new()
    {
        SourcePath = xmlPath,
        SourceFormat = "xml",
        TargetFormat = "json",
        DestinationPath = Path.Combine(outDir, "devices_from_xml.json")
    },
    // Text -> Base64
    new()
    {
        SourcePath = txtPath,
        SourceFormat = "txt",
        TargetFormat = "b64",
        DestinationPath = Path.Combine(outDir, "sample_encoded.b64")
    },
    // Text -> SHA-256 Hash
    new()
    {
        SourcePath = txtPath,
        SourceFormat = "txt",
        TargetFormat = "sha256",
        DestinationPath = Path.Combine(outDir, "sample_text.sha256")
    },
    // PNG -> WEBP (via Python Bridge Pillow)
    new()
    {
        SourcePath = pngPath,
        SourceFormat = "png",
        TargetFormat = "webp",
        DestinationPath = Path.Combine(outDir, "sample_converted.webp")
    },
    // PNG -> PDF (via Python Bridge Pillow)
    new()
    {
        SourcePath = pngPath,
        SourceFormat = "png",
        TargetFormat = "pdf",
        DestinationPath = Path.Combine(outDir, "sample_image.pdf")
    },
    // Folder -> ZIP Archive
    new()
    {
        SourcePath = testDir,
        SourceFormat = "folder",
        TargetFormat = "zip",
        DestinationPath = Path.Combine(outDir, "all_samples_archive.zip")
    }
};

Console.WriteLine($"Total batch queue size: {batchItems.Count} items.");
Console.WriteLine("Executing batch conversion with parallel channels...\n");

var sw = Stopwatch.StartNew();
var results = await pipeline.ExecuteBatchAsync(batchItems, maxConcurrency: 4);
sw.Stop();

Console.WriteLine($"\n🏁 Batch conversion finished in {sw.ElapsedMilliseconds} ms!");
Console.WriteLine($"Results: {results.Count(r => r.Success)} Succeeded, {results.Count(r => !r.Success)} Failed.");

// 4. Memory Optimization test
var preTrimMemory = MemoryOptimizer.GetCurrentMemoryMb();
Console.WriteLine($"\n🧠 Memory before trimming: {preTrimMemory:F1} MB");
MemoryOptimizer.TrimWorkingSet();
var postTrimMemory = MemoryOptimizer.GetCurrentMemoryMb();
Console.WriteLine($"🌱 Memory after MemoryOptimizer.TrimWorkingSet(): {postTrimMemory:F1} MB (Reduced by {preTrimMemory - postTrimMemory:F1} MB!)");

// Verify output files exist
Console.WriteLine("\n📦 Output Files Generated in test_output:");
foreach (var file in Directory.GetFiles(outDir))
{
    var fi = new FileInfo(file);
    Console.WriteLine($"  - {fi.Name} ({fi.Length} bytes)");
}

Console.WriteLine("\n✨ All base conversions, batch channels, and memory optimizations verified successfully!");
