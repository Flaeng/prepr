namespace Prepr.Models;

public record EarlyReturnFileInfo(string FilePath, IReadOnlyList<EarlyReturnViolation> Violations, Severity Severity)
{
    public static List<EarlyReturnFileInfo> Compute(ScanResult result, ReportOptions options)
    {
        if (!options.EarlyReturn)
            return [];

        var violations = new List<EarlyReturnFileInfo>();

        foreach (var (filePath, fileViolations) in result.EarlyReturnViolations)
        {
            if (fileViolations.Count > 0)
            {
                var severity = fileViolations.Count >= 5 ? Severity.High
                             : fileViolations.Count >= 3 ? Severity.Medium
                             : Severity.Low;
                violations.Add(new EarlyReturnFileInfo(filePath, fileViolations, severity));
            }
        }

        return violations.OrderByDescending(v => v.Violations.Count).ThenBy(v => v.FilePath).ToList();
    }

    internal string GetPrompt(string relativePath)
    {
        var details = string.Join("; ", Violations.Select(v => $"line {v.LineNumber}: {v.Description}"));
        return $"Refactor the file `{relativePath}` to use early returns and guard clauses. Found {Violations.Count} opportunity(ies): {details}";
    }
}
