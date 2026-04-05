namespace Prepr.Reporters;

public class PromptReporter : IReporter
{
    public string FileExtension => ".prompt.md";
    public void Report(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var stats = SummaryStatistics.Compute(result);

        writer.WriteLine($"""
            # Code Quality Roadmap

            The following issues were detected in the codebase. They are organized by severity so you can prioritize the most impactful fixes first.

            **Scan summary:** {result.TotalFilesScanned} files scanned, {result.TotalLinesScanned} total lines, {stats.TotalDuplicateBlocks} duplicate block(s) found, {stats.TotalDuplicatedLines} duplicated line(s).
            """);

        // Collect all issues with their severity
        var issues = CollectIssues(result, rootPath, options);

        if (issues.Count == 0)
        {
            writer.WriteLine("No issues found — no action needed.");
            var techDebtScoreEmpty = TechDebtScore.Compute(result, options, rootPath);
            var vsEmpty = ViolationScore.Compute(result, options, rootPath);
            writer.WriteLine($"""

                ---

                ## Tech Debt Score

                **Score:** {techDebtScoreEmpty.Score:F1}/100 \u2014 **Grade: {techDebtScoreEmpty.Grade}**

                ## Violation Score

                **Score:** {vsEmpty.RawScore} ({vsEmpty.NormalizedScore:F1}/1K lines) \u2014 **Grade: {vsEmpty.Grade}**
                """);
            return;
        }

        int taskId = 1;
        var phases = new (Severity Severity, string PhaseName, string PhaseDescription)[]
        {
            (Severity.High, "Phase 1: Critical Issues", "These are high-severity issues that should be addressed first. They have the largest impact on code quality."),
            (Severity.Medium, "Phase 2: Important Issues", "These are medium-severity issues. Address these after resolving all critical issues."),
            (Severity.Low, "Phase 3: Minor Issues", "These are low-severity issues. Address these last as part of ongoing code hygiene.")
        };

        foreach (var (severity, phaseName, phaseDescription) in phases)
        {
            var group = issues.Where(i => i.Severity == severity).ToList();
            if (group.Count == 0) continue;

            writer.WriteLine($"""
                ---

                ## {phaseName} ({group.Count} issue(s))

                {phaseDescription}
                """);

            foreach (var issue in group)
            {
                writer.WriteLine();
                writer.WriteLine($"### TASK-{taskId:D3}: {issue.Title}");
                writer.WriteLine();
                writer.WriteLine(issue.Description);
                if (issue.CodeBlock is not null)
                {
                    writer.WriteLine();
                    writer.WriteLine("```");
                    writer.WriteLine(issue.CodeBlock);
                    writer.WriteLine("```");
                }
                writer.WriteLine();
                writer.WriteLine($"**Action:** {issue.Action}");
                taskId++;
            }

            writer.WriteLine();
        }

        // Tech Debt Score
        var techDebtScore = TechDebtScore.Compute(result, options, rootPath);
        var vs = ViolationScore.Compute(result, options, rootPath);
        writer.WriteLine($"""
            ---

            ## Tech Debt Score

            **Score:** {techDebtScore.Score:F1}/100 \u2014 **Grade: {techDebtScore.Grade}**

            This score reflects the overall technical debt density of the codebase, normalized by codebase size. A larger codebase with the same number of issues scores lower than a smaller one.

            ## Violation Score

            **Score:** {vs.RawScore} ({vs.NormalizedScore:F1}/1K lines) \u2014 **Grade: {vs.Grade}**

            This score counts each violation with flat penalty points. It is normalized per 1,000 lines for grading.
            """);
    }

    private record RoadmapIssue(Severity Severity, string Title, string Description, string? CodeBlock, string Action);

    private static List<RoadmapIssue> CollectIssues(ScanResult result, string rootPath, ReportOptions options)
    {
        var issues = new List<RoadmapIssue>();

        // Duplicate blocks — severity comes from per-file duplication info
        var fileInfos = DuplicationFileInfo.ComputePerFile(result, options);
        var fileSeverity = fileInfos.ToDictionary(f => f.FilePath, f => f.Severity);

        for (int i = 0; i < result.Duplicates.Count; i++)
        {
            var block = result.Duplicates[i];
            // Use the highest severity among the files involved
            var blockSeverity = block.Locations
                .Select(loc => fileSeverity.GetValueOrDefault(loc.FilePath, Severity.Low))
                .Max();

            var locations = string.Join("\n", block.Locations.Select(loc =>
            {
                var rel = Path.GetRelativePath(rootPath, loc.FilePath);
                return $"- `{rel}` lines {loc.StartLine}–{loc.EndLine}";
            }));

            issues.Add(new RoadmapIssue(
                blockSeverity,
                $"Duplicate #{i + 1} — {block.Lines.Length} lines in {block.Locations.Count} locations",
                $"This block of {block.Lines.Length} lines appears in {block.Locations.Count} locations:\n\n{locations}",
                string.Join("\n", block.Lines),
                "Refactor to remove this duplication. Keep the code DRY by extracting into a shared location that all consuming files can reference."
            ));
        }

        // Files exceeding line limit
        var overLimit = OverLimitFileInfo.Compute(result, options, rootPath);
        foreach (var v in overLimit)
        {
            var rel = Path.GetRelativePath(rootPath, v.FilePath);
            issues.Add(new RoadmapIssue(
                v.Severity,
                $"Line limit exceeded — `{rel}`",
                $"`{rel}` has {v.LineCount} lines (limit: {v.Limit}).",
                null,
                "Split this file into smaller, more focused files."
            ));
        }

        // Files exceeding indentation limit
        var overIndented = OverIndentedFileInfo.Compute(result, options, rootPath);
        foreach (var v in overIndented)
        {
            var rel = Path.GetRelativePath(rootPath, v.FilePath);
            issues.Add(new RoadmapIssue(
                v.Severity,
                $"Excessive nesting — `{rel}`",
                $"`{rel}` has nesting depth {v.MaxDepth} at {v.RangesDisplay} (limit: {v.Limit}).",
                null,
                "Refactor to reduce nesting depth. Extract methods, use early returns, or simplify conditionals."
            ));
        }

        // Early return violations
        var earlyReturnViolations = EarlyReturnFileInfo.Compute(result, options);
        foreach (var file in earlyReturnViolations)
        {
            var rel = Path.GetRelativePath(rootPath, file.FilePath);
            var violationLines = string.Join("\n", file.Violations.Select(v => $"- Line {v.LineNumber}: {v.Description}"));
            issues.Add(new RoadmapIssue(
                file.Severity,
                $"Early return opportunities — `{rel}`",
                $"`{rel}` has {file.Violations.Count} else block(s) that could be replaced with guard clauses:\n\n{violationLines}",
                null,
                "Invert the condition and return early to reduce nesting."
            ));
        }

        // Comment density violations
        var commentDensityViolations = CommentDensityFileInfo.Compute(result, options, rootPath);
        foreach (var v in commentDensityViolations)
        {
            var rel = Path.GetRelativePath(rootPath, v.FilePath);
            var direction = v.IsBelowMin ? "under-documented" : "over-commented";
            issues.Add(new RoadmapIssue(
                v.Severity,
                $"Comment density — `{rel}` ({direction})",
                $"`{rel}` has a comment density of {v.DensityPercent:F1}% (limit: {v.LimitPercent:F1}%).",
                null,
                v.GetPrompt(rel) ?? ""
            ));
        }

        // Magic number violations
        var magicNumberViolations = MagicNumberFileInfo.Compute(result, options, rootPath);
        foreach (var file in magicNumberViolations)
        {
            var rel = Path.GetRelativePath(rootPath, file.FilePath);
            var violationLines = string.Join("\n", file.Violations.Select(v => $"- Line {v.LineNumber}: {v.Value}"));
            issues.Add(new RoadmapIssue(
                file.Severity,
                $"Magic numbers — `{rel}`",
                $"`{rel}` has {file.Violations.Count} magic number(s) (limit: {file.Limit}):\n\n{violationLines}",
                null,
                file.GetPrompt(rel)
            ));
        }

        // Magic string violations
        var magicStringViolations = MagicStringFileInfo.Compute(result, options, rootPath);
        foreach (var file in magicStringViolations)
        {
            var rel = Path.GetRelativePath(rootPath, file.FilePath);
            var violationLines = string.Join("\n", file.Violations.Select(v => $"- Line {v.LineNumber}: \"{v.Value}\""));
            issues.Add(new RoadmapIssue(
                file.Severity,
                $"Magic strings — `{rel}`",
                $"`{rel}` has {file.Violations.Count} repeated magic string(s) (limit: {file.Limit}):\n\n{violationLines}",
                null,
                file.GetPrompt(rel)
            ));
        }

        // Folder file count violations
        var overCrowdedFolders = OverCrowdedFolderInfo.Compute(result, options, rootPath);
        foreach (var v in overCrowdedFolders)
        {
            var rel = Path.GetRelativePath(rootPath, v.FolderPath);
            issues.Add(new RoadmapIssue(
                v.Severity,
                $"Overcrowded folder — `{rel}`",
                $"`{rel}` contains {v.FileCount} files (limit: {v.Limit}).",
                null,
                v.GetPrompt(rel)
            ));
        }

        return issues;
    }
}
