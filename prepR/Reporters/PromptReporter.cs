namespace Prepr.Reporters;

public class PromptReporter : IReporter
{
    public string FileExtension => ".prompt.md";
    public void Report(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var stats = SummaryStatistics.Compute(result);

        writer.WriteLine($"""
            # Duplicate Code Refactoring Instructions
        
            The following duplicate code blocks were detected across multiple files.
            Please refactor each duplicate block to eliminate the duplication.
            Extract shared logic into a common method, base class, or shared utility as appropriate.
        
            **Scan summary:** {result.TotalFilesScanned} files scanned, {result.TotalLinesScanned} total lines, {stats.TotalDuplicateBlocks} duplicate block(s) found, {stats.TotalDuplicatedLines} duplicated line(s).
            """);

        if (result.Duplicates.Count == 0)
        {
            writer.WriteLine("No duplicates found — no action needed.");
            var techDebtScoreEmpty = TechDebtScore.Compute(result, options, rootPath);
            writer.WriteLine($"""

                ---

                ## Tech Debt Score

                **Score:** {techDebtScoreEmpty.Score:F1}/100 — **Grade: {techDebtScoreEmpty.Grade}**
                """);
            return;
        }

        // High-severity files first
        var fileInfos = DuplicationFileInfo.ComputePerFile(result, options);
        var highSeverity = fileInfos.Where(f => f.Severity == Severity.High).ToList();
        if (highSeverity.Count > 0)
        {
            writer.WriteLine("""
                ## Priority Files (High Duplication)
            
                These files have the highest duplication and should be refactored first:
                """);
            foreach (var info in highSeverity)
            {
                var rel = Path.GetRelativePath(rootPath, info.FilePath);
                writer.WriteLine($"- `{rel}` — {info.DuplicationPercentage:F1}% duplicated ({info.DuplicatedLineCount}/{info.TotalLineCount} lines, {info.DuplicateBlockCount} block(s))");
            }
            writer.WriteLine();
        }

        WriteDuplications(result, rootPath, writer);

        // File pair instructions
        var pairs = FilePairGroup.ComputeFilePairs(result);
        if (pairs.Count > 0)
        {
            writer.WriteLine("""
                ---
            
                ## File Pair Analysis
                """);
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
        var overLimit = OverLimitFileInfo.Compute(result, options, rootPath);
        if (overLimit.Count > 0)
        {
            writer.WriteLine("""
                ---
            
                ## Files Exceeding Line Limit
            
                The following files exceed their maximum allowed line count. Consider splitting them into smaller, more focused files:
            
                """);

            foreach (var v in overLimit)
            {
                var rel = Path.GetRelativePath(rootPath, v.FilePath);
                writer.WriteLine($"- [{v.Severity}] `{rel}` \u2014 {v.LineCount} lines (limit: {v.Limit})");
            }

            writer.WriteLine();
        }

        // Files exceeding indentation limit
        var overIndented = OverIndentedFileInfo.Compute(result, options, rootPath);
        if (overIndented.Count > 0)
        {
            writer.WriteLine("""
                ---
            
                ## Files Exceeding Indentation Limit
            
                The following files exceed their maximum allowed nesting depth. Consider refactoring to reduce complexity:
            
                """);

            foreach (var v in overIndented)
            {
                var rel = Path.GetRelativePath(rootPath, v.FilePath);
                writer.WriteLine($"- [{v.Severity}] `{rel}` \u2014 depth {v.MaxDepth} at {v.RangesDisplay} (limit: {v.Limit})");
            }

            writer.WriteLine();
        }

        // Early return violations
        var earlyReturnViolations = EarlyReturnFileInfo.Compute(result, options);
        if (earlyReturnViolations.Count > 0)
        {
            writer.WriteLine("""
                ---
            
                ## Early Return Opportunities
            
                The following files contain else blocks that could be replaced with guard clauses (early returns). Invert the condition and return early to reduce nesting:
            
                """);

            foreach (var file in earlyReturnViolations)
            {
                var rel = Path.GetRelativePath(rootPath, file.FilePath);
                writer.WriteLine($"### [{file.Severity}] `{rel}`");
                writer.WriteLine();
                foreach (var v in file.Violations)
                {
                    writer.WriteLine($"- Line {v.LineNumber}: {v.Description}");
                }
                writer.WriteLine();
            }
        }

        // Tech Debt Score
        var techDebtScore = TechDebtScore.Compute(result, options, rootPath);
        writer.WriteLine($"""
            ---

            ## Tech Debt Score

            **Score:** {techDebtScore.Score:F1}/100 — **Grade: {techDebtScore.Grade}**

            This score reflects the overall technical debt density of the codebase, normalized by codebase size. A larger codebase with the same number of issues scores lower than a smaller one.
            """);
    }

    private static void WriteDuplications(ScanResult result, string rootPath, TextWriter writer)
    {
        for (int i = 0; i < result.Duplicates.Count; i++)
        {
            var block = result.Duplicates[i];
            writer.WriteLine($"""
                ---
            
                ## Duplicate #{i + 1}
            
                This block of {block.Lines.Length} lines appears in {block.Locations.Count} locations:
                """);

            foreach (var loc in block.Locations)
            {
                var relativePath = Path.GetRelativePath(rootPath, loc.FilePath);
                writer.WriteLine($"- `{relativePath}` lines {loc.StartLine}–{loc.EndLine}");
            }

            writer.WriteLine("""
                
                **Duplicated code:**
            
                ```
                """);

            foreach (var line in block.Lines)
                writer.WriteLine(line);

            writer.WriteLine("""
                ```
            
                **Action:** Refactor to remove this duplication. Keep the code DRY by extracting into a shared location that all consuming files can reference.
                """);
        }
    }
}
