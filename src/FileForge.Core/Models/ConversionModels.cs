namespace FileForge.Core.Models;

public enum ConversionStatus
{
    Queued,
    Processing,
    Completed,
    Failed,
    Cancelled
}

public sealed class ConversionOptions
{
    public int Quality { get; set; } = 85;
    public int? ResizeWidth { get; set; }
    public int? ResizeHeight { get; set; }
    public bool MaintainAspectRatio { get; set; } = true;
    public string Delimiter { get; set; } = ",";
    public bool Indented { get; set; } = true;
    public bool Overwrite { get; set; } = true;
    public string? TargetFormat { get; set; }
    public Dictionary<string, string> CustomSettings { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static ConversionOptions Default => new();
}

public sealed class ConversionResult
{
    public bool Success { get; init; }
    public string SourcePath { get; init; } = string.Empty;
    public string DestinationPath { get; init; } = string.Empty;
    public TimeSpan ElapsedTime { get; init; }
    public long BytesWritten { get; init; }
    public string? ErrorMessage { get; init; }

    public static ConversionResult Ok(string source, string dest, TimeSpan elapsed, long bytes = 0) =>
        new() { Success = true, SourcePath = source, DestinationPath = dest, ElapsedTime = elapsed, BytesWritten = bytes };

    public static ConversionResult Fail(string source, string dest, string error, TimeSpan elapsed) =>
        new() { Success = false, SourcePath = source, DestinationPath = dest, ErrorMessage = error, ElapsedTime = elapsed };
}

public sealed class ConversionItem
{
    public Guid Id { get; } = Guid.NewGuid();
    public string SourcePath { get; set; } = string.Empty;
    public string FileName => Path.GetFileName(SourcePath);
    public long FileSizeBytes { get; set; }
    public string SourceFormat { get; set; } = string.Empty;
    public string TargetFormat { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public ConversionStatus Status { get; set; } = ConversionStatus.Queued;
    public double Progress { get; set; }
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public ConversionOptions Options { get; set; } = new();
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
