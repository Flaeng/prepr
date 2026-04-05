namespace Prepr.Tests;

public class ViolationScoreTests
{
    private static ScanResult CreateResult(
        List<DuplicateBlock>? duplicates = null,
        int totalFiles = 10,
        int totalLines = 1000,
        Dictionary<string, int>? fileLineCounts = null,
        Dictionary<string, NestingDepthInfo>? fileMaxNestingDepths = null,
        Dictionary<string, IReadOnlyList<EarlyReturnViolation>>? earlyReturnViolations = null,
        Dictionary<string, int>? fileCommentLineCounts = null,
        Dictionary<string, IReadOnlyList<MagicNumberViolation>>? magicNumberViolations = null,
        Dictionary<string, IReadOnlyList<MagicStringViolation>>? magicStringViolations = null)
    {
        return new ScanResult(
            duplicates ?? [],
            totalFiles,
            totalLines,
            fileLineCounts ?? new Dictionary<string, int>(),
            fileMaxNestingDepths ?? new Dictionary<string, NestingDepthInfo>(),
            earlyReturnViolations ?? new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>(),
            fileCommentLineCounts ?? new Dictionary<string, int>(),
            magicNumberViolations ?? new Dictionary<string, IReadOnlyList<MagicNumberViolation>>(),
            magicStringViolations ?? new Dictionary<string, IReadOnlyList<MagicStringViolation>>(), new Dictionary<string, int>());
    }

    [Fact]
    public void Compute_NoViolations_ScoreIsZero()
    {
        var result = CreateResult();
        var score = ViolationScore.Compute(result, new ReportOptions(), "/root");

        Assert.Equal(0, score.RawScore);
        Assert.Equal(0, score.NormalizedScore);
        Assert.Equal('A', score.Grade);
        Assert.Equal(0, score.DuplicateBlockCount);
        Assert.Equal(0, score.LineLimitFileCount);
        Assert.Equal(0, score.IndentationFileCount);
        Assert.Equal(0, score.EarlyReturnFileCount);
        Assert.Equal(0, score.CommentDensityFileCount);
        Assert.Equal(0, score.MagicNumberFileCount);
        Assert.Equal(0, score.MagicStringFileCount);
    }

    [Fact]
    public void Compute_OnlyDuplication_CorrectPoints()
    {
        var block1 = new DuplicateBlock(
            ["a", "b", "c", "d", "e"],
            [new FileLocation("/src/A.cs", 1, 5), new FileLocation("/src/B.cs", 10, 14)]);
        var block2 = new DuplicateBlock(
            ["x", "y", "z"],
            [new FileLocation("/src/C.cs", 1, 3), new FileLocation("/src/D.cs", 5, 7)]);
        var result = CreateResult(duplicates: [block1, block2], totalLines: 1000);

        var score = ViolationScore.Compute(result, new ReportOptions(), "/root");

        Assert.Equal(2, score.DuplicateBlockCount);
        Assert.Equal(2 * ViolationScore.PointsPerDuplicateBlock, score.RawScore);
    }

    [Fact]
    public void Compute_OnlyEarlyReturn_CorrectPoints()
    {
        var violations = new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>
        {
            ["/src/A.cs"] = [new EarlyReturnViolation(10, "else block")],
            ["/src/B.cs"] = [new EarlyReturnViolation(20, "else block")]
        };
        var result = CreateResult(totalFiles: 10, earlyReturnViolations: violations);

        var options = new ReportOptions(EarlyReturn: true);
        var score = ViolationScore.Compute(result, options, "/root");

        Assert.Equal(2, score.EarlyReturnFileCount);
        Assert.Equal(2 * ViolationScore.PointsPerEarlyReturnFile, score.RawScore);
    }

    [Fact]
    public void Compute_DisabledRules_ContributeZero()
    {
        var result = CreateResult();
        var options = new ReportOptions(EarlyReturn: false);
        var score = ViolationScore.Compute(result, options, "/root");

        Assert.Equal(0, score.RawScore);
        Assert.Equal(0, score.EarlyReturnFileCount);
    }

    [Fact]
    public void Compute_NormalizationPer1000Lines()
    {
        var block = new DuplicateBlock(
            ["a", "b", "c", "d", "e"],
            [new FileLocation("/src/A.cs", 1, 5), new FileLocation("/src/B.cs", 10, 14)]);
        var result = CreateResult(duplicates: [block], totalLines: 2000);

        var score = ViolationScore.Compute(result, new ReportOptions(), "/root");

        // 1 block * 5 points = 5 raw, 5/2000*1000 = 2.5 normalized
        Assert.Equal(5, score.RawScore);
        Assert.Equal(2.5, score.NormalizedScore);
    }

    [Fact]
    public void Compute_ZeroLines_NormalizedIsZero()
    {
        var result = CreateResult(totalLines: 0);
        var score = ViolationScore.Compute(result, new ReportOptions(), "/root");

        Assert.Equal(0, score.NormalizedScore);
    }

    [Theory]
    [InlineData(0, 'A')]
    [InlineData(50, 'A')]
    [InlineData(50.1, 'B')]
    [InlineData(150, 'B')]
    [InlineData(150.1, 'C')]
    [InlineData(300, 'C')]
    [InlineData(300.1, 'D')]
    [InlineData(500, 'D')]
    [InlineData(500.1, 'F')]
    [InlineData(1000, 'F')]
    public void GetGrade_ReturnsCorrectGrade(double normalizedScore, char expected)
    {
        Assert.Equal(expected, ViolationScore.GetGrade(normalizedScore));
    }

    [Fact]
    public void Compute_MixedViolations_SumsCorrectly()
    {
        var block = new DuplicateBlock(
            ["a", "b", "c", "d", "e"],
            [new FileLocation("/src/A.cs", 1, 5), new FileLocation("/src/B.cs", 10, 14)]);
        var violations = new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>
        {
            ["/src/A.cs"] = [new EarlyReturnViolation(10, "else block")]
        };
        var result = CreateResult(duplicates: [block], totalLines: 1000, earlyReturnViolations: violations);

        var options = new ReportOptions(EarlyReturn: true);
        var score = ViolationScore.Compute(result, options, "/root");

        var expected = 1 * ViolationScore.PointsPerDuplicateBlock
                     + 1 * ViolationScore.PointsPerEarlyReturnFile;
        Assert.Equal(expected, score.RawScore);
    }
}
