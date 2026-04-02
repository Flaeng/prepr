namespace Prepr.Models;

public record OverIndentedFileInfo(string FilePath, int MaxDepth, int LineNumber, int Limit)
{
    public static List<OverIndentedFileInfo> Compute(ScanResult result, ReportOptions options, string rootPath)
    {
        if (options.IndentationRule is null)
            return [];

        var violations = new List<OverIndentedFileInfo>();

        foreach (var (filePath, (maxDepth, lineNumber)) in result.FileMaxNestingDepths)
        {
            var limit = options.IndentationRule.GetLimit(filePath, rootPath);
            if (limit is not null && maxDepth > limit.Value)
                violations.Add(new OverIndentedFileInfo(filePath, maxDepth, lineNumber, limit.Value));
        }

        return violations.OrderByDescending(v => v.MaxDepth).ThenBy(v => v.FilePath).ToList();
    }
}
