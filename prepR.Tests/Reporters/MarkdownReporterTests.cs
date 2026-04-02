namespace Prepr.Tests;

public class MarkdownReporterTests
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
    public void Report_ContainsMarkdownHeader()
    {
        var reporter = new MarkdownReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("# prepr report", output);
    }

    [Fact]
    public void Report_ContainsBlockContent()
    {
        var reporter = new MarkdownReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("var x = 1;", output);
        Assert.Contains("```", output);
    }

    [Fact]
    public void Report_ContainsRelativePaths()
    {
        var reporter = new MarkdownReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("FileA.cs", output);
        Assert.Contains("FileB.cs", output);
    }

    [Fact]
    public void Report_ContainsPerFileSummaryTable()
    {
        var reporter = new MarkdownReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("## Per-file Summary", output);
        Assert.Contains("| File |", output);
    }

    [Fact]
    public void Report_NoDuplicates_ShowsNoneMessage()
    {
        var reporter = new MarkdownReporter();
        using var writer = new StringWriter();
        reporter.Report(new ScanResult([], 3, 100, new Dictionary<string, int>(), new Dictionary<string, (int, int)>(), new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>()), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("No duplicate blocks found.", output);
        Assert.DoesNotContain("## Duplicate Blocks", output);
    }

    [Fact]
    public void Report_ScanStats_ArePresent()
    {
        var reporter = new MarkdownReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("**Files scanned:** 5", output);
        Assert.Contains("**Total lines:** 200", output);
        Assert.Contains("**Duplicate blocks found:** 1", output);
    }
}
