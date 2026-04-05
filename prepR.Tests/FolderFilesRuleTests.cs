namespace Prepr.Tests;

public class MaxFolderFilesRuleTests
{
    [Fact]
    public void GetLimit_NoRules_ReturnsNull()
    {
        var rule = new MaxFolderFilesRule(null, null);

        Assert.False(rule.HasRules);
        Assert.Null(rule.GetLimit("/root/src", "/root"));
    }

    [Fact]
    public void GetLimit_GlobalDefault_AppliesToAllFolders()
    {
        var rule = new MaxFolderFilesRule(null, 15);

        Assert.True(rule.HasRules);
        Assert.Equal(15, rule.GetLimit("/root/src", "/root"));
        Assert.Equal(15, rule.GetLimit("/root/other/deep", "/root"));
    }

    [Fact]
    public void GetLimit_ConfigStar_ActsAsGlobalDefault()
    {
        var rules = new Dictionary<string, int> { { "*", 20 } };
        var rule = new MaxFolderFilesRule(rules, null);

        Assert.True(rule.HasRules);
        Assert.Equal(20, rule.GetLimit("/root/src", "/root"));
    }

    [Fact]
    public void GetLimit_CliOverridesConfigStar()
    {
        var rules = new Dictionary<string, int> { { "*", 20 } };
        var rule = new MaxFolderFilesRule(rules, 10);

        Assert.Equal(10, rule.GetLimit("/root/src", "/root"));
    }

    [Fact]
    public void GetLimit_PathSpecificRule_MatchesPrefix()
    {
        var rules = new Dictionary<string, int> { { "src/Models", 30 } };
        var rule = new MaxFolderFilesRule(rules, null);

        Assert.Equal(30, rule.GetLimit("/root/src/Models/Sub", "/root"));
        Assert.Null(rule.GetLimit("/root/src/Other", "/root"));
    }

    [Fact]
    public void GetLimit_LongestPrefixWins()
    {
        var rules = new Dictionary<string, int>
        {
            { "src", 20 },
            { "src/Models", 30 }
        };
        var rule = new MaxFolderFilesRule(rules, null);

        Assert.Equal(30, rule.GetLimit("/root/src/Models/Sub", "/root"));
        Assert.Equal(20, rule.GetLimit("/root/src/Other", "/root"));
    }
}

public class OverCrowdedFolderInfoTests
{
    private static ScanResult CreateResult(Dictionary<string, int>? folderFileCounts = null)
    {
        return new ScanResult(
            [], 10, 1000,
            new Dictionary<string, int>(),
            new Dictionary<string, NestingDepthInfo>(),
            new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>(),
            new Dictionary<string, int>(),
            new Dictionary<string, IReadOnlyList<MagicNumberViolation>>(),
            new Dictionary<string, IReadOnlyList<MagicStringViolation>>(),
            folderFileCounts ?? new Dictionary<string, int>());
    }

    [Fact]
    public void Compute_NoRule_ReturnsEmpty()
    {
        var result = CreateResult(new Dictionary<string, int> { { "/root/src", 20 } });
        var options = new ReportOptions();

        var violations = OverCrowdedFolderInfo.Compute(result, options, "/root");

        Assert.Empty(violations);
    }

    [Fact]
    public void Compute_NoViolations_ReturnsEmpty()
    {
        var result = CreateResult(new Dictionary<string, int>
        {
            { "/root/src", 10 },
            { "/root/tests", 5 }
        });
        var rule = new MaxFolderFilesRule(null, 15);
        var options = new ReportOptions(MaxFolderFilesRule: rule);

        var violations = OverCrowdedFolderInfo.Compute(result, options, "/root");

        Assert.Empty(violations);
    }

    [Fact]
    public void Compute_FolderAtExactLimit_NotReported()
    {
        var result = CreateResult(new Dictionary<string, int> { { "/root/src", 15 } });
        var rule = new MaxFolderFilesRule(null, 15);
        var options = new ReportOptions(MaxFolderFilesRule: rule);

        var violations = OverCrowdedFolderInfo.Compute(result, options, "/root");

        Assert.Empty(violations);
    }

    [Fact]
    public void Compute_FolderOverLimit_Reported()
    {
        var result = CreateResult(new Dictionary<string, int>
        {
            { "/root/src", 20 },
            { "/root/tests", 5 }
        });
        var rule = new MaxFolderFilesRule(null, 15);
        var options = new ReportOptions(MaxFolderFilesRule: rule);

        var violations = OverCrowdedFolderInfo.Compute(result, options, "/root");

        Assert.Single(violations);
        Assert.Equal("/root/src", violations[0].FolderPath);
        Assert.Equal(20, violations[0].FileCount);
        Assert.Equal(15, violations[0].Limit);
    }

    [Fact]
    public void Compute_SortedByFileCountDescending()
    {
        var result = CreateResult(new Dictionary<string, int>
        {
            { "/root/a", 18 },
            { "/root/b", 25 },
            { "/root/c", 20 }
        });
        var rule = new MaxFolderFilesRule(null, 15);
        var options = new ReportOptions(MaxFolderFilesRule: rule);

        var violations = OverCrowdedFolderInfo.Compute(result, options, "/root");

        Assert.Equal(3, violations.Count);
        Assert.Equal("/root/b", violations[0].FolderPath);
        Assert.Equal("/root/c", violations[1].FolderPath);
        Assert.Equal("/root/a", violations[2].FolderPath);
    }

    [Fact]
    public void Compute_SeverityLow_WhenRatioBelow4Over3()
    {
        // 16/15 = 1.067, below 4/3 = 1.333 → Low
        var result = CreateResult(new Dictionary<string, int> { { "/root/src", 16 } });
        var rule = new MaxFolderFilesRule(null, 15);
        var options = new ReportOptions(MaxFolderFilesRule: rule);

        var violations = OverCrowdedFolderInfo.Compute(result, options, "/root");

        Assert.Single(violations);
        Assert.Equal(Severity.Low, violations[0].Severity);
    }

    [Fact]
    public void Compute_SeverityMedium_WhenRatioAt4Over3()
    {
        // 20/15 = 1.333, exactly 4/3 → Medium
        var result = CreateResult(new Dictionary<string, int> { { "/root/src", 20 } });
        var rule = new MaxFolderFilesRule(null, 15);
        var options = new ReportOptions(MaxFolderFilesRule: rule);

        var violations = OverCrowdedFolderInfo.Compute(result, options, "/root");

        Assert.Single(violations);
        Assert.Equal(Severity.Medium, violations[0].Severity);
    }

    [Fact]
    public void Compute_SeverityMedium_WhenRatioBetween4Over3And5Over3()
    {
        // 22/15 = 1.467, between 4/3 and 5/3 → Medium
        var result = CreateResult(new Dictionary<string, int> { { "/root/src", 22 } });
        var rule = new MaxFolderFilesRule(null, 15);
        var options = new ReportOptions(MaxFolderFilesRule: rule);

        var violations = OverCrowdedFolderInfo.Compute(result, options, "/root");

        Assert.Single(violations);
        Assert.Equal(Severity.Medium, violations[0].Severity);
    }

    [Fact]
    public void Compute_SeverityHigh_WhenRatioAt5Over3()
    {
        // 25/15 = 1.667, exactly 5/3 → High
        var result = CreateResult(new Dictionary<string, int> { { "/root/src", 25 } });
        var rule = new MaxFolderFilesRule(null, 15);
        var options = new ReportOptions(MaxFolderFilesRule: rule);

        var violations = OverCrowdedFolderInfo.Compute(result, options, "/root");

        Assert.Single(violations);
        Assert.Equal(Severity.High, violations[0].Severity);
    }

    [Fact]
    public void Compute_SeverityHigh_WhenRatioAbove5Over3()
    {
        // 30/15 = 2.0, well above 5/3 → High
        var result = CreateResult(new Dictionary<string, int> { { "/root/src", 30 } });
        var rule = new MaxFolderFilesRule(null, 15);
        var options = new ReportOptions(MaxFolderFilesRule: rule);

        var violations = OverCrowdedFolderInfo.Compute(result, options, "/root");

        Assert.Single(violations);
        Assert.Equal(Severity.High, violations[0].Severity);
    }

    [Fact]
    public void GetPrompt_ReturnsActionableMessage()
    {
        var info = new OverCrowdedFolderInfo("/root/src/Models", 30, 15, Severity.High);

        var prompt = info.GetPrompt("src/Models");

        Assert.Contains("src/Models", prompt);
        Assert.Contains("30", prompt);
        Assert.Contains("15", prompt);
    }
}
