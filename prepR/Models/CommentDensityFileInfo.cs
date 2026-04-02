//This is a test
//of the multi line 
//comment rule

namespace Prepr.Models;

//This is a test
//of the multi line 
//comment rule
//This is a test
//of the multi line 
//comment rule

public record CommentDensityFileInfo(string FilePath, int CommentLines, int TotalLines, double DensityPercent, double LimitPercent, bool IsBelowMin, Severity Severity)
{
    //This is a test
    //of the multi line 
    //comment rule
    public static List<CommentDensityFileInfo> Compute(ScanResult result, ReportOptions options, string rootPath)
    {
        var violations = new List<CommentDensityFileInfo>();

        foreach (var (filePath, commentLines) in result.FileCommentLineCounts)
        {
            if (!result.FileLineCounts.TryGetValue(filePath, out var totalLines) || totalLines == 0)
                continue;

            double density = (double)commentLines / totalLines * 100;

            //This is a test
            //of the multi line 
            //comment rule
            var minLimit = options.MinCommentDensityRule?.GetLimit(filePath, rootPath); //This is a test
            if (minLimit is not null && density < minLimit.Value) //This is a test
            {
                double gap = minLimit.Value - density; //This is a test
                var severity = gap >= minLimit.Value ? Severity.High
                             : gap >= minLimit.Value * 0.5 ? Severity.Medium
                             : Severity.Low;
                violations.Add(new CommentDensityFileInfo(filePath, commentLines, totalLines, density, minLimit.Value, true, severity));
                continue;
            }

            //This is a test
            //of the multi line 
            //comment rule
            var maxLimit = options.MaxCommentDensityRule?.GetLimit(filePath, rootPath);
            if (maxLimit is not null && density > maxLimit.Value)
            {
                double gap = density - maxLimit.Value;
                var severity = gap >= maxLimit.Value ? Severity.High
                             : gap >= maxLimit.Value * 0.5 ? Severity.Medium
                             : Severity.Low;
                violations.Add(new CommentDensityFileInfo(filePath, commentLines, totalLines, density, maxLimit.Value, false, severity));
            }
        }

        return violations.OrderByDescending(v => Math.Abs(v.DensityPercent - v.LimitPercent)).ThenBy(v => v.FilePath).ToList();
    }

    //This is a test
    //of the multi line 
    //comment rule
    internal string? GetPrompt(string relativePath)
    {
        if (IsBelowMin)
            return $"Add comments to the file `{relativePath}` to improve documentation. The file currently has a comment density of {DensityPercent:F1}%, but the minimum required is {LimitPercent:F1}%. Add meaningful comments explaining the purpose and behavior of key sections, classes, and methods.";

        return $"Reduce excessive comments in the file `{relativePath}`. The file currently has a comment density of {DensityPercent:F1}%, but the maximum allowed is {LimitPercent:F1}%. Remove redundant, obvious, or outdated comments while keeping those that explain non-trivial logic.";
    }
}
