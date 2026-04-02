namespace Prepr.Tests;

public class MagicNumberTests
{
    [Fact]
    public void FindMagicNumbers_DetectsIntegerLiterals()
    {
        var lines = new IndexedLine[]
        {
            new(1, "if (count > 42) { }"),
        };

        var violations = RuleChecker.FindMagicNumbers(lines);

        Assert.Single(violations);
        Assert.Equal("42", violations[0].Value);
        Assert.Equal(1, violations[0].LineNumber);
    }

    [Fact]
    public void FindMagicNumbers_DetectsAllNumbers_IncludingZeroAndOne()
    {
        var lines = new IndexedLine[]
        {
            new(1, "int x = 0;"),
            new(2, "int y = 1;"),
            new(3, "int z = -1;"),
        };

        var violations = RuleChecker.FindMagicNumbers(lines);

        Assert.Equal(3, violations.Count);
    }

    [Fact]
    public void FindMagicNumbers_DetectsFloatingPointLiterals()
    {
        var lines = new IndexedLine[]
        {
            new(1, "double pi = 3.14;"),
        };

        var violations = RuleChecker.FindMagicNumbers(lines);

        Assert.Contains(violations, v => v.Value == "3.14");
    }

    [Fact]
    public void FindMagicNumbers_DetectsHexLiterals()
    {
        var lines = new IndexedLine[]
        {
            new(1, "int color = 0xFF00FF;"),
        };

        var violations = RuleChecker.FindMagicNumbers(lines);

        Assert.Single(violations);
        Assert.Equal("0xFF00FF", violations[0].Value);
    }

    [Fact]
    public void FindMagicNumbers_IgnoresSingleLineComments()
    {
        var lines = new IndexedLine[]
        {
            new(1, "// int x = 42;"),
        };

        var violations = RuleChecker.FindMagicNumbers(lines);

        Assert.Empty(violations);
    }

    [Fact]
    public void FindMagicNumbers_IgnoresBlockComments()
    {
        var lines = new IndexedLine[]
        {
            new(1, "/* start block"),
            new(2, "int x = 42;"),
            new(3, "end block */"),
            new(4, "int y = 99;"),
        };

        var violations = RuleChecker.FindMagicNumbers(lines);

        Assert.Single(violations);
        Assert.Equal("99", violations[0].Value);
        Assert.Equal(4, violations[0].LineNumber);
    }

    [Fact]
    public void FindMagicNumbers_DetectsMultipleOnSameLine()
    {
        var lines = new IndexedLine[]
        {
            new(1, "var result = 10 + 20 * 30;"),
        };

        var violations = RuleChecker.FindMagicNumbers(lines);

        Assert.Equal(3, violations.Count);
    }

    [Fact]
    public void FindMagicNumbers_EmptyLines_NoViolations()
    {
        var lines = Array.Empty<IndexedLine>();

        var violations = RuleChecker.FindMagicNumbers(lines);

        Assert.Empty(violations);
    }

    [Fact]
    public void Compute_WithViolations_ReportsCorrectSeverity()
    {
        var magicNumbers = new Dictionary<string, IReadOnlyList<MagicNumberViolation>>
        {
            ["/root/a.cs"] = new List<MagicNumberViolation>
            {
                new(1, "42", "if (x > 42)"),
                new(2, "100", "var y = 100;"),
            }
        };
        var result = new ScanResult([], 1, 100,
            new Dictionary<string, int>(),
            new Dictionary<string, NestingDepthInfo>(),
            new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>(),
            new Dictionary<string, int>(),
            magicNumbers,
            new Dictionary<string, IReadOnlyList<MagicStringViolation>>());
        var options = new ReportOptions(MagicNumberRule: new MagicNumberRule(new Dictionary<string, int> { ["*"] = 0 }, null));

        var violations = MagicNumberFileInfo.Compute(result, options, "/root");

        Assert.Single(violations);
        Assert.Equal(2, violations[0].Violations.Count);
        Assert.Equal(0, violations[0].Limit);
    }

    [Fact]
    public void Compute_BelowLimit_NoViolations()
    {
        var magicNumbers = new Dictionary<string, IReadOnlyList<MagicNumberViolation>>
        {
            ["/root/a.cs"] = new List<MagicNumberViolation>
            {
                new(1, "42", "if (x > 42)")
            }
        };
        var result = new ScanResult([], 1, 100,
            new Dictionary<string, int>(),
            new Dictionary<string, NestingDepthInfo>(),
            new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>(),
            new Dictionary<string, int>(),
            magicNumbers,
            new Dictionary<string, IReadOnlyList<MagicStringViolation>>());
        var options = new ReportOptions(MagicNumberRule: new MagicNumberRule(new Dictionary<string, int> { ["*"] = 5 }, null));

        var violations = MagicNumberFileInfo.Compute(result, options, "/root");

        Assert.Empty(violations);
    }

    [Fact]
    public void Compute_NoRuleConfigured_NoViolations()
    {
        var magicNumbers = new Dictionary<string, IReadOnlyList<MagicNumberViolation>>
        {
            ["/root/a.cs"] = new List<MagicNumberViolation>
            {
                new(1, "42", "if (x > 42)")
            }
        };
        var result = new ScanResult([], 1, 100,
            new Dictionary<string, int>(),
            new Dictionary<string, NestingDepthInfo>(),
            new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>(),
            new Dictionary<string, int>(),
            magicNumbers,
            new Dictionary<string, IReadOnlyList<MagicStringViolation>>());
        var options = new ReportOptions();

        var violations = MagicNumberFileInfo.Compute(result, options, "/root");

        Assert.Empty(violations);
    }
}
