namespace prepr;

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
        var deduped = DeduplicateBlocks(duplicateBlocks);

        // Merge overlapping/adjacent blocks across different hash groups into maximal blocks,
        // then remove any block that is a subset of a larger block
        var consolidated = ConsolidateBlocks(deduped, fileLines);

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
            bool added = false;
            foreach (var group in groups)
            {
                if (WindowsMatch(group[0], window, minConsecutiveLines))
                {
                    group.Add(window);
                    added = true;
                    break;
                }
            }
            if (!added)
                groups.Add([window]);
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

    private static List<DuplicateBlock> ConsolidateBlocks(List<DuplicateBlock> blocks,
        IReadOnlyDictionary<string, IndexedLine[]> fileLines)
    {
        if (blocks.Count <= 1)
            return blocks;

        // Group blocks by their set of file paths (sorted), so we only merge blocks
        // that appear in exactly the same set of files
        var groups = blocks
            .GroupBy(b => string.Join("\0", b.Locations.Select(l => l.FilePath).OrderBy(f => f)))
            .ToList();

        var result = new List<DuplicateBlock>();

        foreach (var group in groups)
        {
            var groupBlocks = group.ToList();
            if (groupBlocks.Count <= 1)
            {
                result.AddRange(groupBlocks);
                continue;
            }

            // Try to merge blocks that overlap or are adjacent in ALL their file locations.
            // Use a greedy approach: keep merging until no more merges are possible.
            bool merged = true;
            while (merged)
            {
                merged = false;
                for (int i = 0; i < groupBlocks.Count && !merged; i++)
                {
                    for (int j = i + 1; j < groupBlocks.Count && !merged; j++)
                    {
                        var mergedBlock = TryMergeBlocks(groupBlocks[i], groupBlocks[j], fileLines);
                        if (mergedBlock is not null)
                        {
                            groupBlocks[i] = mergedBlock;
                            groupBlocks.RemoveAt(j);
                            merged = true;
                        }
                    }
                }
            }

            result.AddRange(groupBlocks);
        }

        // Remove any block that is a strict subset of another block
        return RemoveSubsetBlocks(result);
    }

    private static DuplicateBlock? TryMergeBlocks(DuplicateBlock a, DuplicateBlock b,
        IReadOnlyDictionary<string, IndexedLine[]> fileLines)
    {
        // Both blocks must have the same files (already guaranteed by grouping)
        var filesA = a.Locations.OrderBy(l => l.FilePath).ToList();
        var filesB = b.Locations.OrderBy(l => l.FilePath).ToList();

        if (filesA.Count != filesB.Count)
            return null;

        // Check that in every file, the two blocks overlap or are adjacent
        var mergedLocations = new List<FileLocation>();
        for (int i = 0; i < filesA.Count; i++)
        {
            if (filesA[i].FilePath != filesB[i].FilePath)
                return null;

            var locA = filesA[i];
            var locB = filesB[i];

            // Check overlap or adjacency (adjacent = end+1 >= start of other)
            if (locA.EndLine + 1 < locB.StartLine && locB.EndLine + 1 < locA.StartLine)
                return null; // gap between them in this file

            int newStart = Math.Min(locA.StartLine, locB.StartLine);
            int newEnd = Math.Max(locA.EndLine, locB.EndLine);
            mergedLocations.Add(new FileLocation(locA.FilePath, newStart, newEnd));
        }

        // Verify the merged ranges actually have identical content across all files
        var firstLoc = mergedLocations[0];
        var firstFile = fileLines[firstLoc.FilePath];
        var firstLines = ExtractLineRange(firstFile, firstLoc.StartLine, firstLoc.EndLine);

        for (int i = 1; i < mergedLocations.Count; i++)
        {
            var loc = mergedLocations[i];
            var otherFile = fileLines[loc.FilePath];
            var otherLines = ExtractLineRange(otherFile, loc.StartLine, loc.EndLine);

            if (firstLines.Length != otherLines.Length)
                return null;

            for (int j = 0; j < firstLines.Length; j++)
            {
                if (firstLines[j] != otherLines[j])
                    return null;
            }
        }

        return new DuplicateBlock(firstLines, mergedLocations);
    }

    private static string[] ExtractLineRange(IndexedLine[] indexedLines, int startLine, int endLine)
    {
        return indexedLines
            .Where(l => l.LineNumber >= startLine && l.LineNumber <= endLine)
            .OrderBy(l => l.LineNumber)
            .Select(l => l.Text)
            .ToArray();
    }

    private static List<DuplicateBlock> RemoveSubsetBlocks(List<DuplicateBlock> blocks)
    {
        var result = new List<DuplicateBlock>();

        for (int i = 0; i < blocks.Count; i++)
        {
            bool isSubset = false;
            for (int j = 0; j < blocks.Count; j++)
            {
                if (i == j) continue;

                if (IsSubsetOf(blocks[i], blocks[j]))
                {
                    isSubset = true;
                    break;
                }
            }
            if (!isSubset)
                result.Add(blocks[i]);
        }

        return result;
    }

    private static bool IsSubsetOf(DuplicateBlock smaller, DuplicateBlock larger)
    {
        // A block is a subset if every location in `smaller` is contained within
        // a corresponding location in `larger` (same file, range fully enclosed)
        if (smaller.Lines.Length >= larger.Lines.Length)
            return false;

        foreach (var sLoc in smaller.Locations)
        {
            bool found = larger.Locations.Any(lLoc =>
                lLoc.FilePath == sLoc.FilePath &&
                lLoc.StartLine <= sLoc.StartLine &&
                lLoc.EndLine >= sLoc.EndLine);

            if (!found)
                return false;
        }

        return true;
    }

    private static List<DuplicateBlock> DeduplicateBlocks(List<DuplicateBlock> blocks)
    {
        var seen = new HashSet<string>();
        var result = new List<DuplicateBlock>();

        foreach (var block in blocks)
        {
            var key = string.Join("\n", block.Locations
                .OrderBy(l => l.FilePath)
                .ThenBy(l => l.StartLine)
                .Select(l => $"{l.FilePath}:{l.StartLine}-{l.EndLine}"));

            if (seen.Add(key))
                result.Add(block);
        }

        return result;
    }

    private record WindowLocation(string FilePath, IndexedLine[] Lines, int StartIndex);
}
