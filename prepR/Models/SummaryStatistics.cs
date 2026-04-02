namespace prepr;

public record SummaryStatistics(
    int TotalDuplicateBlocks,
    int TotalDuplicatedLines,
    string? MostDuplicatedFile,
    int MostDuplicatedFileBlockCount,
    int UniqueFilesWithDuplicates)
{
    public static SummaryStatistics Compute(ScanResult result)
    {
        if (result.Duplicates.Count == 0)
            return new SummaryStatistics(0, 0, null, 0, 0);

        var perFile = result.Duplicates
            .SelectMany(b => b.Locations.Select(loc => new { loc.FilePath, loc.StartLine, loc.EndLine, Block = b }))
            .GroupBy(x => x.FilePath);

        var fileStats = perFile.Select(g => new
        {
            FilePath = g.Key,
            BlockCount = g.Select(x => x.Block).Distinct().Count(),
            DuplicatedLines = g.Sum(x => x.EndLine - x.StartLine + 1)
        }).ToList();

        var mostDuplicated = fileStats.OrderByDescending(f => f.BlockCount).ThenBy(f => f.FilePath).First();

        return new SummaryStatistics(
            result.Duplicates.Count,
            fileStats.Sum(f => f.DuplicatedLines),
            mostDuplicated.FilePath,
            mostDuplicated.BlockCount,
            fileStats.Count);
    }
}
