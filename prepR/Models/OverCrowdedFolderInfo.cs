namespace Prepr.Models;

public record OverCrowdedFolderInfo(string FolderPath, int FileCount, int Limit, Severity Severity)
{
    public static List<OverCrowdedFolderInfo> Compute(ScanResult result, ReportOptions options, string rootPath)
    {
        if (options.MaxFolderFilesRule is null)
            return [];

        var violations = new List<OverCrowdedFolderInfo>();

        foreach (var (folderPath, fileCount) in result.FolderFileCounts)
        {
            var limit = options.MaxFolderFilesRule.GetLimit(folderPath, rootPath);
            if (limit is not null && fileCount > limit.Value)
            {
                double ratio = (double)fileCount / limit.Value;
                var severity = ratio >= 5.0 / 3.0 ? Severity.High
                             : ratio >= 4.0 / 3.0 ? Severity.Medium
                             : Severity.Low;
                violations.Add(new OverCrowdedFolderInfo(folderPath, fileCount, limit.Value, severity));
            }
        }

        return violations.OrderByDescending(v => v.FileCount).ThenBy(v => v.FolderPath).ToList();
    }

    internal string GetPrompt(string relativePath)
    {
        return $"Reorganize the folder `{relativePath}` — split {FileCount} files into sub-folders to bring it under the limit of {Limit} files per folder.";
    }
}
