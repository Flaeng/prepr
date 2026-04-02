namespace Prepr.Tests;

public class PromptReporterTests
{
    private static ScanResult CreateSampleResult()
    {
        var block = new DuplicateBlock(
            ["var x = 1;", "var y = 2;", "var z = 3;", "Console.WriteLine(x);", "Console.WriteLine(y);"],
            [
                new FileLocation("/src/FileA.cs", 10, 14),
                new FileLocation("/src/FileB.cs", 20, 24)
            ]);
        return new ScanResult([block], 5, 200, new Dictionary<string, int>(), new Dictionary<string, NestingDepthInfo>(), new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>(), new Dictionary<string, int>());
    }

    [Fact]
    public void Report_ContainsRoadmapHeader()
    {
        var reporter = new PromptReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("# Code Quality Roadmap", output);
    }

    [Fact]
    public void Report_ContainsBlockContent()
    {
        var reporter = new PromptReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("var x = 1;", output);
        Assert.Contains("```", output);
    }

    [Fact]
    public void Report_ContainsLocationsWithRelativePaths()
    {
        var reporter = new PromptReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("FileA.cs", output);
        Assert.Contains("FileB.cs", output);
        Assert.Contains("lines 10", output);
    }

    [Fact]
    public void Report_ContainsActionInstruction()
    {
        var reporter = new PromptReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("Refactor to remove this duplication", output);
    }

    [Fact]
    public void Report_ContainsScanSummary()
    {
        var reporter = new PromptReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("5 files scanned", output);
        Assert.Contains("1 duplicate block(s) found", output);
    }

    [Fact]
    public void Report_NoIssues_ShowsNoActionNeeded()
    {
        var reporter = new PromptReporter();
        using var writer = new StringWriter();
        reporter.Report(new ScanResult([], 3, 100, new Dictionary<string, int>(), new Dictionary<string, NestingDepthInfo>(), new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>(), new Dictionary<string, int>()), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("No issues found", output);
    }

    [Fact]
    public void Report_GroupsIssuesIntoPhases()
    {
        var reporter = new PromptReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Matches("## Phase \\d:", output);
    }

    [Fact]
    public void Report_AssignsTaskIds()
    {
        var reporter = new PromptReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("TASK-001:", output);
    }
}
