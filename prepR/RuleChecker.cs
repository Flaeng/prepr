using System.Collections.Concurrent;

namespace Prepr;

public static class RuleChecker
{
    private const int DefaultMinConsecutiveLines = 5;

    public static ScanResult Run(IReadOnlyList<string> filePaths, int minConsecutiveLines = DefaultMinConsecutiveLines,
        TextWriter? progressWriter = null,
        ScanCache? cache = null)
    {
        var (fileLines, fileLineCounts, fileMaxNestingDepths, earlyReturnViolations, totalLines) = ReadFiles(filePaths, progressWriter, cache);
        var duplicates = DuplicateDetector.Detect(fileLines, minConsecutiveLines, progressWriter);
        return new ScanResult(duplicates, fileLines.Count, totalLines, fileLineCounts, fileMaxNestingDepths, earlyReturnViolations);
    }

    private static (IReadOnlyDictionary<string, IndexedLine[]> fileLines, IReadOnlyDictionary<string, int> fileLineCounts, IReadOnlyDictionary<string, (int MaxDepth, IReadOnlyList<(int LineNumber, int Depth)> LineDepths)> fileMaxNestingDepths, IReadOnlyDictionary<string, IReadOnlyList<EarlyReturnViolation>> earlyReturnViolations, int totalLines)
        ReadFiles(IReadOnlyList<string> filePaths,
            TextWriter? progressWriter,
            ScanCache? cache)
    {
        var fileLines = new ConcurrentDictionary<string, IndexedLine[]>();
        var fileLineCounts = new ConcurrentDictionary<string, int>();
        var fileMaxNestingDepths = new ConcurrentDictionary<string, (int MaxDepth, IReadOnlyList<(int LineNumber, int Depth)> LineDepths)>();
        var earlyReturnViolations = new ConcurrentDictionary<string, IReadOnlyList<EarlyReturnViolation>>();
        int totalLines = 0;
        int fileCount = filePaths.Count;
        int filesRead = 0;

        ProgressBar? bar = (progressWriter is not null && fileCount > 0)
            ? new ProgressBar(progressWriter, fileCount)
            : null;

        Parallel.ForEach(filePaths, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, path =>
        {
            ReadSingleFile(path, cache, ref totalLines, fileLines, fileLineCounts, fileMaxNestingDepths, earlyReturnViolations);

            int current = Interlocked.Increment(ref filesRead);
            bar?.Update(current, "Reading files...");
        });

        bar?.Complete();

        return (fileLines, fileLineCounts, fileMaxNestingDepths, earlyReturnViolations, totalLines);
    }

    private static void ReadSingleFile(string path, ScanCache? cache, ref int totalLines,
        ConcurrentDictionary<string, IndexedLine[]> fileLines,
        ConcurrentDictionary<string, int> fileLineCounts,
        ConcurrentDictionary<string, (int MaxDepth, IReadOnlyList<(int LineNumber, int Depth)> LineDepths)> fileMaxNestingDepths,
        ConcurrentDictionary<string, IReadOnlyList<EarlyReturnViolation>> earlyReturnViolations)
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
            fileMaxNestingDepths[path] = ComputeMaxNestingDepth(indexed);
            earlyReturnViolations[path] = EarlyReturnAnalyzer.Analyze(indexed);
        }
        catch (UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Warning: Access denied to '{path}', skipping.");
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Warning: Could not read '{path}': {ex.Message}");
        }
    }

    private static (int MaxDepth, IReadOnlyList<(int LineNumber, int Depth)> LineDepths) ComputeMaxNestingDepth(IndexedLine[] lines)
    {
        int currentDepth = 0;
        int maxDepth = 0;
        var lineDepths = new List<(int LineNumber, int Depth)>(lines.Length);

        foreach (var line in lines)
        {
            int lineMax = currentDepth;
            foreach (var ch in line.Text)
            {
                if (ch == '{')
                {
                    currentDepth++;
                    if (currentDepth > lineMax)
                        lineMax = currentDepth;
                }
                else if (ch == '}')
                    currentDepth--;
            }

            lineDepths.Add((line.LineNumber, lineMax));
            if (lineMax > maxDepth)
                maxDepth = lineMax;
        }

        return (maxDepth, lineDepths);
    }
}
