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

    internal string? GetPrompt(string relativePath)
    {
        return $"Refactor the file `{relativePath}` to reduce its line count. The file currently has {LineCount} lines, but the limit is {Limit}. Split it into smaller, more focused files or extract logic into separate classes or methods to bring it under {Limit} lines. Do not simply make the class partial to spread it across files — each new file must represent a distinct logical responsibility.";
    }
}
