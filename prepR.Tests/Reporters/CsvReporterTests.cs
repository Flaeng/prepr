namespace Prepr.Tests;

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
        return new ScanResult([block], 5, 200, new Dictionary<string, int>(), new Dictionary<string, (int, int)>(), new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>());
    }

    [Fact]
    public void Report_ContainsHeaderRow()
    {
        var reporter = new CsvReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var lines = writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("BlockNumber,LineCount,OccurrenceCount,FilePath,StartLine,EndLine", lines[0]);
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

        // Each block data row should have 6 columns
        foreach (var line in blockLines)
        {
            Assert.Equal(6, line.Split(',').Length);
        }
    }

    [Fact]
    public void Report_NoDuplicates_OnlyHeaderRow()
    {
        var reporter = new CsvReporter();
        using var writer = new StringWriter();
        reporter.Report(new ScanResult([], 3, 100, new Dictionary<string, int>(), new Dictionary<string, (int, int)>(), new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>()), "/src", writer, new ReportOptions());
        var lines = writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Single(lines);
        Assert.StartsWith("BlockNumber", lines[0]);
    }
}
