using PrepR;

namespace prepR.Tests;

public class CsvReporterTests
{
    private static ScanResult CreateSampleResult()
    {
        var block = new DuplicateBlock(
            ["var x = 1;", "var y = 2;", "var z = 3;", "Console.WriteLine(x);", "Console.WriteLine(y);"],
            [
                new FileLocation("/src/FileA.cs", 10, 14),
                new FileLocation("/src/FileB.cs", 20, 24)
            ]);
        return new ScanResult([block], 5, 200, new Dictionary<string, int>());
    }

    [Fact]
    public void Report_ContainsHeaderRow()
    {
        var reporter = new CsvReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var lines = writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("BlockNumber,LineCount,OccurrenceCount,FilePath,StartLine,EndLine,Content", lines[0]);
    }

    [Fact]
    public void Report_CorrectRowCount()
    {
        var reporter = new CsvReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();
        var blockLines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .TakeWhile(l => !l.StartsWith("File,"))
            .ToArray();

        // 1 header + 2 data rows (one per location)
        Assert.Equal(3, blockLines.Length);
    }

    [Fact]
    public void Report_CorrectColumnCount()
    {
        var reporter = new CsvReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();
        var blockLines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .TakeWhile(l => !l.StartsWith("File,"))
            .ToArray();

        // Each block data row should have 7 columns
        foreach (var line in blockLines)
        {
            Assert.Equal(7, line.Split(',').Length);
        }
    }

    [Fact]
    public void Report_EscapesCommasInContent()
    {
        var block = new DuplicateBlock(
            ["var x = 1, y = 2;", "a();", "b();", "c();", "d();"],
            [
                new FileLocation("/src/A.cs", 1, 5),
                new FileLocation("/src/B.cs", 1, 5)
            ]);
        var result = new ScanResult([block], 2, 10, new Dictionary<string, int>());

        var reporter = new CsvReporter();
        using var writer = new StringWriter();
        reporter.Report(result, "/src", writer, new ReportOptions());
        var output = writer.ToString();

        // Content with comma should be quoted
        Assert.Contains("\"var x = 1, y = 2;\"", output);
    }

    [Fact]
    public void Report_EscapesQuotesInContent()
    {
        var block = new DuplicateBlock(
            ["Console.WriteLine(\"hello\");", "a();", "b();", "c();", "d();"],
            [
                new FileLocation("/src/A.cs", 1, 5),
                new FileLocation("/src/B.cs", 1, 5)
            ]);
        var result = new ScanResult([block], 2, 10, new Dictionary<string, int>());

        var reporter = new CsvReporter();
        using var writer = new StringWriter();
        reporter.Report(result, "/src", writer, new ReportOptions());
        var output = writer.ToString();

        // Quotes should be doubled and wrapped
        Assert.Contains("\"\"hello\"\"", output);
    }

    [Fact]
    public void Report_NoDuplicates_OnlyHeaderRow()
    {
        var reporter = new CsvReporter();
        using var writer = new StringWriter();
        reporter.Report(new ScanResult([], 3, 100, new Dictionary<string, int>()), "/src", writer, new ReportOptions());
        var lines = writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Single(lines);
        Assert.StartsWith("BlockNumber", lines[0]);
    }
}
