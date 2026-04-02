namespace prepr;

public class MarkdownReporter : IReporter
{
    public void Report(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var stats = SummaryStatistics.Compute(result);

        writer.WriteLine("# prepr — Duplicate Block Report");
        writer.WriteLine();
        writer.WriteLine($"- **Files scanned:** {result.TotalFilesScanned}");
        writer.WriteLine($"- **Total lines:** {result.TotalLinesScanned}");
        writer.WriteLine($"- **Duplicate blocks found:** {stats.TotalDuplicateBlocks}");
        writer.WriteLine($"- **Duplicated lines:** {stats.TotalDuplicatedLines}");
        if (stats.MostDuplicatedFile is not null)
        {
            var relMost = Path.GetRelativePath(rootPath, stats.MostDuplicatedFile);
            writer.WriteLine($"- **Most duplicated:** `{relMost}` ({stats.MostDuplicatedFileBlockCount} block(s))");
        }
        writer.WriteLine();

        if (result.Duplicates.Count == 0)
        {
            writer.WriteLine("No duplicate blocks found.");
            return;
        }

        if (options.Verbosity != Verbosity.Quiet)
        {
            writer.WriteLine("---");
            writer.WriteLine();
            writer.WriteLine("## Duplicate Blocks");

            for (int i = 0; i < result.Duplicates.Count; i++)
            {
                var block = result.Duplicates[i];
                writer.WriteLine();
                writer.WriteLine($"### Block #{i + 1} ({block.Lines.Length} lines, {block.Locations.Count} occurrences)");
                writer.WriteLine();
                writer.WriteLine("```");
                foreach (var line in block.Lines)
                    writer.WriteLine(line);
                writer.WriteLine("```");
                writer.WriteLine();
                writer.WriteLine("**Locations:**");
                writer.WriteLine();
                foreach (var loc in block.Locations)
                {
                    var relativePath = Path.GetRelativePath(rootPath, loc.FilePath);
                    writer.WriteLine($"- `{relativePath}` lines {loc.StartLine}–{loc.EndLine}");
                }
            }
        }

        writer.WriteLine();
        writer.WriteLine("---");
        writer.WriteLine();
        writer.WriteLine("## Per-file Summary");
        writer.WriteLine();
        writer.WriteLine("| File | Blocks | Duplicated Lines | Duplication % | Severity |");
        writer.WriteLine("|------|--------|------------------|---------------|----------|");

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
            writer.WriteLine();
            writer.WriteLine("---");
            writer.WriteLine();
            writer.WriteLine("## File Pairs");
            writer.WriteLine();
            writer.WriteLine("| File A | File B | Shared Blocks | Shared Lines |");
            writer.WriteLine("|--------|--------|---------------|--------------|");

            foreach (var pair in pairs)
            {
                var relA = Path.GetRelativePath(rootPath, pair.FileA);
                var relB = Path.GetRelativePath(rootPath, pair.FileB);
                writer.WriteLine($"| `{relA}` | `{relB}` | {pair.SharedBlocks.Count} | {pair.SharedLineCount} |");

                if (options.Verbosity == Verbosity.Detailed)
                {
                    foreach (var block in pair.SharedBlocks)
                    {
                        var locA = block.Locations.FirstOrDefault(l => l.FilePath == pair.FileA);
                        var locB = block.Locations.FirstOrDefault(l => l.FilePath == pair.FileB);
                        writer.WriteLine($"| | ↳ Block ({block.Lines.Length} lines): `{relA}`:{locA?.StartLine}-{locA?.EndLine} ↔ `{relB}`:{locB?.StartLine}-{locB?.EndLine} | | |");
                    }
                }
            }
        }

        writer.WriteLine();
        writer.WriteLine($"**Total: {stats.TotalDuplicateBlocks} duplicate block(s) across {result.TotalFilesScanned} files**");

        // Files exceeding line limit
        if (options.LineLimitRule is not null)
        {
            var overLimit = OverLimitFileInfo.Compute(result, options.LineLimitRule, rootPath);
            if (overLimit.Count > 0)
            {
                writer.WriteLine();
                writer.WriteLine("---");
                writer.WriteLine();
                writer.WriteLine("## Files Exceeding Line Limit");
                writer.WriteLine();
                writer.WriteLine("| File | Lines | Limit |");
                writer.WriteLine("|------|-------|-------|");
                foreach (var v in overLimit)
                {
                    var relativePath = Path.GetRelativePath(rootPath, v.FilePath);
                    writer.WriteLine($"| `{relativePath}` | {v.LineCount} | {v.Limit} |");
                }
            }
        }
    }
}
