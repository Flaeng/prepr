namespace PrepR;

public record FileDuplicationInfo(
    string FilePath,
    int DuplicateBlockCount,
    int DuplicatedLineCount,
    int TotalLineCount,
    double DuplicationPercentage,
    Severity Severity)
{
    public static List<FileDuplicationInfo> ComputePerFile(ScanResult result, ReportOptions options)
    {
        if (result.Duplicates.Count == 0)
            return [];

        var perFile = result.Duplicates
            .SelectMany(b => b.Locations.Select(loc => new { loc.FilePath, loc.StartLine, loc.EndLine, Block = b }))
            .GroupBy(x => x.FilePath);

        var infos = new List<FileDuplicationInfo>();
        foreach (var group in perFile)
        {
            int blockCount = group.Select(x => x.Block).Distinct().Count();
            int duplicatedLines = group.Sum(x => x.EndLine - x.StartLine + 1);
            int totalLines = CountFileLines(group.Key);
            double pct = totalLines > 0 ? (double)duplicatedLines / totalLines * 100 : 0;
            var severity = pct >= options.HighSeverityThreshold ? Severity.High
                         : pct >= options.MediumSeverityThreshold ? Severity.Medium
                         : Severity.Low;

            infos.Add(new FileDuplicationInfo(group.Key, blockCount, duplicatedLines, totalLines, pct, severity));
        }

        return infos.OrderByDescending(f => f.DuplicationPercentage).ThenBy(f => f.FilePath).ToList();
    }

    private static int CountFileLines(string filePath)
    {
        try { return File.ReadAllLines(filePath).Length; }
        catch { return 0; }
    }
}
