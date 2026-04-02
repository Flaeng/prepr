using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prepr;

public class ScanCache
{
    private const string CacheFileName = ".prepr-cache";

    private readonly ConcurrentDictionary<string, CachedFileEntry> _entries;

    private ScanCache(ConcurrentDictionary<string, CachedFileEntry> entries)
    {
        _entries = entries;
    }

    public bool TryGetCached(string filePath, out IndexedLine[] lines, out int totalLineCount)
    {
        if (_entries.TryGetValue(filePath, out var entry))
        {
            try
            {
                var lastWrite = File.GetLastWriteTimeUtc(filePath);
                if (lastWrite == entry.LastWriteTimeUtc)
                {
                    lines = entry.Lines.Select(l => new IndexedLine(l.LineNumber, l.Text)).ToArray();
                    totalLineCount = entry.TotalLineCount;
                    return true;
                }
            }
            catch
            {
                // File may have been deleted; treat as stale
            }
        }

        lines = [];
        totalLineCount = 0;
        return false;
    }

    public void Update(string filePath, IndexedLine[] lines, int totalLineCount)
    {
        try
        {
            var lastWrite = File.GetLastWriteTimeUtc(filePath);
            _entries[filePath] = new CachedFileEntry(
                lastWrite,
                totalLineCount,
                lines.Select(l => new CachedLine(l.LineNumber, l.Text)).ToArray());
        }
        catch
        {
            // If we can't get file info, skip caching this file
        }
    }

    public static ScanCache Load(string rootPath)
    {
        var cachePath = Path.Combine(rootPath, CacheFileName);
        if (!File.Exists(cachePath))
            return new ScanCache(new ConcurrentDictionary<string, CachedFileEntry>());

        try
        {
            var json = File.ReadAllText(cachePath);
            var entries = JsonSerializer.Deserialize<Dictionary<string, CachedFileEntry>>(json) ?? [];
            return new ScanCache(new ConcurrentDictionary<string, CachedFileEntry>(entries));
        }
        catch
        {
            return new ScanCache(new ConcurrentDictionary<string, CachedFileEntry>());
        }
    }

    public void Save(string rootPath)
    {
        var cachePath = Path.Combine(rootPath, CacheFileName);
        try
        {
            var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = false });
            File.WriteAllText(cachePath, json);
        }
        catch (IOException)
        {
            // Best-effort: don't fail the scan if cache can't be written
        }
    }

    internal record CachedFileEntry(
        [property: JsonPropertyName("lastWriteTimeUtc")] DateTime LastWriteTimeUtc,
        [property: JsonPropertyName("totalLineCount")] int TotalLineCount,
        [property: JsonPropertyName("lines")] CachedLine[] Lines);

    internal record CachedLine(
        [property: JsonPropertyName("lineNumber")] int LineNumber,
        [property: JsonPropertyName("text")] string Text);
}
