using FileForge.Core.Models;

namespace FileForge.Core.Detection;

public static class FormatDetector
{
    private static readonly Dictionary<string, FileTypeInfo> KnownExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        ["png"] = new("png", "Portable Network Graphics", FileCategory.Image, "image/png"),
        ["jpg"] = new("jpg", "JPEG Image", FileCategory.Image, "image/jpeg"),
        ["jpeg"] = new("jpeg", "JPEG Image", FileCategory.Image, "image/jpeg"),
        ["webp"] = new("webp", "WebP Image", FileCategory.Image, "image/webp"),
        ["bmp"] = new("bmp", "Bitmap Image", FileCategory.Image, "image/bmp"),
        ["ico"] = new("ico", "Icon", FileCategory.Image, "image/x-icon"),
        ["gif"] = new("gif", "Graphics Interchange Format", FileCategory.Image, "image/gif"),
        ["tiff"] = new("tiff", "Tagged Image File Format", FileCategory.Image, "image/tiff"),
        ["tif"] = new("tif", "Tagged Image File Format", FileCategory.Image, "image/tiff"),
        ["svg"] = new("svg", "Scalable Vector Graphics", FileCategory.Image, "image/svg+xml"),

        // Documents
        ["pdf"] = new("pdf", "Portable Document Format", FileCategory.Document, "application/pdf"),
        ["docx"] = new("docx", "Microsoft Word Document", FileCategory.Document, "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
        ["doc"] = new("doc", "Legacy Word Document", FileCategory.Document, "application/msword"),
        ["pptx"] = new("pptx", "PowerPoint Presentation", FileCategory.Document, "application/vnd.openxmlformats-officedocument.presentationml.presentation"),
        ["ppt"] = new("ppt", "Legacy PowerPoint Presentation", FileCategory.Document, "application/vnd.ms-powerpoint"),
        ["ppsx"] = new("ppsx", "PowerPoint SlideShow", FileCategory.Document, "application/vnd.openxmlformats-officedocument.presentationml.slideshow"),
        ["txt"] = new("txt", "Plain Text Document", FileCategory.Document, "text/plain"),
        ["md"] = new("md", "Markdown Document", FileCategory.Document, "text/markdown"),
        ["html"] = new("html", "HTML Document", FileCategory.Document, "text/html"),
        ["htm"] = new("htm", "HTML Document", FileCategory.Document, "text/html"),

        // Data / Spreadsheets
        ["csv"] = new("csv", "Comma Separated Values", FileCategory.Data, "text/csv"),
        ["tsv"] = new("tsv", "Tab Separated Values", FileCategory.Data, "text/tab-separated-values"),
        ["xlsx"] = new("xlsx", "Excel Spreadsheet", FileCategory.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
        ["xls"] = new("xls", "Legacy Excel Spreadsheet", FileCategory.Data, "application/vnd.ms-excel"),
        ["json"] = new("json", "JavaScript Object Notation", FileCategory.Data, "application/json"),
        ["xml"] = new("xml", "Extensible Markup Language", FileCategory.Data, "application/xml"),
        ["yaml"] = new("yaml", "YAML Data", FileCategory.Data, "application/x-yaml"),
        ["yml"] = new("yml", "YAML Data", FileCategory.Data, "application/x-yaml"),

        // Archives
        ["zip"] = new("zip", "ZIP Archive", FileCategory.Archive, "application/zip"),
        ["tar"] = new("tar", "Tape Archive", FileCategory.Archive, "application/x-tar"),
        ["gz"] = new("gz", "GZip Archive", FileCategory.Archive, "application/gzip"),
        ["7z"] = new("7z", "7-Zip Archive", FileCategory.Archive, "application/x-7z-compressed"),

        // Dev
        ["b64"] = new("b64", "Base64 Encoded File", FileCategory.Developer, "text/plain"),
        ["hex"] = new("hex", "Hexadecimal Dump", FileCategory.Developer, "text/plain"),
    };

    public static FileTypeInfo DetectFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            var fallbackExt = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
            return KnownExtensions.GetValueOrDefault(fallbackExt, new FileTypeInfo(fallbackExt, fallbackExt.ToUpperInvariant(), FileCategory.Other, "application/octet-stream"));
        }

        // 1. Inspect magic bytes from file header
        byte[] buffer = new byte[512];
        int bytesRead = 0;
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            bytesRead = fs.Read(buffer, 0, buffer.Length);
        }
        catch
        {
            // Fall back to extension
        }

        if (bytesRead >= 4)
        {
            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (bytesRead >= 8 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
                return KnownExtensions["png"];

            // JPEG: FF D8 FF
            if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
                return KnownExtensions["jpg"];

            // GIF: GIF87a or GIF89a
            if (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x38)
                return KnownExtensions["gif"];

            // WEBP: RIFF....WEBP
            if (bytesRead >= 12 && buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 &&
                buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
                return KnownExtensions["webp"];

            // BMP: 42 4D (BM)
            if (buffer[0] == 0x42 && buffer[1] == 0x4D)
                return KnownExtensions["bmp"];

            // TIFF
            if ((buffer[0] == 0x49 && buffer[1] == 0x49 && buffer[2] == 0x2A && buffer[3] == 0x00) ||
                (buffer[0] == 0x4D && buffer[1] == 0x4D && buffer[2] == 0x00 && buffer[3] == 0x2A))
                return KnownExtensions["tiff"];

            // PDF: %PDF (25 50 44 46)
            if (buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46)
                return KnownExtensions["pdf"];

            // GZ: 1F 8B
            if (buffer[0] == 0x1F && buffer[1] == 0x8B)
                return KnownExtensions["gz"];

            // 7Z: 37 7A BC AF 27 1C
            if (bytesRead >= 6 && buffer[0] == 0x37 && buffer[1] == 0x7A && buffer[2] == 0xBC && buffer[3] == 0xAF && buffer[4] == 0x27 && buffer[5] == 0x1C)
                return KnownExtensions["7z"];

            // ZIP / Office Open XML (PK..)
            if (buffer[0] == 0x50 && buffer[1] == 0x4B && (buffer[2] == 0x03 || buffer[2] == 0x05 || buffer[2] == 0x07))
            {
                var ext = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
                if (ext is "docx" or "xlsx" or "pptx" or "ppsx") return KnownExtensions[ext];
                return KnownExtensions["zip"];
            }
        }

        // 2. Fall back to file extension
        var extension = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
        if (KnownExtensions.TryGetValue(extension, out var detected))
            return detected;

        return new FileTypeInfo(extension, string.IsNullOrEmpty(extension) ? "Binary" : extension.ToUpperInvariant(), FileCategory.Other, "application/octet-stream");
    }

    public static IReadOnlyList<string> GetValidTargetFormats(string sourceExtension)
    {
        var ext = sourceExtension.TrimStart('.').ToLowerInvariant();
        if (ext is "pptx" or "ppt" or "ppsx")
        {
            return ["pdf", "txt", "docx", "html", "md"];
        }

        if (KnownExtensions.TryGetValue(ext, out var info))
        {
            return info.Category switch
            {
                FileCategory.Image => ["png", "jpg", "webp", "bmp", "ico", "pdf", "tiff"],
                FileCategory.Document => ["pdf", "docx", "txt", "md", "html"],
                FileCategory.Data => ["json", "csv", "xlsx", "xml"],
                FileCategory.Archive => ["zip", "tar", "gz"],
                FileCategory.Developer => ["txt", "b64", "hex", "sha256", "md5"],
                _ => ["txt", "b64", "hex", "zip"]
            };
        }

        return ["txt", "b64", "hex", "zip"];
    }

    public static FileTypeInfo? GetInfoByExtension(string ext) =>
        KnownExtensions.GetValueOrDefault(ext.TrimStart('.').ToLowerInvariant());
}
