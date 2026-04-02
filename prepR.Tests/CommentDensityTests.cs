namespace Prepr.Tests;

public class CommentDensityTests
{
    [Fact]
    public void CountCommentLines_SingleLineComments()
    {
        var lines = new IndexedLine[]
        {
            new(1, "int x = 1;"),
            new(2, "// this is a comment"),
            new(3, "int y = 2;"),
            new(4, "  // indented comment"),
        };

        var count = RuleChecker.CountCommentLines(lines);

        Assert.Equal(2, count);
    }

    [Fact]
    public void CountCommentLines_BlockComment_SingleLine()
    {
        var lines = new IndexedLine[]
        {
            new(1, "int x = 1;"),
            new(2, "/* single line block comment */"),
            new(3, "int y = 2;"),
        };

        var count = RuleChecker.CountCommentLines(lines);

        Assert.Equal(1, count);
    }

    [Fact]
    public void CountCommentLines_BlockComment_MultiLine()
    {
        var lines = new IndexedLine[]
        {
            new(1, "int x = 1;"),
            new(2, "/* start of block"),
            new(3, "   middle of block"),
            new(4, "   end of block */"),
            new(5, "int y = 2;"),
        };

        var count = RuleChecker.CountCommentLines(lines);

        Assert.Equal(3, count);
    }

    [Fact]
    public void CountCommentLines_NoComments()
    {
        var lines = new IndexedLine[]
        {
            new(1, "int x = 1;"),
            new(2, "int y = 2;"),
        };

        var count = RuleChecker.CountCommentLines(lines);

        Assert.Equal(0, count);
    }

    [Fact]
    public void CountCommentLines_MixedComments()
    {
        var lines = new IndexedLine[]
        {
            new(1, "// single line"),
            new(2, "int x = 1;"),
            new(3, "/* block start"),
            new(4, "   block end */"),
            new(5, "// another single"),
        };

        var count = RuleChecker.CountCommentLines(lines);

        Assert.Equal(4, count);
    }

    [Fact]
    public void Compute_BelowMinDensity_ReportsViolation()
    {
        var commentCounts = new Dictionary<string, int> { ["/root/a.cs"] = 1 };
        var lineCounts = new Dictionary<string, int> { ["/root/a.cs"] = 100 };
        var result = new ScanResult([], 1, 100, lineCounts,
            new Dictionary<string, NestingDepthInfo>(),
            new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>(),
            commentCounts);
        var options = new ReportOptions(MinCommentDensityRule: new MinCommentDensityRule(new Dictionary<string, int> { ["*"] = 10 }, null));

        var violations = CommentDensityFileInfo.Compute(result, options, "/root");

        Assert.Single(violations);
        Assert.True(violations[0].IsBelowMin);
        Assert.Equal(1.0, violations[0].DensityPercent);
        Assert.Equal(10, violations[0].LimitPercent);
    }

    [Fact]
    public void Compute_AboveMaxDensity_ReportsViolation()
    {
        var commentCounts = new Dictionary<string, int> { ["/root/a.cs"] = 80 };
        var lineCounts = new Dictionary<string, int> { ["/root/a.cs"] = 100 };
        var result = new ScanResult([], 1, 100, lineCounts,
            new Dictionary<string, NestingDepthInfo>(),
            new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>(),
            commentCounts);
        var options = new ReportOptions(MaxCommentDensityRule: new MaxCommentDensityRule(new Dictionary<string, int> { ["*"] = 50 }, null));

        var violations = CommentDensityFileInfo.Compute(result, options, "/root");

        Assert.Single(violations);
        Assert.False(violations[0].IsBelowMin);
        Assert.Equal(80.0, violations[0].DensityPercent);
        Assert.Equal(50, violations[0].LimitPercent);
    }

    [Fact]
    public void Compute_WithinLimits_NoViolations()
    {
        var commentCounts = new Dictionary<string, int> { ["/root/a.cs"] = 15 };
        var lineCounts = new Dictionary<string, int> { ["/root/a.cs"] = 100 };
        var result = new ScanResult([], 1, 100, lineCounts,
            new Dictionary<string, NestingDepthInfo>(),
            new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>(),
            commentCounts);
        var options = new ReportOptions(
            MinCommentDensityRule: new MinCommentDensityRule(new Dictionary<string, int> { ["*"] = 10 }, null),
            MaxCommentDensityRule: new MaxCommentDensityRule(new Dictionary<string, int> { ["*"] = 50 }, null));

        var violations = CommentDensityFileInfo.Compute(result, options, "/root");

        Assert.Empty(violations);
    }

    [Fact]
    public void Compute_NoRulesConfigured_NoViolations()
    {
        var commentCounts = new Dictionary<string, int> { ["/root/a.cs"] = 0 };
        var lineCounts = new Dictionary<string, int> { ["/root/a.cs"] = 100 };
        var result = new ScanResult([], 1, 100, lineCounts,
            new Dictionary<string, NestingDepthInfo>(),
            new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>(),
            commentCounts);
        var options = new ReportOptions();

        var violations = CommentDensityFileInfo.Compute(result, options, "/root");

        Assert.Empty(violations);
    }

    [Fact]
    public void Compute_Severity_HighWhenFarFromLimit()
    {
        var commentCounts = new Dictionary<string, int> { ["/root/a.cs"] = 0 };
        var lineCounts = new Dictionary<string, int> { ["/root/a.cs"] = 100 };
        var result = new ScanResult([], 1, 100, lineCounts,
            new Dictionary<string, NestingDepthInfo>(),
            new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>(),
            commentCounts);
        var options = new ReportOptions(MinCommentDensityRule: new MinCommentDensityRule(new Dictionary<string, int> { ["*"] = 10 }, null));

        var violations = CommentDensityFileInfo.Compute(result, options, "/root");

        Assert.Single(violations);
        Assert.Equal(Severity.High, violations[0].Severity);
    }

    [Fact]
    public void RuleChecker_CountsCommentLines_Integration()
    {
        using var tree = new TempFileTree();
        var path = tree.AddFile("test.cs", new[]
        {
            "// comment 1",
            "int x = 1;",
            "/* block start",
            "   block end */",
            "int y = 2;",
        });

        var result = RuleChecker.Run([path]);

        Assert.True(result.FileCommentLineCounts.ContainsKey(path));
        Assert.Equal(3, result.FileCommentLineCounts[path]);
    }

    [Fact]
    public void GetPrompt_BelowMin_SuggestsAddingComments()
    {
        var info = new CommentDensityFileInfo("/root/a.cs", 1, 100, 1.0, 10.0, true, Severity.Low);
        var prompt = info.GetPrompt("a.cs");

        Assert.Contains("Add comments", prompt);
        Assert.Contains("a.cs", prompt);
    }

    [Fact]
    public void GetPrompt_AboveMax_SuggestsReducingComments()
    {
        var info = new CommentDensityFileInfo("/root/a.cs", 80, 100, 80.0, 50.0, false, Severity.Low);
        var prompt = info.GetPrompt("a.cs");

        Assert.Contains("Reduce", prompt);
        Assert.Contains("a.cs", prompt);
    }
}
