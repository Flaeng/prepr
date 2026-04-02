namespace Prepr.Tests;

public class IndentationRuleTests
{
    [Fact]
    public void GetLimit_NoRules_ReturnsNull()
    {
        var rule = new IndentationRule(null, null);

        Assert.False(rule.HasRules);
        Assert.Null(rule.GetLimit("/root/src/file.cs", "/root"));
    }

    [Fact]
    public void GetLimit_GlobalDefault_AppliesToAllFiles()
    {
        var rule = new IndentationRule(null, 5);

        Assert.True(rule.HasRules);
        Assert.Equal(5, rule.GetLimit("/root/src/file.cs", "/root"));
        Assert.Equal(5, rule.GetLimit("/root/other/deep/file.cs", "/root"));
    }

    [Fact]
    public void GetLimit_ConfigStar_ActsAsGlobalDefault()
    {
        var rules = new Dictionary<string, int> { { "*", 4 } };
        var rule = new IndentationRule(rules, null);

        Assert.True(rule.HasRules);
        Assert.Equal(4, rule.GetLimit("/root/src/file.cs", "/root"));
    }

    [Fact]
    public void GetLimit_CliOverridesConfigStar()
    {
        var rules = new Dictionary<string, int> { { "*", 4 } };
        var rule = new IndentationRule(rules, 3);

        Assert.Equal(3, rule.GetLimit("/root/src/file.cs", "/root"));
    }

    [Fact]
    public void GetLimit_PathSpecificRule_MatchesPrefix()
    {
        var rules = new Dictionary<string, int> { { "src/FolderA", 3 } };
        var rule = new IndentationRule(rules, null);

        Assert.Equal(3, rule.GetLimit("/root/src/FolderA/file.cs", "/root"));
        Assert.Null(rule.GetLimit("/root/src/FolderB/file.cs", "/root"));
    }

    [Fact]
    public void GetLimit_LongestPrefixWins()
    {
        var rules = new Dictionary<string, int>
        {
            { "src/FolderA", 3 },
            { "src/FolderA/FolderB", 5 }
        };
        var rule = new IndentationRule(rules, null);

        Assert.Equal(5, rule.GetLimit("/root/src/FolderA/FolderB/file.cs", "/root"));
        Assert.Equal(3, rule.GetLimit("/root/src/FolderA/other.cs", "/root"));
    }

    [Fact]
    public void GetLimit_FallsBackToGlobalWhenNoPathMatch()
    {
        var rules = new Dictionary<string, int>
        {
            { "*", 4 },
            { "src/FolderA", 3 }
        };
        var rule = new IndentationRule(rules, null);

        Assert.Equal(3, rule.GetLimit("/root/src/FolderA/file.cs", "/root"));
        Assert.Equal(4, rule.GetLimit("/root/src/Other/file.cs", "/root"));
    }

    [Fact]
    public void GetLimit_BackslashPathsNormalized()
    {
        var rules = new Dictionary<string, int> { { "src/FolderA", 3 } };
        var rule = new IndentationRule(rules, null);

        Assert.Equal(3, rule.GetLimit("/root/src/FolderA/file.cs", "/root"));
    }

    [Fact]
    public void GetLimit_DoesNotMatchPartialDirectoryName()
    {
        var rules = new Dictionary<string, int> { { "src/Fold", 3 } };
        var rule = new IndentationRule(rules, null);

        Assert.Null(rule.GetLimit("/root/src/FolderA/file.cs", "/root"));
    }
}

public class OverIndentedFileInfoTests
{
    [Fact]
    public void Compute_NoViolations_ReturnsEmpty()
    {
        var nestingDepths = new Dictionary<string, (int, int)>
        {
            { "/root/a.cs", (2, 10) },
            { "/root/b.cs", (3, 15) }
        };
        var result = new ScanResult([], 2, 80, new Dictionary<string, int>(), nestingDepths, new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>());
        var rule = new IndentationRule(null, 5);
        var options = new ReportOptions(IndentationRule: rule);

        var violations = OverIndentedFileInfo.Compute(result, options, "/root");

        Assert.Empty(violations);
    }

    [Fact]
    public void Compute_ReportsFilesOverLimit()
    {
        var nestingDepths = new Dictionary<string, (int, int)>
        {
            { "/root/a.cs", (6, 20) },
            { "/root/b.cs", (3, 10) },
            { "/root/c.cs", (8, 42) }
        };
        var result = new ScanResult([], 3, 380, new Dictionary<string, int>(), nestingDepths, new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>());
        var rule = new IndentationRule(null, 5);
        var options = new ReportOptions(IndentationRule: rule);

        var violations = OverIndentedFileInfo.Compute(result, options, "/root");

        Assert.Equal(2, violations.Count);
        // Sorted by max depth descending
        Assert.Equal("/root/c.cs", violations[0].FilePath);
        Assert.Equal(8, violations[0].MaxDepth);
        Assert.Equal(42, violations[0].LineNumber);
        Assert.Equal(5, violations[0].Limit);
        Assert.Equal("/root/a.cs", violations[1].FilePath);
        Assert.Equal(20, violations[1].LineNumber);
    }

    [Fact]
    public void Compute_PathSpecificRules_ApplyCorrectLimits()
    {
        var nestingDepths = new Dictionary<string, (int, int)>
        {
            { "/root/src/FolderA/file.cs", (4, 30) },
            { "/root/src/FolderA/FolderB/file.cs", (4, 25) }
        };
        var result = new ScanResult([], 2, 120, new Dictionary<string, int>(), nestingDepths, new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>());
        var rules = new Dictionary<string, int>
        {
            { "src/FolderA", 3 },
            { "src/FolderA/FolderB", 5 }
        };
        var rule = new IndentationRule(rules, null);
        var options = new ReportOptions(IndentationRule: rule);

        var violations = OverIndentedFileInfo.Compute(result, options, "/root");

        // FolderA/file.cs (depth 4, limit 3) → violation
        // FolderA/FolderB/file.cs (depth 4, limit 5) → NOT a violation
        Assert.Single(violations);
        Assert.Equal("/root/src/FolderA/file.cs", violations[0].FilePath);
        Assert.Equal(4, violations[0].MaxDepth);
        Assert.Equal(3, violations[0].Limit);
    }

    [Fact]
    public void Compute_FileAtExactLimit_NotReported()
    {
        var nestingDepths = new Dictionary<string, (int, int)> { { "/root/a.cs", (5, 10) } };
        var result = new ScanResult([], 1, 100, new Dictionary<string, int>(), nestingDepths, new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>());
        var rule = new IndentationRule(null, 5);
        var options = new ReportOptions(IndentationRule: rule);

        var violations = OverIndentedFileInfo.Compute(result, options, "/root");

        Assert.Empty(violations);
    }
}
