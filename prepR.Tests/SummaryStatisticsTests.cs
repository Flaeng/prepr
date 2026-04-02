namespace Prepr.Tests;

public class SummaryStatisticsTests
{
    [Fact]
    public void Compute_NoDuplicates_ReturnsZeros()
    {
        var result = new ScanResult([], 5, 100, new Dictionary<string, int>(), new Dictionary<string, (int, int)>(), new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>());
        var stats = SummaryStatistics.Compute(result);

        Assert.Equal(0, stats.TotalDuplicateBlocks);
        Assert.Equal(0, stats.TotalDuplicatedLines);
        Assert.Null(stats.MostDuplicatedFile);
        Assert.Equal(0, stats.MostDuplicatedFileBlockCount);
        Assert.Equal(0, stats.UniqueFilesWithDuplicates);
    }

    [Fact]
    public void Compute_SingleBlock_CorrectStats()
    {
        var block = new DuplicateBlock(
            ["a", "b", "c", "d", "e"],
            [
                new FileLocation("/src/A.cs", 1, 5),
                new FileLocation("/src/B.cs", 10, 14)
            ]);
        var result = new ScanResult([block], 3, 100, new Dictionary<string, int>(), new Dictionary<string, (int, int)>(), new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>());
        var stats = SummaryStatistics.Compute(result);

        Assert.Equal(1, stats.TotalDuplicateBlocks);
        Assert.Equal(10, stats.TotalDuplicatedLines); // 5 lines in A + 5 lines in B
        Assert.Equal(2, stats.UniqueFilesWithDuplicates);
        Assert.Equal(1, stats.MostDuplicatedFileBlockCount);
    }

    [Fact]
    public void Compute_MultipleBlocks_MostDuplicatedFile()
    {
        var block1 = new DuplicateBlock(
            ["a", "b", "c", "d", "e"],
            [
                new FileLocation("/src/A.cs", 1, 5),
                new FileLocation("/src/B.cs", 10, 14)
            ]);
        var block2 = new DuplicateBlock(
            ["f", "g", "h", "i", "j"],
            [
                new FileLocation("/src/A.cs", 20, 24),
                new FileLocation("/src/C.cs", 1, 5)
            ]);
        var result = new ScanResult([block1, block2], 3, 200, new Dictionary<string, int>(), new Dictionary<string, (int, int)>(), new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>());
        var stats = SummaryStatistics.Compute(result);

        Assert.Equal(2, stats.TotalDuplicateBlocks);
        Assert.Equal(20, stats.TotalDuplicatedLines); // A:5+5, B:5, C:5
        Assert.Equal("/src/A.cs", stats.MostDuplicatedFile); // A has 2 blocks
        Assert.Equal(2, stats.MostDuplicatedFileBlockCount);
        Assert.Equal(3, stats.UniqueFilesWithDuplicates);
    }

    [Fact]
    public void Compute_ThreeFilesShareBlock_CountsAllFiles()
    {
        var block = new DuplicateBlock(
            ["a", "b", "c", "d", "e"],
            [
                new FileLocation("/src/A.cs", 1, 5),
                new FileLocation("/src/B.cs", 1, 5),
                new FileLocation("/src/C.cs", 1, 5)
            ]);
        var result = new ScanResult([block], 3, 150, new Dictionary<string, int>(), new Dictionary<string, (int, int)>(), new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>());
        var stats = SummaryStatistics.Compute(result);

        Assert.Equal(1, stats.TotalDuplicateBlocks);
        Assert.Equal(15, stats.TotalDuplicatedLines); // 5 × 3 files
        Assert.Equal(3, stats.UniqueFilesWithDuplicates);
    }
}
