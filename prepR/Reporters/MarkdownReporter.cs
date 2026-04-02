namespace Prepr.Reporters;

public class MarkdownReporter : IReporter
{
    public string FileExtension => ".md";

    public void Report(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var stats = SummaryStatistics.Compute(result);

        writer.WriteLine($"""
            # prepr report

            - **Files scanned:** {result.TotalFilesScanned}
            - **Total lines:** {result.TotalLinesScanned}
            - **Duplicate blocks found:** {stats.TotalDuplicateBlocks}
            - **Duplicated lines:** {stats.TotalDuplicatedLines}

            """);

        if (result.Duplicates.Count == 0)
        {
            writer.WriteLine("No duplicate blocks found.");
            return;
        }

        writer.Write("""
            ---

            ## Duplicate Blocks
            """);

        for (int i = 0; i < result.Duplicates.Count; i++)
        {
            var block = result.Duplicates[i];
            writer.WriteLine($"""

                ### Block #{i + 1} ({block.Lines.Length} lines, {block.Locations.Count} occurrences)

                ```
                """);

            foreach (var line in block.Lines)
            {
                writer.WriteLine(line);
            }

            writer.Write("""
                ```

                **Locations:**

                """);

            foreach (var loc in block.Locations)
            {
                var relativePath = Path.GetRelativePath(rootPath, loc.FilePath);
                writer.WriteLine($"- `{relativePath}` lines {loc.StartLine}–{loc.EndLine}");
            }
        }

        writer.Write("""

            ---

            ## Per-file Summary

            | File | Blocks | Duplicated Lines | Duplication % | Severity |
            |------|--------|------------------|---------------|----------|
            """);

        var fileInfos = FileDuplicationInfo.ComputePerFile(result, options);
        foreach (var info in fileInfos)
        {
            var relativePath = Path.GetRelativePath(rootPath, info.FilePath);
            writer.WriteLine($"| `{relativePath}` | {info.DuplicateBlockCount} | {info.DuplicatedLineCount} | {info.DuplicationPercentage:F1}% | {info.Severity} |");
        }

        // File pairs
        var pairs = FilePairGroup.ComputeFilePairs(result);
        if (pairs.Count > 0)
        {
            WritePairs(rootPath, writer, options, pairs);
        }

        writer.WriteLine($"**Total: {stats.TotalDuplicateBlocks} duplicate block(s) across {result.TotalFilesScanned} files**");

        // Files exceeding line limit
        WriteLineLimitRule(result, rootPath, writer, options);

        // Files exceeding indentation limit
        WriteIndentationRule(result, rootPath, writer, options);

        // Early return violations
        WriteEarlyReturnRule(result, rootPath, writer, options);
    }

    private static void WriteLineLimitRule(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var overLimit = OverLimitFileInfo.Compute(result, options, rootPath);
        if (overLimit.Count <= 0)
        {
            return;
        }

        writer.Write("""

            ---

            ## Files Exceeding Line Limit

            | File | Lines | Limit |
            |------|-------|-------|
            """);

        foreach (var v in overLimit)
        {
            var relativePath = Path.GetRelativePath(rootPath, v.FilePath);
            writer.WriteLine($"| `{relativePath}` | {v.LineCount} | {v.Limit} |");
        }
    }

    private static void WriteIndentationRule(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var overIndented = OverIndentedFileInfo.Compute(result, options, rootPath);
        if (overIndented.Count <= 0)
        {
            return;
        }

        writer.Write("""

            ---

            ## Files Exceeding Indentation Limit

            | File | Max Depth | Line | Limit |
            |------|-----------|------|-------|
            """);

        foreach (var v in overIndented)
        {
            var relativePath = Path.GetRelativePath(rootPath, v.FilePath);
            writer.WriteLine($"| `{relativePath}` | {v.MaxDepth} | {v.LineNumber} | {v.Limit} |");
        }
    }

    private static void WriteEarlyReturnRule(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var violations = EarlyReturnFileInfo.Compute(result, options);
        if (violations.Count <= 0)
        {
            return;
        }

        writer.Write("""

            ---

            ## Early Return Opportunities

            | File | Line | Description |
            |------|------|-------------|
            """);

        foreach (var file in violations)
        {
            var relativePath = Path.GetRelativePath(rootPath, file.FilePath);
            foreach (var v in file.Violations)
            {
                writer.WriteLine($"| `{relativePath}` | {v.LineNumber} | {v.Description} |");
            }
        }
    }

    private static void WritePairs(string rootPath, TextWriter writer, ReportOptions options, List<FilePairGroup> pairs)
    {
        writer.Write("""

            ---

            ## File Pairs

            | File A | File B | Shared Blocks | Shared Lines |
            |--------|--------|---------------|--------------|
            """);

        foreach (var pair in pairs)
        {
            var relA = Path.GetRelativePath(rootPath, pair.FileA);
            var relB = Path.GetRelativePath(rootPath, pair.FileB);
            writer.WriteLine($"| `{relA}` | `{relB}` | {pair.SharedBlocks.Count} | {pair.SharedLineCount} |");

            foreach (var block in pair.SharedBlocks)
            {
                var locA = block.Locations.FirstOrDefault(l => l.FilePath == pair.FileA);
                var locB = block.Locations.FirstOrDefault(l => l.FilePath == pair.FileB);
                writer.WriteLine($"| | ↳ Block ({block.Lines.Length} lines): `{relA}`:{locA?.StartLine}-{locA?.EndLine} ↔ `{relB}`:{locB?.StartLine}-{locB?.EndLine} | | |");
            }
        }
    }
}
