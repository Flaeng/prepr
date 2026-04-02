namespace Prepr.Tests;

public class MagicStringTests
{
    [Fact]
    public void FindMagicStrings_DetectsStringLiterals()
    {
        var lines = new IndexedLine[]
        {
            new(1, "var x = \"hello world\";"),
        };

        var violations = RuleChecker.FindMagicStrings(lines);

        Assert.Single(violations);
        Assert.Equal("hello world", violations[0].Value);
    }

    [Fact]
    public void FindMagicStrings_IgnoresEmptyStrings()
    {
        var lines = new IndexedLine[]
        {
            new(1, "var x = \"\";"),
        };

        var violations = RuleChecker.FindMagicStrings(lines);

        Assert.Empty(violations);
    }

    [Fact]
    public void FindMagicStrings_IgnoresSingleCharStrings()
    {
        var lines = new IndexedLine[]
        {
            new(1, "var x = \"a\";"),
        };

        var violations = RuleChecker.FindMagicStrings(lines);

        Assert.Empty(violations);
    }

    [Fact]
    public void FindMagicStrings_IgnoresSingleLineComments()
    {
        var lines = new IndexedLine[]
        {
            new(1, "// var x = \"hello\";"),
        };

        var violations = RuleChecker.FindMagicStrings(lines);

        Assert.Empty(violations);
    }

    [Fact]
    public void FindMagicStrings_IgnoresBlockComments()
    {
        var lines = new IndexedLine[]
        {
            new(1, "/* start"),
            new(2, "var x = \"hello\";"),
            new(3, "end */"),
            new(4, "var y = \"world\";"),
        };

        var violations = RuleChecker.FindMagicStrings(lines);

        Assert.Single(violations);
        Assert.Equal("world", violations[0].Value);
    }

    [Fact]
    public void FindMagicStrings_DetectsMultipleOnSameLine()
    {
        var lines = new IndexedLine[]
        {
            new(1, "Console.WriteLine(\"hello\" + \"world\");"),
        };

        var violations = RuleChecker.FindMagicStrings(lines);

        Assert.Equal(2, violations.Count);
    }

    [Fact]
    public void FindMagicStrings_EmptyLines_NoViolations()
    {
        var lines = Array.Empty<IndexedLine>();

        var violations = RuleChecker.FindMagicStrings(lines);

        Assert.Empty(violations);
    }

    [Fact]
    public void Compute_RepeatedStrings_ReportsViolation()
    {
        var magicStrings = new Dictionary<string, IReadOnlyList<MagicStringViolation>>
        {
            ["/root/a.cs"] = new List<MagicStringViolation>
            {
                new(1, "connection_string", "var x = \"connection_string\";"),
                new(5, "connection_string", "var y = \"connection_string\";"),
                new(10, "connection_string", "var z = \"connection_string\";"),
            }
        };
        var result = new ScanResult([], 1, 100,
            new Dictionary<string, int>(),
            new Dictionary<string, NestingDepthInfo>(),
            new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>(),
            new Dictionary<string, int>(),
            new Dictionary<string, IReadOnlyList<MagicNumberViolation>>(),
            magicStrings);
        var options = new ReportOptions(MagicStringRule: new MagicStringRule(new Dictionary<string, int> { ["*"] = 2 }, null));

        var violations = MagicStringFileInfo.Compute(result, options, "/root");

        Assert.Single(violations);
        Assert.Equal(3, violations[0].Violations.Count);
    }

    [Fact]
    public void Compute_BelowThreshold_NoViolations()
    {
        var magicStrings = new Dictionary<string, IReadOnlyList<MagicStringViolation>>
        {
            ["/root/a.cs"] = new List<MagicStringViolation>
            {
                new(1, "hello", "var x = \"hello\";"),
            }
        };
        var result = new ScanResult([], 1, 100,
            new Dictionary<string, int>(),
            new Dictionary<string, NestingDepthInfo>(),
            new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>(),
            new Dictionary<string, int>(),
            new Dictionary<string, IReadOnlyList<MagicNumberViolation>>(),
            magicStrings);
        var options = new ReportOptions(MagicStringRule: new MagicStringRule(new Dictionary<string, int> { ["*"] = 2 }, null));

        var violations = MagicStringFileInfo.Compute(result, options, "/root");

        Assert.Empty(violations);
    }

    [Fact]
    public void Compute_NoRuleConfigured_NoViolations()
    {
        var magicStrings = new Dictionary<string, IReadOnlyList<MagicStringViolation>>
        {
            ["/root/a.cs"] = new List<MagicStringViolation>
            {
                new(1, "hello", "var x = \"hello\";"),
                new(5, "hello", "var y = \"hello\";"),
                new(10, "hello", "var z = \"hello\";"),
            }
        };
        var result = new ScanResult([], 1, 100,
            new Dictionary<string, int>(),
            new Dictionary<string, NestingDepthInfo>(),
            new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>(),
            new Dictionary<string, int>(),
            new Dictionary<string, IReadOnlyList<MagicNumberViolation>>(),
            magicStrings);
        var options = new ReportOptions();

        var violations = MagicStringFileInfo.Compute(result, options, "/root");

        Assert.Empty(violations);
    }
}
