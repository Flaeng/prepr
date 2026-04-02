namespace Prepr.Models;

public record MagicNumberFileInfo(string FilePath, IReadOnlyList<MagicNumberViolation> Violations, int Limit, Severity Severity)
{
    public static List<MagicNumberFileInfo> Compute(ScanResult result, ReportOptions options, string rootPath)
    {
        var violations = new List<MagicNumberFileInfo>();

        if (options.MagicNumberRule is null)
            return violations;

        foreach (var (filePath, fileViolations) in result.MagicNumberViolations)
        {
            var limit = options.MagicNumberRule.GetLimit(filePath, rootPath);
            if (limit is null)
                continue;

            if (fileViolations.Count > limit.Value)
            {
                int overage = fileViolations.Count - limit.Value;
                var severity = overage >= limit.Value + 10 ? Severity.High
                             : overage >= limit.Value + 5 ? Severity.Medium
                             : Severity.Low;
                violations.Add(new MagicNumberFileInfo(filePath, fileViolations, limit.Value, severity));
            }
        }

        return violations.OrderByDescending(v => v.Violations.Count).ThenBy(v => v.FilePath).ToList();
    }

    internal string GetPrompt(string relativePath) =>
        $"Extract magic numbers in `{relativePath}` into named constants. The file has {Violations.Count} magic number(s) (limit: {Limit}). Replace hardcoded numeric literals with descriptive constant names to improve readability and maintainability.";
}
