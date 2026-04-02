namespace prepr.Tests;

/// <summary>
/// Creates a temporary directory tree with files of known content for testing.
/// Disposes by deleting the entire temp directory.
/// </summary>
public sealed class TempFileTree : IDisposable
{
    public string RootPath { get; }

    public TempFileTree()
    {
        RootPath = Path.Combine(Path.GetTempPath(), "prepr_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(RootPath);
    }

    public string AddFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(RootPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    public string AddFile(string relativePath, string[] lines)
    {
        return AddFile(relativePath, string.Join(Environment.NewLine, lines));
    }

    public void Dispose()
    {
        try { Directory.Delete(RootPath, recursive: true); }
        catch { /* best effort cleanup */ }
    }
}
