namespace Prepr.Models;

public record MagicStringFileInfo(string FilePath, IReadOnlyList<MagicStringViolation> Violations, int Limit, Severity Severity)
{
    public static List<MagicStringFileInfo> Compute(ScanResult result, ReportOptions options, string rootPath)
    {
        var violations = new List<MagicStringFileInfo>();

        if (options.MagicStringRule is null)
            return violations;

        var minRepeat = options.MinMagicStringRepeat;

        foreach (var (filePath, fileViolations) in result.MagicStringViolations)
        {
            var limit = options.MagicStringRule.GetLimit(filePath, rootPath);
            if (limit is null)
                continue;

            // Group by string value; only flag strings that appear at least minRepeat times
            var repeatedStrings = fileViolations
                .GroupBy(v => v.Value)
                .Where(g => g.Count() >= minRepeat)
                .SelectMany(g => g)
                .ToList();

            if (repeatedStrings.Count > 0)
            {
                int distinctRepeated = fileViolations.GroupBy(v => v.Value).Count(g => g.Count() >= minRepeat);
                var severity = distinctRepeated >= 10 ? Severity.High
                             : distinctRepeated >= 5 ? Severity.Medium
                             : Severity.Low;
                violations.Add(new MagicStringFileInfo(filePath, repeatedStrings, limit.Value, severity));
            }
        }

        return violations.OrderByDescending(v => v.Violations.Count).ThenBy(v => v.FilePath).ToList();
    }

    internal string GetPrompt(string relativePath)
    {
        var distinctValues = Violations.Select(v => v.Value).Distinct().Take(5);
        var examples = string.Join(", ", distinctValues.Select(v => $"\"{v}\""));
        return $"Extract repeated magic strings in `{relativePath}` into named constants. The file has {Violations.Count} magic string occurrence(s) exceeding the repeat limit of {Limit}. Examples: {examples}. Replace hardcoded string literals with descriptive constant names to improve maintainability.";
    }
}
