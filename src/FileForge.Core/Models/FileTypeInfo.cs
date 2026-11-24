namespace FileForge.Core.Models;

public enum FileCategory
{
    Image,
    Document,
    Data,
    Archive,
    Developer,
    Audio,
    Other
}

public sealed record FileTypeInfo(
    string Extension,
    string Name,
    FileCategory Category,
    string MimeType,
    byte[]? MagicBytes = null,
    int MagicOffset = 0);
