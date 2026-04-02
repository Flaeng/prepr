using prepr;

namespace prepr.Tests;

public class FileDuplicationInfoTests
{
    [Fact]
    public void ComputePerFile_NoDuplicates_ReturnsEmpty()
    {
        var result = new ScanResult([], 5, 100, new Dictionary<string, int>());
        var infos = FileDuplicationInfo.ComputePerFile(result, new ReportOptions());

        Assert.Empty(infos);
    }

    [Fact]
    public void ComputePerFile_WithDuplicates_ReturnsSeverity()
    {
        using var tree = new TempFileTree();
        // Create a file with 10 lines, 5 of which are duplicated = 50%
        var pathA = tree.AddFile("A.cs", Enumerable.Range(1, 10).Select(i => $"line{i}").ToArray());
        var pathB = tree.AddFile("B.cs", Enumerable.Range(1, 10).Select(i => $"line{i}").ToArray());

        var block = new DuplicateBlock(
            ["line1", "line2", "line3", "line4", "line5"],
            [
                new FileLocation(pathA, 1, 5),
                new FileLocation(pathB, 1, 5)
            ]);
        var result = new ScanResult([block], 2, 20, new Dictionary<string, int>());
        var infos = FileDuplicationInfo.ComputePerFile(result, new ReportOptions());

        Assert.Equal(2, infos.Count);
        foreach (var info in infos)
        {
            Assert.Equal(1, info.DuplicateBlockCount);
            Assert.Equal(5, info.DuplicatedLineCount);
            Assert.Equal(10, info.TotalLineCount);
            Assert.Equal(50.0, info.DuplicationPercentage, 1);
            Assert.Equal(Severity.High, info.Severity); // 50% >= 50 threshold
        }
    }

    [Fact]
    public void ComputePerFile_MediumSeverity()
    {
        using var tree = new TempFileTree();
        // 20 lines file, 5 duplicated = 25%
        var pathA = tree.AddFile("A.cs", Enumerable.Range(1, 20).Select(i => $"line{i}").ToArray());
        var pathB = tree.AddFile("B.cs", Enumerable.Range(1, 20).Select(i => $"line{i}").ToArray());

        var block = new DuplicateBlock(
            ["line1", "line2", "line3", "line4", "line5"],
            [
                new FileLocation(pathA, 1, 5),
                new FileLocation(pathB, 1, 5)
            ]);
        var result = new ScanResult([block], 2, 40, new Dictionary<string, int>());
        var infos = FileDuplicationInfo.ComputePerFile(result, new ReportOptions());

        Assert.All(infos, info => Assert.Equal(Severity.Medium, info.Severity));
    }

    [Fact]
    public void ComputePerFile_LowSeverity()
    {
        using var tree = new TempFileTree();
        // 100 lines file, 5 duplicated = 5%
        var pathA = tree.AddFile("A.cs", Enumerable.Range(1, 100).Select(i => $"line{i}").ToArray());
        var pathB = tree.AddFile("B.cs", Enumerable.Range(1, 100).Select(i => $"line{i}").ToArray());

        var block = new DuplicateBlock(
            ["line1", "line2", "line3", "line4", "line5"],
            [
                new FileLocation(pathA, 1, 5),
                new FileLocation(pathB, 1, 5)
            ]);
        var result = new ScanResult([block], 2, 200, new Dictionary<string, int>());
        var infos = FileDuplicationInfo.ComputePerFile(result, new ReportOptions());

        Assert.All(infos, info => Assert.Equal(Severity.Low, info.Severity));
    }

    [Fact]
    public void ComputePerFile_CustomThresholds()
    {
        using var tree = new TempFileTree();
        // 10 lines file, 5 duplicated = 50%
        var pathA = tree.AddFile("A.cs", Enumerable.Range(1, 10).Select(i => $"line{i}").ToArray());
        var pathB = tree.AddFile("B.cs", Enumerable.Range(1, 10).Select(i => $"line{i}").ToArray());

        var block = new DuplicateBlock(
            ["line1", "line2", "line3", "line4", "line5"],
            [
                new FileLocation(pathA, 1, 5),
                new FileLocation(pathB, 1, 5)
            ]);
        var result = new ScanResult([block], 2, 20, new Dictionary<string, int>());

        // With custom high threshold of 60%, 50% should be medium
        var options = new ReportOptions(HighSeverityThreshold: 60, MediumSeverityThreshold: 40);
        var infos = FileDuplicationInfo.ComputePerFile(result, options);

        Assert.All(infos, info => Assert.Equal(Severity.Medium, info.Severity));
    }

    [Fact]
    public void ComputePerFile_SortedByDuplicationPercentageDescending()
    {
        using var tree = new TempFileTree();
        var pathA = tree.AddFile("A.cs", Enumerable.Range(1, 10).Select(i => $"line{i}").ToArray());
        var pathB = tree.AddFile("B.cs", Enumerable.Range(1, 100).Select(i => $"line{i}").ToArray());

        var block = new DuplicateBlock(
            ["line1", "line2", "line3", "line4", "line5"],
            [
                new FileLocation(pathA, 1, 5),
                new FileLocation(pathB, 1, 5)
            ]);
        var result = new ScanResult([block], 2, 110, new Dictionary<string, int>());
        var infos = FileDuplicationInfo.ComputePerFile(result, new ReportOptions());

        Assert.Equal(2, infos.Count);
        // A has 50%, B has 5% → A should be first
        Assert.True(infos[0].DuplicationPercentage > infos[1].DuplicationPercentage);
    }
}
