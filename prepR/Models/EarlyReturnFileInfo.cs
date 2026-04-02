namespace Prepr.Models;

public record EarlyReturnFileInfo(string FilePath, IReadOnlyList<EarlyReturnViolation> Violations)
{
    public static List<EarlyReturnFileInfo> Compute(ScanResult result, ReportOptions options)
    {
        if (!options.EarlyReturn)
            return [];

        var violations = new List<EarlyReturnFileInfo>();

        foreach (var (filePath, fileViolations) in result.EarlyReturnViolations)
        {
            if (fileViolations.Count > 0)
                violations.Add(new EarlyReturnFileInfo(filePath, fileViolations));
        }

        return violations.OrderByDescending(v => v.Violations.Count).ThenBy(v => v.FilePath).ToList();
    }
}
