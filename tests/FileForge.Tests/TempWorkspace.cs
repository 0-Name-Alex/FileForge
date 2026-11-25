namespace FileForge.Tests;

internal sealed class TempWorkspace : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "FileForge_Tests_" + Guid.NewGuid().ToString("N"));

    public TempWorkspace()
    {
        Directory.CreateDirectory(Root);
    }

    public string PathFor(string relativeName)
    {
        var full = Path.Combine(Root, relativeName);
        var dir = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        return full;
    }

    public string WriteText(string relativeName, string content)
    {
        var path = PathFor(relativeName);
        File.WriteAllText(path, content);
        return path;
    }

    public string WriteBytes(string relativeName, byte[] bytes)
    {
        var path = PathFor(relativeName);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
        catch
        {
            // Best-effort cleanup
        }
    }
}
