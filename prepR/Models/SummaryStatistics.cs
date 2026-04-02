namespace Prepr.Models;

public record SummaryStatistics(
    int TotalDuplicateBlocks,
    int TotalDuplicatedLines,
    int UniqueFilesWithDuplicates)
{
    public static SummaryStatistics Compute(ScanResult result)
    {
        if (result.Duplicates.Count == 0)
            return new SummaryStatistics(0, 0, 0);

        var perFile = result.Duplicates
            .SelectMany(b => b.Locations.Select(loc => new { loc.FilePath, loc.StartLine, loc.EndLine, Block = b }))
            .GroupBy(x => x.FilePath);

        var fileStats = perFile.Select(g => new
        {
            FilePath = g.Key,
            BlockCount = g.Select(x => x.Block).Distinct().Count(),
            DuplicatedLines = g.Sum(x => x.EndLine - x.StartLine + 1)
        }).ToList();

        return new SummaryStatistics(
            result.Duplicates.Count,
            fileStats.Sum(f => f.DuplicatedLines),
            fileStats.Count);
    }
}
