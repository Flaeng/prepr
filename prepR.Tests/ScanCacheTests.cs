using PrepR;

namespace prepR.Tests;

public class ScanCacheTests : IDisposable
{
    private readonly TempFileTree _tree = new();

    public void Dispose() => _tree.Dispose();

    [Fact]
    public void Load_WhenNoCacheFile_ReturnsEmptyCache()
    {
        var cache = ScanCache.Load(_tree.RootPath);

        var result = cache.TryGetCached("nonexistent.cs", out var lines, out var lineCount);

        Assert.False(result);
        Assert.Empty(lines);
        Assert.Equal(0, lineCount);
    }

    [Fact]
    public void SaveAndLoad_RoundTrip()
    {
        _tree.AddFile("test.cs", ["line1", "line2", "line3"]);
        var filePath = Path.Combine(_tree.RootPath, "test.cs");

        var cache = ScanCache.Load(_tree.RootPath);
        var indexed = new[]
        {
            new IndexedLine(1, "line1"),
            new IndexedLine(2, "line2"),
            new IndexedLine(3, "line3")
        };
        cache.Update(filePath, indexed, 3);
        cache.Save(_tree.RootPath);

        // Reload from disk
        var cache2 = ScanCache.Load(_tree.RootPath);
        var hit = cache2.TryGetCached(filePath, out var cachedLines, out var cachedLineCount);

        Assert.True(hit);
        Assert.Equal(3, cachedLineCount);
        Assert.Equal(3, cachedLines.Length);
        Assert.Equal("line1", cachedLines[0].Text);
        Assert.Equal(2, cachedLines[1].LineNumber);
    }

    [Fact]
    public void TryGetCached_ReturnsFalse_WhenFileModified()
    {
        _tree.AddFile("test.cs", ["line1", "line2"]);
        var filePath = Path.Combine(_tree.RootPath, "test.cs");

        var cache = ScanCache.Load(_tree.RootPath);
        var indexed = new[]
        {
            new IndexedLine(1, "line1"),
            new IndexedLine(2, "line2")
        };
        cache.Update(filePath, indexed, 2);
        cache.Save(_tree.RootPath);

        // Modify the file (change timestamp)
        Thread.Sleep(100); // ensure timestamp differs
        File.WriteAllText(filePath, "modified content");

        var cache2 = ScanCache.Load(_tree.RootPath);
        var hit = cache2.TryGetCached(filePath, out _, out _);

        Assert.False(hit);
    }

    [Fact]
    public void TryGetCached_ReturnsFalse_WhenFileDeleted()
    {
        _tree.AddFile("test.cs", ["line1"]);
        var filePath = Path.Combine(_tree.RootPath, "test.cs");

        var cache = ScanCache.Load(_tree.RootPath);
        cache.Update(filePath, [new IndexedLine(1, "line1")], 1);
        cache.Save(_tree.RootPath);

        File.Delete(filePath);

        var cache2 = ScanCache.Load(_tree.RootPath);
        var hit = cache2.TryGetCached(filePath, out _, out _);

        Assert.False(hit);
    }

    [Fact]
    public void ModifyingOneFile_DoesNotInvalidateOthers()
    {
        _tree.AddFile("a.cs", ["alpha"]);
        _tree.AddFile("b.cs", ["beta"]);
        var pathA = Path.Combine(_tree.RootPath, "a.cs");
        var pathB = Path.Combine(_tree.RootPath, "b.cs");

        var cache = ScanCache.Load(_tree.RootPath);
        cache.Update(pathA, [new IndexedLine(1, "alpha")], 1);
        cache.Update(pathB, [new IndexedLine(1, "beta")], 1);
        cache.Save(_tree.RootPath);

        // Modify only file A
        Thread.Sleep(100);
        File.WriteAllText(pathA, "changed");

        var cache2 = ScanCache.Load(_tree.RootPath);

        Assert.False(cache2.TryGetCached(pathA, out _, out _));
        Assert.True(cache2.TryGetCached(pathB, out var bLines, out _));
        Assert.Equal("beta", bLines[0].Text);
    }

    [Fact]
    public void Load_WithCorruptedCacheFile_ReturnsEmptyCache()
    {
        File.WriteAllText(Path.Combine(_tree.RootPath, ".prepr-cache"), "not valid json{{{");

        var cache = ScanCache.Load(_tree.RootPath);
        var hit = cache.TryGetCached("any.cs", out _, out _);

        Assert.False(hit);
    }
}
