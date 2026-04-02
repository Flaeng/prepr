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

    internal string? GetPrompt(string relativePath)
    {
        return $"Refactor the file `{relativePath}` to reduce nesting depth. The maximum nesting depth is {MaxDepth} (found at line {LineNumber}), but the limit is {Limit}. Reduce the nesting to at most {Limit} levels using techniques like early returns, guard clauses, extracting methods, or inverting conditions.";
    }
}
