namespace PrepR;

public record OverLimitFileInfo(string FilePath, int LineCount, int Limit)
{
    public static List<OverLimitFileInfo> Compute(ScanResult result, LineLimitRule rule, string rootPath)
    {
        var violations = new List<OverLimitFileInfo>();

        foreach (var (filePath, lineCount) in result.FileLineCounts)
        {
            var limit = rule.GetLimit(filePath, rootPath);
            if (limit is not null && lineCount > limit.Value)
                violations.Add(new OverLimitFileInfo(filePath, lineCount, limit.Value));
        }

        return violations.OrderByDescending(v => v.LineCount).ThenBy(v => v.FilePath).ToList();
    }
}
