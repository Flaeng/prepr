namespace Prepr.Tests;

public class HtmlReporterTests
{
    private static ScanResult CreateSampleResult()
    {
        var block = new DuplicateBlock(
            ["var x = 1;", "var y = 2;", "var z = 3;", "Console.WriteLine(x);", "Console.WriteLine(y);"],
            [
                new FileLocation("/src/FileA.cs", 10, 14),
                new FileLocation("/src/FileB.cs", 20, 24)
            ]);
        return new ScanResult([block], 5, 200, new Dictionary<string, int>(), new Dictionary<string, (int, int)>(), new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>());
    }

    [Fact]
    public void Report_IsValidHtmlStructure()
    {
        var reporter = new HtmlReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("<!DOCTYPE html>", output);
        Assert.Contains("<html class=\"dark\"", output);
        Assert.Contains("</html>", output);
        Assert.Contains("<head>", output);
        Assert.Contains("<main", output);
        Assert.Contains("</main>", output);
        Assert.Contains("</body>", output);
        Assert.Contains("<details", output);
    }

    [Fact]
    public void Report_ContainsTitle()
    {
        var reporter = new HtmlReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("<title>prepr report</title>", output);
    }

    [Fact]
    public void Report_ContainsBlockContent()
    {
        var reporter = new HtmlReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("var x = 1;", output);
        Assert.Contains("code-block", output);
        Assert.Contains("<pre", output);
    }

    [Fact]
    public void Report_HtmlEncodesContent()
    {
        var block = new DuplicateBlock(
            ["if (x < 5 && y > 3)", "var a = 1;", "var b = 2;", "var c = 3;", "var d = 4;"],
            [
                new FileLocation("/src/A.cs", 1, 5),
                new FileLocation("/src/B.cs", 1, 5)
            ]);
        var result = new ScanResult([block], 2, 10, new Dictionary<string, int>(), new Dictionary<string, (int, int)>(), new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>());

        var reporter = new HtmlReporter();
        using var writer = new StringWriter();
        reporter.Report(result, "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("&lt;", output);
        Assert.Contains("&amp;", output);
    }

    [Fact]
    public void Report_ContainsPerFileSummaryTable()
    {
        var reporter = new HtmlReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("File", output);
        Assert.Contains("Blocks", output);
        Assert.Contains("Per-file Summary", output);
    }

    [Fact]
    public void Report_NoDuplicates_ShowsNoneMessage()
    {
        var reporter = new HtmlReporter();
        using var writer = new StringWriter();
        reporter.Report(new ScanResult([], 3, 100, new Dictionary<string, int>(), new Dictionary<string, (int, int)>(), new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>()), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("No duplicate blocks found.", output);
        Assert.Contains("</html>", output);
    }

    [Fact]
    public void Report_IndentationOverage_ContainsPromptButtons()
    {
        var nestingDepths = new Dictionary<string, (int, int)>
        {
            ["/src/Deep.cs"] = (8, 42)
        };
        var block = new DuplicateBlock(
            ["var x = 1;", "var y = 2;", "var z = 3;", "Console.WriteLine(x);", "Console.WriteLine(y);"],
            [
                new FileLocation("/src/Deep.cs", 10, 14),
                new FileLocation("/src/Other.cs", 20, 24)
            ]);
        var result = new ScanResult([block], 1, 50,
            new Dictionary<string, int>(),
            nestingDepths,
            new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>());

        var options = new ReportOptions { IndentationRule = new IndentationRule(new Dictionary<string, int> { ["*"] = 3 }, null) };
        var reporter = new HtmlReporter();
        using var writer = new StringWriter();
        reporter.Report(result, "/src", writer, options);
        var output = writer.ToString();

        Assert.Contains("Indentation Overage", output);
        Assert.Contains("Show prompt", output);
        Assert.Contains("Copy prompt", output);
        Assert.Contains("showPromptModal", output);
        Assert.Contains("copyPrompt", output);
        Assert.Contains("Deep.cs", output);
        Assert.Contains("depth is 8", output);
        Assert.Contains("line 42", output);
        Assert.Contains("limit is 3", output);
    }

    [Fact]
    public void Report_LineLimitOverage_ContainsPromptButtons()
    {
        var lineCounts = new Dictionary<string, int>
        {
            ["/src/BigFile.cs"] = 500
        };
        var block = new DuplicateBlock(
            ["var x = 1;", "var y = 2;", "var z = 3;", "Console.WriteLine(x);", "Console.WriteLine(y);"],
            [
                new FileLocation("/src/BigFile.cs", 10, 14),
                new FileLocation("/src/Other.cs", 20, 24)
            ]);
        var result = new ScanResult([block], 1, 500,
            lineCounts,
            new Dictionary<string, (int, int)>(),
            new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>());

        var options = new ReportOptions { LineLimitRule = new LineLimitRule(new Dictionary<string, int> { ["*"] = 200 }, null) };
        var reporter = new HtmlReporter();
        using var writer = new StringWriter();
        reporter.Report(result, "/src", writer, options);
        var output = writer.ToString();

        Assert.Contains("Line Count Overage", output);
        Assert.Contains("Show prompt", output);
        Assert.Contains("Copy prompt", output);
        Assert.Contains("BigFile.cs", output);
        Assert.Contains("has 500 lines", output);
        Assert.Contains("limit is 200", output);
    }

    [Fact]
    public void Report_PerFileSummary_ContainsPromptButtons()
    {
        var reporter = new HtmlReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("Per-file Summary", output);
        Assert.Contains("Show prompt", output);
        Assert.Contains("Copy prompt", output);
        Assert.Contains("showPromptModal", output);
        Assert.Contains("eliminate code duplication", output);
    }
}
