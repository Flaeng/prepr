namespace Prepr.Tests;

public class TechDebtScoreTests
{
    private static ScanResult CreateResult(
        List<DuplicateBlock>? duplicates = null,
        int totalFiles = 10,
        int totalLines = 1000,
        Dictionary<string, int>? fileLineCounts = null,
        Dictionary<string, NestingDepthInfo>? fileMaxNestingDepths = null,
        Dictionary<string, IReadOnlyList<EarlyReturnViolation>>? earlyReturnViolations = null,
        Dictionary<string, int>? fileCommentLineCounts = null)
    {
        return new ScanResult(
            duplicates ?? [],
            totalFiles,
            totalLines,
            fileLineCounts ?? new Dictionary<string, int>(),
            fileMaxNestingDepths ?? new Dictionary<string, NestingDepthInfo>(),
            earlyReturnViolations ?? new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>(),
            fileCommentLineCounts ?? new Dictionary<string, int>());
    }

    [Fact]
    public void Compute_NoViolations_ScoreIsZero()
    {
        var result = CreateResult();
        var score = TechDebtScore.Compute(result, new ReportOptions(), "/root");

        Assert.Equal(0, score.Score);
        Assert.Equal('A', score.Grade);
        Assert.Equal(0, score.DuplicationDensity);
        Assert.Equal(0, score.LineLimitDensity);
        Assert.Equal(0, score.IndentationDensity);
        Assert.Equal(0, score.EarlyReturnDensity);
    }

    [Fact]
    public void Compute_OnlyDuplication_ScoresCorrectly()
    {
        var block = new DuplicateBlock(
            ["a", "b", "c", "d", "e"],
            [new FileLocation("/src/A.cs", 1, 5), new FileLocation("/src/B.cs", 10, 14)]);
        var result = CreateResult(duplicates: [block], totalLines: 100);

        var score = TechDebtScore.Compute(result, new ReportOptions(), "/root");

        // 10 duplicated lines / 100 total = 10% density, weighted at 30% = 3.0
        Assert.Equal(10, score.DuplicationDensity);
        Assert.Equal(3.0, score.Score);
        Assert.Equal('A', score.Grade);
    }

    [Fact]
    public void Compute_LargerCodebase_LowerScore()
    {
        var block = new DuplicateBlock(
            ["a", "b", "c", "d", "e"],
            [new FileLocation("/src/A.cs", 1, 5), new FileLocation("/src/B.cs", 10, 14)]);

        var smallResult = CreateResult(duplicates: [block], totalLines: 100);
        var largeResult = CreateResult(duplicates: [block], totalLines: 10000);

        var smallScore = TechDebtScore.Compute(smallResult, new ReportOptions(), "/root");
        var largeScore = TechDebtScore.Compute(largeResult, new ReportOptions(), "/root");

        Assert.True(smallScore.Score > largeScore.Score,
            $"Small codebase score ({smallScore.Score}) should be higher than large ({largeScore.Score})");
    }

    [Fact]
    public void Compute_OnlyEarlyReturn_ScoresCorrectly()
    {
        var violations = new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>
        {
            ["/src/A.cs"] = [new EarlyReturnViolation(10, "else block")],
            ["/src/B.cs"] = [new EarlyReturnViolation(20, "else block")]
        };
        var result = CreateResult(totalFiles: 10, earlyReturnViolations: violations);

        var options = new ReportOptions(EarlyReturn: true);
        var score = TechDebtScore.Compute(result, options, "/root");

        // 2 files with violations / 10 total files = 20%, weighted at 15% = 3.0
        Assert.Equal(20, score.EarlyReturnDensity);
        Assert.Equal(3.0, score.Score);
    }

    [Fact]
    public void Compute_DisabledRules_ContributeZero()
    {
        var result = CreateResult();
        // No LineLimitRule, no IndentationRule, EarlyReturn disabled
        var options = new ReportOptions(EarlyReturn: false);
        var score = TechDebtScore.Compute(result, options, "/root");

        Assert.Equal(0, score.LineLimitDensity);
        Assert.Equal(0, score.IndentationDensity);
        Assert.Equal(0, score.EarlyReturnDensity);
    }

    [Fact]
    public void Compute_CustomWeights_Applied()
    {
        var block = new DuplicateBlock(
            ["a", "b", "c", "d", "e"],
            [new FileLocation("/src/A.cs", 1, 5), new FileLocation("/src/B.cs", 10, 14)]);
        var result = CreateResult(duplicates: [block], totalLines: 100);

        // All weight on duplication
        var options = new ReportOptions(
            TechDebtWeightDuplication: 100,
            TechDebtWeightLineLimit: 0,
            TechDebtWeightIndentation: 0,
            TechDebtWeightEarlyReturn: 0);
        var score = TechDebtScore.Compute(result, options, "/root");

        // 10% density * 100% weight = 10.0
        Assert.Equal(10.0, score.Score);
    }

    [Theory]
    [InlineData(0, 'A')]
    [InlineData(10, 'A')]
    [InlineData(10.1, 'B')]
    [InlineData(25, 'B')]
    [InlineData(25.1, 'C')]
    [InlineData(50, 'C')]
    [InlineData(50.1, 'D')]
    [InlineData(75, 'D')]
    [InlineData(75.1, 'F')]
    [InlineData(100, 'F')]
    public void GetGrade_ReturnsCorrectGrade(double score, char expectedGrade)
    {
        Assert.Equal(expectedGrade, TechDebtScore.GetGrade(score));
    }

    [Fact]
    public void Compute_ZeroFilesAndLines_ScoreIsZero()
    {
        var result = CreateResult(totalFiles: 0, totalLines: 0);
        var score = TechDebtScore.Compute(result, new ReportOptions(), "/root");

        Assert.Equal(0, score.Score);
        Assert.Equal('A', score.Grade);
    }

    [Fact]
    public void Compute_MixedViolations_AggregatesCorrectly()
    {
        var block = new DuplicateBlock(
            ["a", "b", "c", "d", "e"],
            [new FileLocation("/src/A.cs", 1, 5), new FileLocation("/src/B.cs", 10, 14)]);

        var earlyViolations = new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>
        {
            ["/src/A.cs"] = [new EarlyReturnViolation(10, "else block")]
        };

        var result = CreateResult(
            duplicates: [block],
            totalFiles: 10,
            totalLines: 100,
            earlyReturnViolations: earlyViolations);

        var options = new ReportOptions(EarlyReturn: true);
        var score = TechDebtScore.Compute(result, options, "/root");

        // Duplication: 10/100 = 10% * 0.3 = 3.0
        // Early return: 1/10 = 10% * 0.15 = 1.5
        // Total = 4.5
        Assert.Equal(4.5, score.Score);
        Assert.Equal('A', score.Grade);
    }
}
