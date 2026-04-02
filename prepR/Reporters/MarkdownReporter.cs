namespace Prepr.Reporters;

public class MarkdownReporter : IReporter
{
    public string FileExtension => ".md";

    public void Report(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var score = TechDebtScore.Compute(result, options, rootPath);

        writer.WriteLine($"""
            # prepr report

            | Files scanned | Total lines | Tech Debt Score |
            |---------------|-------------|-----------------|
            | {result.TotalFilesScanned} | {result.TotalLinesScanned} | {score.Score:F1}/100 — Grade: {score.Grade} |

            """);

        WriteDuplicationSection(result, rootPath, writer, options);

        WriteLineLimitRule(result, rootPath, writer, options);

        WriteIndentationRule(result, rootPath, writer, options);

        WriteEarlyReturnRule(result, rootPath, writer, options);

        WriteCommentDensityRule(result, rootPath, writer, options);

        WriteTechDebtScore(result, rootPath, writer, options);
    }

    private static string SeverityCounts(int high, int medium, int low) =>
        $"({high} HIGH, {medium} MEDIUM, {low} LOW)";

    private static void WriteDuplicationSection(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var stats = SummaryStatistics.Compute(result);
        var fileInfos = DuplicationFileInfo.ComputePerFile(result, options);
        var highCount = fileInfos.Count(f => f.Severity == Severity.High);
        var mediumCount = fileInfos.Count(f => f.Severity == Severity.Medium);
        var lowCount = fileInfos.Count(f => f.Severity == Severity.Low);

        writer.Write($"""

            ---

            ## Code Duplication {SeverityCounts(highCount, mediumCount, lowCount)}
            """);

        if (result.Duplicates.Count == 0)
        {
            writer.WriteLine();
            writer.WriteLine("No duplicate blocks found.");
            return;
        }

        writer.WriteLine("""

            ### Per-file Summary

            | File | Blocks | Duplicated Lines | Duplication % | Severity |
            |------|--------|------------------|---------------|----------|
            """);

        foreach (var info in fileInfos)
        {
            var relativePath = Path.GetRelativePath(rootPath, info.FilePath);
            writer.WriteLine($"| `{relativePath}` | {info.DuplicateBlockCount} | {info.DuplicatedLineCount} | {info.DuplicationPercentage:F1}% | {info.Severity} |");
        }

        writer.Write("""

            ### Duplicate Blocks
            """);

        for (int i = 0; i < result.Duplicates.Count; i++)
        {
            var block = result.Duplicates[i];
            writer.WriteLine($"""

                #### Block #{i + 1} ({block.Lines.Length} lines, {block.Locations.Count} occurrences)

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

        var pairs = FilePairGroup.ComputeFilePairs(result);
        if (pairs.Count > 0)
        {
            WritePairs(rootPath, writer, pairs);
        }

        writer.WriteLine();
        writer.WriteLine($"**Total: {stats.TotalDuplicateBlocks} duplicate block(s) across {result.TotalFilesScanned} files**");
    }

    private static void WriteLineLimitRule(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var overLimit = OverLimitFileInfo.Compute(result, options, rootPath);
        var highCount = overLimit.Count(v => v.Severity == Severity.High);
        var mediumCount = overLimit.Count(v => v.Severity == Severity.Medium);
        var lowCount = overLimit.Count(v => v.Severity == Severity.Low);

        writer.Write($"""

            ---

            ## Line Count Overage {SeverityCounts(highCount, mediumCount, lowCount)}
            """);

        if (overLimit.Count == 0)
        {
            writer.WriteLine();
            writer.WriteLine("No violations found.");
            return;
        }

        writer.WriteLine("""

            | File | Lines | Limit | Severity |
            |------|-------|-------|----------|
            """);

        foreach (var v in overLimit)
        {
            var relativePath = Path.GetRelativePath(rootPath, v.FilePath);
            writer.WriteLine($"| `{relativePath}` | {v.LineCount} | {v.Limit} | {v.Severity} |");
        }
    }

    private static void WriteIndentationRule(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var overIndented = OverIndentedFileInfo.Compute(result, options, rootPath);
        var highCount = overIndented.Count(v => v.Severity == Severity.High);
        var mediumCount = overIndented.Count(v => v.Severity == Severity.Medium);
        var lowCount = overIndented.Count(v => v.Severity == Severity.Low);

        writer.Write($"""

            ---

            ## Indentation Overage {SeverityCounts(highCount, mediumCount, lowCount)}
            """);

        if (overIndented.Count == 0)
        {
            writer.WriteLine();
            writer.WriteLine("No violations found.");
            return;
        }

        writer.WriteLine("""

            | File | Max Depth | Lines | Limit | Severity |
            |------|-----------|-------|-------|----------|
            """);

        foreach (var v in overIndented)
        {
            var relativePath = Path.GetRelativePath(rootPath, v.FilePath);
            writer.WriteLine($"| `{relativePath}` | {v.MaxDepth} | {v.RangesDisplay} | {v.Limit} | {v.Severity} |");
        }
    }

    private static void WriteEarlyReturnRule(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var violations = EarlyReturnFileInfo.Compute(result, options);
        var highCount = violations.Count(f => f.Severity == Severity.High);
        var mediumCount = violations.Count(f => f.Severity == Severity.Medium);
        var lowCount = violations.Count(f => f.Severity == Severity.Low);

        writer.Write($"""

            ---

            ## Early Return Opportunities {SeverityCounts(highCount, mediumCount, lowCount)}
            """);

        if (violations.Count == 0)
        {
            writer.WriteLine();
            writer.WriteLine("No violations found.");
            return;
        }

        writer.WriteLine("""

            | File | Line | Description | Severity |
            |------|------|-------------|----------|
            """);

        foreach (var file in violations)
        {
            var relativePath = Path.GetRelativePath(rootPath, file.FilePath);
            foreach (var v in file.Violations)
            {
                writer.WriteLine($"| `{relativePath}` | {v.LineNumber} | {v.Description} | {file.Severity} |");
            }
        }
    }

    private static void WriteCommentDensityRule(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var violations = CommentDensityFileInfo.Compute(result, options, rootPath);
        var highCount = violations.Count(v => v.Severity == Severity.High);
        var mediumCount = violations.Count(v => v.Severity == Severity.Medium);
        var lowCount = violations.Count(v => v.Severity == Severity.Low);

        writer.Write($"""

            ---

            ## Comment Density {SeverityCounts(highCount, mediumCount, lowCount)}
            """);

        if (violations.Count == 0)
        {
            writer.WriteLine();
            writer.WriteLine("No violations found.");
            return;
        }

        writer.WriteLine("""

            | File | Comments | Total Lines | Density % | Limit % | Direction | Severity |
            |------|----------|-------------|-----------|---------|-----------|----------|
            """);

        foreach (var v in violations)
        {
            var relativePath = Path.GetRelativePath(rootPath, v.FilePath);
            var direction = v.IsBelowMin ? "Below min" : "Above max";
            writer.WriteLine($"| `{relativePath}` | {v.CommentLines} | {v.TotalLines} | {v.DensityPercent:F1}% | {v.LimitPercent:F1}% | {direction} | {v.Severity} |");
        }
    }

    private static void WriteTechDebtScore(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var score = TechDebtScore.Compute(result, options, rootPath);
        writer.Write($"""

            ---

            ## Tech Debt Score

            **Score:** {score.Score:F1}/100 — **Grade: {score.Grade}**

            """);
    }

    private static void WritePairs(string rootPath, TextWriter writer, List<FilePairGroup> pairs)
    {
        writer.WriteLine("""

            ### File Pairs

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
