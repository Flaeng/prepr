namespace Prepr.Models;

public record OverIndentedFileInfo(string FilePath, int MaxDepth, IReadOnlyList<(int StartLine, int EndLine)> OverLimitRanges, int Limit, Severity Severity)
{
    public static List<OverIndentedFileInfo> Compute(ScanResult result, ReportOptions options, string rootPath)
    {
        if (options.IndentationRule is null)
            return [];

        var violations = new List<OverIndentedFileInfo>();

        foreach (var (filePath, (maxDepth, lineDepths)) in result.FileMaxNestingDepths)
        {
            var limit = options.IndentationRule.GetLimit(filePath, rootPath);
            if (limit is not null && maxDepth > limit.Value)
            {
                var ranges = ComputeOverLimitRanges(lineDepths, limit.Value);
                int overage = maxDepth - limit.Value;
                var severity = overage >= 3 ? Severity.High
                             : overage >= 2 ? Severity.Medium
                             : Severity.Low;
                violations.Add(new OverIndentedFileInfo(filePath, maxDepth, ranges, limit.Value, severity));
            }
        }

        return violations.OrderByDescending(v => v.MaxDepth).ThenBy(v => v.FilePath).ToList();
    }

    private static List<(int StartLine, int EndLine)> ComputeOverLimitRanges(IReadOnlyList<(int LineNumber, int Depth)> lineDepths, int limit)
    {
        var ranges = new List<(int StartLine, int EndLine)>();
        int rangeStart = -1;
        int rangeEnd = -1;

        foreach (var (lineNumber, depth) in lineDepths)
        {
            if (depth > limit)
            {
                if (rangeStart == -1)
                {
                    rangeStart = lineNumber;
                    rangeEnd = lineNumber;
                }
                else
                {
                    rangeEnd = lineNumber;
                }
            }
            else if (rangeStart != -1)
            {
                ranges.Add((rangeStart, rangeEnd));
                rangeStart = -1;
            }
        }

        if (rangeStart != -1)
            ranges.Add((rangeStart, rangeEnd));

        return ranges;
    }

    public string RangesDisplay =>
        string.Join(", ", OverLimitRanges.Select(r => r.StartLine == r.EndLine ? $"{r.StartLine}" : $"{r.StartLine}-{r.EndLine}"));

    internal string? GetPrompt(string relativePath)
    {
        var rangeDesc = string.Join(", ", OverLimitRanges.Select(r => r.StartLine == r.EndLine ? $"line {r.StartLine}" : $"lines {r.StartLine}-{r.EndLine}"));
        return $"Refactor the file `{relativePath}` to reduce nesting depth. The maximum nesting depth is {MaxDepth} (at {rangeDesc}), but the limit is {Limit}. Reduce the nesting to at most {Limit} levels using techniques like early returns, guard clauses, extracting methods, or inverting conditions.";
    }
}
