namespace prepr;

public class PromptReporter : IReporter
{
    public void Report(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var stats = SummaryStatistics.Compute(result);

        writer.WriteLine("# Duplicate Code Refactoring Instructions");
        writer.WriteLine();
        writer.WriteLine("The following duplicate code blocks were detected across multiple files.");
        writer.WriteLine("Please refactor each duplicate block to eliminate the duplication.");
        writer.WriteLine("Extract shared logic into a common method, base class, or shared utility as appropriate.");
        writer.WriteLine();
        writer.WriteLine($"**Scan summary:** {result.TotalFilesScanned} files scanned, {result.TotalLinesScanned} total lines, {stats.TotalDuplicateBlocks} duplicate block(s) found, {stats.TotalDuplicatedLines} duplicated line(s).");
        writer.WriteLine();

        if (result.Duplicates.Count == 0)
        {
            writer.WriteLine("No duplicates found — no action needed.");
            return;
        }

        // High-severity files first
        var fileInfos = FileDuplicationInfo.ComputePerFile(result, options);
        var highSeverity = fileInfos.Where(f => f.Severity == Severity.High).ToList();
        if (highSeverity.Count > 0)
        {
            writer.WriteLine("## Priority Files (High Duplication)");
            writer.WriteLine();
            writer.WriteLine("These files have the highest duplication and should be refactored first:");
            writer.WriteLine();
            foreach (var info in highSeverity)
            {
                var rel = Path.GetRelativePath(rootPath, info.FilePath);
                writer.WriteLine($"- `{rel}` — {info.DuplicationPercentage:F1}% duplicated ({info.DuplicatedLineCount}/{info.TotalLineCount} lines, {info.DuplicateBlockCount} block(s))");
            }
            writer.WriteLine();
        }

        for (int i = 0; i < result.Duplicates.Count; i++)
        {
            var block = result.Duplicates[i];
            writer.WriteLine("---");
            writer.WriteLine();
            writer.WriteLine($"## Duplicate #{i + 1}");
            writer.WriteLine();
            writer.WriteLine($"This block of {block.Lines.Length} lines appears in {block.Locations.Count} locations:");
            writer.WriteLine();

            foreach (var loc in block.Locations)
            {
                var relativePath = Path.GetRelativePath(rootPath, loc.FilePath);
                writer.WriteLine($"- `{relativePath}` lines {loc.StartLine}–{loc.EndLine}");
            }

            writer.WriteLine();
            writer.WriteLine("**Duplicated code:**");
            writer.WriteLine();
            writer.WriteLine("```");
            foreach (var line in block.Lines)
                writer.WriteLine(line);
            writer.WriteLine("```");
            writer.WriteLine();
            writer.WriteLine("**Action:** Refactor to remove this duplication. Keep the code DRY by extracting into a shared location that all consuming files can reference.");
            writer.WriteLine();
        }

        // File pair instructions
        var pairs = FilePairGroup.ComputeFilePairs(result);
        if (pairs.Count > 0 && options.Verbosity == Verbosity.Detailed)
        {
            writer.WriteLine("---");
            writer.WriteLine();
            writer.WriteLine("## File Pair Analysis");
            writer.WriteLine();
            foreach (var pair in pairs)
            {
                var relA = Path.GetRelativePath(rootPath, pair.FileA);
                var relB = Path.GetRelativePath(rootPath, pair.FileB);
                writer.WriteLine($"### `{relA}` ↔ `{relB}`");
                writer.WriteLine();
                writer.WriteLine($"These files share {pair.SharedBlocks.Count} duplicate block(s) ({pair.SharedLineCount} lines). Consider extracting shared logic into a common location.");
                writer.WriteLine();
            }
        }

        // Files exceeding line limit
        if (options.LineLimitRule is not null)
        {
            var overLimit = OverLimitFileInfo.Compute(result, options.LineLimitRule, rootPath);
            if (overLimit.Count > 0)
            {
                writer.WriteLine("---");
                writer.WriteLine();
                writer.WriteLine("## Files Exceeding Line Limit");
                writer.WriteLine();
                writer.WriteLine("The following files exceed their maximum allowed line count. Consider splitting them into smaller, more focused files:");
                writer.WriteLine();
                foreach (var v in overLimit)
                {
                    var rel = Path.GetRelativePath(rootPath, v.FilePath);
                    writer.WriteLine($"- `{rel}` \u2014 {v.LineCount} lines (limit: {v.Limit})");
                }
                writer.WriteLine();
            }
        }
    }
}
