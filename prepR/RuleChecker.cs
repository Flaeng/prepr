using System.Collections.Concurrent;

namespace Prepr;

public static class RuleChecker
{
    private const int DefaultMinConsecutiveLines = 5;

    public static ScanResult Run(IReadOnlyList<string> filePaths, int minConsecutiveLines = DefaultMinConsecutiveLines,
        TextWriter? progressWriter = null,
        ScanCache? cache = null)
    {
        var (fileLines, fileLineCounts, totalLines) = ReadFiles(filePaths, progressWriter, cache);
        var duplicates = DuplicateDetector.Detect(fileLines, minConsecutiveLines, progressWriter);
        return new ScanResult(duplicates, fileLines.Count, totalLines, fileLineCounts);
    }

    private static (IReadOnlyDictionary<string, IndexedLine[]> fileLines, IReadOnlyDictionary<string, int> fileLineCounts, int totalLines)
        ReadFiles(IReadOnlyList<string> filePaths,
            TextWriter? progressWriter,
            ScanCache? cache)
    {
        var fileLines = new ConcurrentDictionary<string, IndexedLine[]>();
        var fileLineCounts = new ConcurrentDictionary<string, int>();
        int totalLines = 0;
        int fileCount = filePaths.Count;
        int filesRead = 0;

        ProgressBar? bar = (progressWriter is not null && fileCount > 0)
            ? new ProgressBar(progressWriter, fileCount)
            : null;

        Parallel.ForEach(filePaths, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, path =>
        {
            try
            {
                IndexedLine[]? indexed = null;
                int lineCount;

                if (cache is not null && cache.TryGetCached(path, out var cachedLines, out var cachedLineCount))
                {
                    indexed = cachedLines;
                    lineCount = cachedLineCount;
                }
                else
                {
                    var raw = File.ReadAllLines(path);
                    lineCount = raw.Length;
                    indexed = raw
                        .Select((text, idx) => new IndexedLine(idx + 1, text))
                        .Where(l => !string.IsNullOrWhiteSpace(l.Text))
                        .ToArray();

                    cache?.Update(path, indexed, lineCount);
                }

                Interlocked.Add(ref totalLines, lineCount);
                fileLines[path] = indexed;
                fileLineCounts[path] = lineCount;
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Warning: Access denied to '{path}', skipping.");
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"Warning: Could not read '{path}': {ex.Message}");
            }

            int current = Interlocked.Increment(ref filesRead);
            bar?.Update(current, "Reading files...");
        });

        bar?.Complete();

        return (fileLines, fileLineCounts, totalLines);
    }
}
