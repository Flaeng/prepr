using PrepR;

namespace prepR.Tests;

public class LineLimitRuleTests
{
    [Fact]
    public void GetLimit_NoRules_ReturnsNull()
    {
        var rule = new LineLimitRule(null, null);

        Assert.False(rule.HasRules);
        Assert.Null(rule.GetLimit("/root/src/file.cs", "/root"));
    }

    [Fact]
    public void GetLimit_GlobalDefault_AppliesToAllFiles()
    {
        var rule = new LineLimitRule(null, 100);

        Assert.True(rule.HasRules);
        Assert.Equal(100, rule.GetLimit("/root/src/file.cs", "/root"));
        Assert.Equal(100, rule.GetLimit("/root/other/deep/file.cs", "/root"));
    }

    [Fact]
    public void GetLimit_ConfigStar_ActsAsGlobalDefault()
    {
        var rules = new Dictionary<string, int> { { "*", 200 } };
        var rule = new LineLimitRule(rules, null);

        Assert.True(rule.HasRules);
        Assert.Equal(200, rule.GetLimit("/root/src/file.cs", "/root"));
    }

    [Fact]
    public void GetLimit_CliOverridesConfigStar()
    {
        var rules = new Dictionary<string, int> { { "*", 200 } };
        var rule = new LineLimitRule(rules, 150);

        Assert.Equal(150, rule.GetLimit("/root/src/file.cs", "/root"));
    }

    [Fact]
    public void GetLimit_PathSpecificRule_MatchesPrefix()
    {
        var rules = new Dictionary<string, int> { { "src/FolderA", 50 } };
        var rule = new LineLimitRule(rules, null);

        Assert.Equal(50, rule.GetLimit("/root/src/FolderA/file.cs", "/root"));
        Assert.Null(rule.GetLimit("/root/src/FolderB/file.cs", "/root"));
    }

    [Fact]
    public void GetLimit_LongestPrefixWins()
    {
        var rules = new Dictionary<string, int>
        {
            { "src/FolderA", 50 },
            { "src/FolderA/FolderB", 70 }
        };
        var rule = new LineLimitRule(rules, null);

        // File in FolderB matches the more specific rule
        Assert.Equal(70, rule.GetLimit("/root/src/FolderA/FolderB/file.cs", "/root"));
        // File directly in FolderA matches the less specific rule
        Assert.Equal(50, rule.GetLimit("/root/src/FolderA/other.cs", "/root"));
    }

    [Fact]
    public void GetLimit_FallsBackToGlobalWhenNoPathMatch()
    {
        var rules = new Dictionary<string, int>
        {
            { "*", 200 },
            { "src/FolderA", 50 }
        };
        var rule = new LineLimitRule(rules, null);

        Assert.Equal(50, rule.GetLimit("/root/src/FolderA/file.cs", "/root"));
        Assert.Equal(200, rule.GetLimit("/root/src/Other/file.cs", "/root"));
    }

    [Fact]
    public void GetLimit_BackslashPathsNormalized()
    {
        var rules = new Dictionary<string, int> { { "src/FolderA", 50 } };
        var rule = new LineLimitRule(rules, null);

        // On Windows, relative path may use backslashes
        Assert.Equal(50, rule.GetLimit("/root/src/FolderA/file.cs", "/root"));
    }

    [Fact]
    public void GetLimit_DoesNotMatchPartialDirectoryName()
    {
        var rules = new Dictionary<string, int> { { "src/Fold", 50 } };
        var rule = new LineLimitRule(rules, null);

        // "src/FolderA/file.cs" starts with "src/Fold" but "src/Fold" is not a directory prefix
        // It should NOT match because "src/FolderA/file.cs" does not start with "src/Fold/"
        Assert.Null(rule.GetLimit("/root/src/FolderA/file.cs", "/root"));
    }
}

public class OverLimitFileInfoTests
{
    [Fact]
    public void Compute_NoViolations_ReturnsEmpty()
    {
        var lineCounts = new Dictionary<string, int>
        {
            { "/root/a.cs", 50 },
            { "/root/b.cs", 30 }
        };
        var result = new ScanResult([], 2, 80, lineCounts);
        var rule = new LineLimitRule(null, 100);

        var violations = OverLimitFileInfo.Compute(result, rule, "/root");

        Assert.Empty(violations);
    }

    [Fact]
    public void Compute_ReportsFilesOverLimit()
    {
        var lineCounts = new Dictionary<string, int>
        {
            { "/root/a.cs", 150 },
            { "/root/b.cs", 30 },
            { "/root/c.cs", 200 }
        };
        var result = new ScanResult([], 3, 380, lineCounts);
        var rule = new LineLimitRule(null, 100);

        var violations = OverLimitFileInfo.Compute(result, rule, "/root");

        Assert.Equal(2, violations.Count);
        // Sorted by line count descending
        Assert.Equal("/root/c.cs", violations[0].FilePath);
        Assert.Equal(200, violations[0].LineCount);
        Assert.Equal(100, violations[0].Limit);
        Assert.Equal("/root/a.cs", violations[1].FilePath);
    }

    [Fact]
    public void Compute_PathSpecificRules_ApplyCorrectLimits()
    {
        var lineCounts = new Dictionary<string, int>
        {
            { "/root/src/FolderA/file.cs", 60 },
            { "/root/src/FolderA/FolderB/file.cs", 60 }
        };
        var result = new ScanResult([], 2, 120, lineCounts);
        var rules = new Dictionary<string, int>
        {
            { "src/FolderA", 50 },
            { "src/FolderA/FolderB", 70 }
        };
        var rule = new LineLimitRule(rules, null);

        var violations = OverLimitFileInfo.Compute(result, rule, "/root");

        // FolderA/file.cs (60 lines, limit 50) → violation
        // FolderA/FolderB/file.cs (60 lines, limit 70) → NOT a violation
        Assert.Single(violations);
        Assert.Equal("/root/src/FolderA/file.cs", violations[0].FilePath);
        Assert.Equal(60, violations[0].LineCount);
        Assert.Equal(50, violations[0].Limit);
    }

    [Fact]
    public void Compute_FileAtExactLimit_NotReported()
    {
        var lineCounts = new Dictionary<string, int> { { "/root/a.cs", 100 } };
        var result = new ScanResult([], 1, 100, lineCounts);
        var rule = new LineLimitRule(null, 100);

        var violations = OverLimitFileInfo.Compute(result, rule, "/root");

        Assert.Empty(violations);
    }
}

public class DuplicateDetectorFileLineCountsTests : IDisposable
{
    private readonly TempFileTree _tree = new();

    public void Dispose() => _tree.Dispose();

    [Fact]
    public void Detect_PopulatesFileLineCounts()
    {
        _tree.AddFile("a.cs", ["line1", "line2", "line3"]);
        _tree.AddFile("b.cs", ["line1", "line2"]);

        var files = new List<string>
        {
            Path.Combine(_tree.RootPath, "a.cs"),
            Path.Combine(_tree.RootPath, "b.cs")
        };

        var result = RuleChecker.Run(files);

        Assert.Equal(2, result.FileLineCounts.Count);
        Assert.Equal(3, result.FileLineCounts[files[0]]);
        Assert.Equal(2, result.FileLineCounts[files[1]]);
    }
}
