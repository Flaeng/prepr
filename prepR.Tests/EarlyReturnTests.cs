namespace Prepr.Tests;

public class EarlyReturnAnalyzerTests
{
    private static IndexedLine[] MakeLines(params string[] lines)
        => lines.Select((text, idx) => new IndexedLine(idx + 1, text)).ToArray();

    [Fact]
    public void Analyze_NoElseBlocks_ReturnsEmpty()
    {
        var lines = MakeLines(
            "if (x > 0)",
            "{",
            "    DoSomething();",
            "}");

        var violations = EarlyReturnAnalyzer.Analyze(lines);

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_ShortElseWithReturn_ReportsViolation()
    {
        var lines = MakeLines(
            "if (x > 0)",
            "{",
            "    DoSomething();",
            "} else {",
            "    return;",
            "}");

        var violations = EarlyReturnAnalyzer.Analyze(lines);

        Assert.Single(violations);
        Assert.Equal(4, violations[0].LineNumber);
        Assert.Contains("guard clause", violations[0].Description);
    }

    [Fact]
    public void Analyze_ShortElseWithThrow_ReportsViolation()
    {
        var lines = MakeLines(
            "if (x > 0)",
            "{",
            "    DoSomething();",
            "} else {",
            "    throw new Exception();",
            "}");

        var violations = EarlyReturnAnalyzer.Analyze(lines);

        Assert.Single(violations);
        Assert.Equal(4, violations[0].LineNumber);
    }

    [Fact]
    public void Analyze_ShortElseWithReturnValue_ReportsViolation()
    {
        var lines = MakeLines(
            "if (condition)",
            "{",
            "    Process();",
            "} else {",
            "    return null;",
            "}");

        var violations = EarlyReturnAnalyzer.Analyze(lines);

        Assert.Single(violations);
    }

    [Fact]
    public void Analyze_LongElseBlock_NoViolation()
    {
        var lines = MakeLines(
            "if (x > 0)",
            "{",
            "    DoSomething();",
            "} else {",
            "    var a = 1;",
            "    var b = 2;",
            "    var c = 3;",
            "    return a + b + c;",
            "}");

        var violations = EarlyReturnAnalyzer.Analyze(lines);

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_ElseWithoutReturnOrThrow_NoViolation()
    {
        var lines = MakeLines(
            "if (x > 0)",
            "{",
            "    DoSomething();",
            "} else {",
            "    DoOther();",
            "}");

        var violations = EarlyReturnAnalyzer.Analyze(lines);

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_MultipleViolations_ReportsAll()
    {
        var lines = MakeLines(
            "if (a)",
            "{",
            "    DoA();",
            "} else {",
            "    return;",
            "}",
            "if (b)",
            "{",
            "    DoB();",
            "} else {",
            "    return;",
            "}");

        var violations = EarlyReturnAnalyzer.Analyze(lines);

        Assert.Equal(2, violations.Count);
        Assert.Equal(4, violations[0].LineNumber);
        Assert.Equal(10, violations[1].LineNumber);
    }

    [Fact]
    public void Analyze_ElseWithThreeLines_ReportsViolation()
    {
        var lines = MakeLines(
            "if (x)",
            "{",
            "    DoSomething();",
            "} else {",
            "    var msg = \"error\";",
            "    Log(msg);",
            "    return msg;",
            "}");

        var violations = EarlyReturnAnalyzer.Analyze(lines);

        Assert.Single(violations);
    }

    [Fact]
    public void Analyze_IfWrapsRemainingMethodBody_ReportsViolation()
    {
        // Pattern: if block is the last thing in a method and wraps all remaining logic.
        // Could be inverted to: if (items.Count <= 0) return; + unwrapped body.
        var lines = MakeLines(
            "private static void Method()",
            "{",
            "    var items = GetItems();",
            "    if (items.Count > 0)",
            "    {",
            "        Console.WriteLine(\"a\");",
            "        Console.WriteLine(\"b\");",
            "        Console.WriteLine(\"c\");",
            "    }",
            "}");

        var violations = EarlyReturnAnalyzer.Analyze(lines);

        Assert.Single(violations);
        Assert.Equal(4, violations[0].LineNumber);
        Assert.Contains("guard clause", violations[0].Description);
    }

    [Fact]
    public void Analyze_IfWrapsBody_WithElse_NoGuardViolation()
    {
        // If there's an else, this pattern doesn't apply
        var lines = MakeLines(
            "private static void Method()",
            "{",
            "    var items = GetItems();",
            "    if (items.Count > 0)",
            "    {",
            "        Console.WriteLine(\"a\");",
            "    }",
            "    else",
            "    {",
            "        Console.WriteLine(\"b\");",
            "    }",
            "}");

        var violations = EarlyReturnAnalyzer.Analyze(lines);

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_IfDoesNotWrapRemainingBody_NoViolation()
    {
        // Code continues after the if block — not a wrapping pattern
        var lines = MakeLines(
            "private static void Method()",
            "{",
            "    if (items.Count > 0)",
            "    {",
            "        Console.WriteLine(\"a\");",
            "    }",
            "    DoMore();",
            "}");

        var violations = EarlyReturnAnalyzer.Analyze(lines);

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_IfWrapsMethodBody_FollowedByAnotherMethod_ReportsViolation()
    {
        // The if wraps the rest of Method1's body, but Method2 follows in the same file.
        // This is the real-world pattern from ConsoleReporter.WriteIndentationRule.
        var lines = MakeLines(
            "private static void Method1(ScanResult result, string rootPath)",
            "{",
            "    var items = Compute(result);",
            "    if (items.Count > 0)",
            "    {",
            "        Console.WriteLine(\"header\");",
            "        foreach (var v in items)",
            "        {",
            "            Console.WriteLine(v);",
            "        }",
            "        Console.WriteLine();",
            "    }",
            "}",
            "",
            "private static void Method2()",
            "{",
            "    DoSomething();",
            "}");

        var violations = EarlyReturnAnalyzer.Analyze(lines);

        Assert.Single(violations);
        Assert.Equal(4, violations[0].LineNumber);
    }
}

public class EarlyReturnFileInfoTests
{
    [Fact]
    public void Compute_Disabled_ReturnsEmpty()
    {
        var earlyReturns = new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>
        {
            { "/root/a.cs", new List<EarlyReturnViolation> { new(10, "test") } }
        };
        var result = new ScanResult([], 1, 100,
            new Dictionary<string, int>(),
            new Dictionary<string, (int MaxDepth, IReadOnlyList<(int LineNumber, int Depth)> LineDepths)>(),
            earlyReturns);
        var options = new ReportOptions(EarlyReturn: false);

        var violations = EarlyReturnFileInfo.Compute(result, options);

        Assert.Empty(violations);
    }

    [Fact]
    public void Compute_Enabled_ReportsFiles()
    {
        var earlyReturns = new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>
        {
            { "/root/a.cs", new List<EarlyReturnViolation> { new(10, "test"), new(20, "test2") } },
            { "/root/b.cs", new List<EarlyReturnViolation>() },
            { "/root/c.cs", new List<EarlyReturnViolation> { new(5, "test3") } }
        };
        var result = new ScanResult([], 3, 300,
            new Dictionary<string, int>(),
            new Dictionary<string, (int MaxDepth, IReadOnlyList<(int LineNumber, int Depth)> LineDepths)>(),
            earlyReturns);
        var options = new ReportOptions(EarlyReturn: true);

        var violations = EarlyReturnFileInfo.Compute(result, options);

        Assert.Equal(2, violations.Count);
        // Sorted by violation count descending
        Assert.Equal("/root/a.cs", violations[0].FilePath);
        Assert.Equal(2, violations[0].Violations.Count);
        Assert.Equal("/root/c.cs", violations[1].FilePath);
    }

    [Fact]
    public void Compute_NoViolations_ReturnsEmpty()
    {
        var earlyReturns = new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>
        {
            { "/root/a.cs", new List<EarlyReturnViolation>() }
        };
        var result = new ScanResult([], 1, 100,
            new Dictionary<string, int>(),
            new Dictionary<string, (int MaxDepth, IReadOnlyList<(int LineNumber, int Depth)> LineDepths)>(),
            earlyReturns);
        var options = new ReportOptions(EarlyReturn: true);

        var violations = EarlyReturnFileInfo.Compute(result, options);

        Assert.Empty(violations);
    }
}
