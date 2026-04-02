namespace Prepr;

public static class DuplicateDetector
{
    public static List<DuplicateBlock> Detect(IReadOnlyDictionary<string, IndexedLine[]> fileLines,
        int minConsecutiveLines,
        TextWriter? progressWriter = null)
    {
        // Build windows: for each file, extract all sliding windows of MinConsecutiveLines
        // Key: hash of window content -> list of (filePath, windowIndex in that file's indexed lines)
        var windowMap = new Dictionary<long, List<WindowLocation>>();

        int hashFileIndex = 0;
        int hashFileTotal = fileLines.Count;

        ProgressBar? bar = (progressWriter is not null && hashFileTotal > 0)
            ? new ProgressBar(progressWriter, hashFileTotal)
            : null;

        foreach (var (path, lines) in fileLines)
        {
            if (lines.Length < minConsecutiveLines)
            {
                hashFileIndex++;
                bar?.Update(hashFileIndex, "Detecting duplicates...");
                continue;
            }

            for (int i = 0; i <= lines.Length - minConsecutiveLines; i++)
            {
                long hash = ComputeWindowHash(lines, i, minConsecutiveLines);
                if (!windowMap.TryGetValue(hash, out var list))
                {
                    list = [];
                    windowMap[hash] = list;
                }
                list.Add(new WindowLocation(path, lines, i));
            }

            hashFileIndex++;
            bar?.Update(hashFileIndex, "Detecting duplicates...");
        }

        bar?.Complete();

        // Group windows that are exact matches, then merge overlapping windows into larger blocks
        var duplicateBlocks = new List<DuplicateBlock>();

        foreach (var (_, windows) in windowMap)
        {
            if (windows.Count < 2)
                continue;

            // Sub-group by exact content match (handle hash collisions)
            var groups = GroupByExactContent(windows, minConsecutiveLines);

            foreach (var group in groups)
            {
                if (group.Count < 2)
                    continue;

                // Merge overlapping/adjacent windows in each file into maximal blocks
                var merged = MergeWindows(group, minConsecutiveLines);

                // Only keep if still 2+ distinct locations after merging
                if (merged.Locations.Count < 2)
                    continue;

                duplicateBlocks.Add(merged);
            }
        }

        // Deduplicate blocks that share the same set of locations (can happen from overlapping window groups)
        var deduped = BlockConsolidator.DeduplicateBlocks(duplicateBlocks);

        // Merge overlapping/adjacent blocks across different hash groups into maximal blocks,
        // then remove any block that is a subset of a larger block
        var consolidated = BlockConsolidator.ConsolidateBlocks(deduped, fileLines);

        return consolidated;
    }

    private static long ComputeWindowHash(IndexedLine[] lines, int start, int count)
    {
        long hash = 17;
        for (int i = start; i < start + count; i++)
        {
            hash = hash * 31 + lines[i].Text.GetHashCode(StringComparison.Ordinal);
        }
        return hash;
    }

    private static List<List<WindowLocation>> GroupByExactContent(List<WindowLocation> windows, int minConsecutiveLines)
    {
        var groups = new List<List<WindowLocation>>();

        foreach (var window in windows)
        {
            var match = groups.FirstOrDefault(g => WindowsMatch(g[0], window, minConsecutiveLines));
            if (match is not null)
            {
                match.Add(window);
            }
            else
            {
                groups.Add([window]);
            }
        }

        return groups;
    }

    private static bool WindowsMatch(WindowLocation a, WindowLocation b, int minConsecutiveLines)
    {
        for (int i = 0; i < minConsecutiveLines; i++)
        {
            if (a.Lines[a.StartIndex + i].Text != b.Lines[b.StartIndex + i].Text)
                return false;
        }
        return true;
    }

    private static DuplicateBlock MergeWindows(List<WindowLocation> windows, int minConsecutiveLines)
    {
        // Group windows by file path, then merge overlapping/adjacent windows
        var byFile = windows.GroupBy(w => w.FilePath);
        var locations = new List<FileLocation>();
        string[]? blockLines = null;

        foreach (var fileGroup in byFile)
        {
            var sorted = fileGroup.OrderBy(w => w.StartIndex).ToList();
            int mergedStart = sorted[0].StartIndex;
            int mergedEnd = sorted[0].StartIndex + minConsecutiveLines - 1;

            for (int i = 1; i < sorted.Count; i++)
            {
                int wStart = sorted[i].StartIndex;
                int wEnd = wStart + minConsecutiveLines - 1;

                if (wStart <= mergedEnd + 1)
                {
                    mergedEnd = Math.Max(mergedEnd, wEnd);
                }
                else
                {
                    AddLocation(sorted[0].Lines, fileGroup.Key, mergedStart, mergedEnd, locations, ref blockLines);
                    mergedStart = wStart;
                    mergedEnd = wEnd;
                }
            }
            AddLocation(sorted[0].Lines, fileGroup.Key, mergedStart, mergedEnd, locations, ref blockLines);
        }

        return new DuplicateBlock(blockLines!, locations);
    }

    private static void AddLocation(IndexedLine[] lines, string filePath, int startIdx, int endIdx,
        List<FileLocation> locations, ref string[]? blockLines)
    {
        int startLine = lines[startIdx].LineNumber;
        int endLine = lines[endIdx].LineNumber;
        locations.Add(new FileLocation(filePath, startLine, endLine));

        blockLines ??= Enumerable.Range(startIdx, endIdx - startIdx + 1)
            .Select(i => lines[i].Text)
            .ToArray();
    }

    private sealed record WindowLocation(string FilePath, IndexedLine[] Lines, int StartIndex);
}
