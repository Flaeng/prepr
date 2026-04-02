namespace Prepr.Models;

public record OverLimitFileInfo(string FilePath, int LineCount, int Limit)
{
    public static List<OverLimitFileInfo> Compute(ScanResult result, ReportOptions options, string rootPath)
    {
        if (options.LineLimitRule is null)
            return [];

        var violations = new List<OverLimitFileInfo>();

        foreach (var (filePath, lineCount) in result.FileLineCounts)
        {
            var limit = options.LineLimitRule.GetLimit(filePath, rootPath);
            if (limit is not null && lineCount > limit.Value)
                violations.Add(new OverLimitFileInfo(filePath, lineCount, limit.Value));
        }

        return violations.OrderByDescending(v => v.LineCount).ThenBy(v => v.FilePath).ToList();
    }
}
