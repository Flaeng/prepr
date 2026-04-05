namespace Prepr;

internal static class BlockConsolidator
{
    internal static List<DuplicateBlock> ConsolidateBlocks(List<DuplicateBlock> blocks,
        IReadOnlyDictionary<string, IndexedLine[]> fileLines,
        TextWriter? progressWriter = null)
    {
        if (blocks.Count <= 1)
            return blocks;

        // Group blocks by their set of file paths (sorted), so we only merge blocks
        // that appear in exactly the same set of files
        var groups = blocks
            .GroupBy(b => string.Join("\0", b.Locations.Select(l => l.FilePath).OrderBy(f => f)))
            .ToList();

        var result = new List<DuplicateBlock>();

        int groupIndex = 0;
        int groupTotal = groups.Count;

        ProgressBar? bar = (progressWriter is not null && groupTotal > 0)
            ? new ProgressBar(progressWriter, groupTotal)
            : null;

        foreach (var group in groups)
        {
            var groupBlocks = group.ToList();
            if (groupBlocks.Count <= 1)
            {
                result.AddRange(groupBlocks);
            }
            else
            {
                while (TryMergeAnyPair(groupBlocks, fileLines)) ;
                result.AddRange(groupBlocks);
            }

            groupIndex++;
            bar?.Update(groupIndex, "Consolidating blocks...");
        }

        bar?.Complete();

        // Remove any block that is a strict subset of another block
        return RemoveSubsetBlocks(result);
    }

    private static bool TryMergeAnyPair(List<DuplicateBlock> groupBlocks,
        IReadOnlyDictionary<string, IndexedLine[]> fileLines)
    {
        for (int i = 0; i < groupBlocks.Count; i++)
        {
            for (int j = i + 1; j < groupBlocks.Count; j++)
            {
                var mergedBlock = TryMergeBlocks(groupBlocks[i], groupBlocks[j], fileLines);
                if (mergedBlock is null)
                    continue;

                groupBlocks[i] = mergedBlock;
                groupBlocks.RemoveAt(j);
                return true;
            }
        }
        return false;
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

    internal static List<DuplicateBlock> DeduplicateBlocks(List<DuplicateBlock> blocks)
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
}
