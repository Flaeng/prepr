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
}
