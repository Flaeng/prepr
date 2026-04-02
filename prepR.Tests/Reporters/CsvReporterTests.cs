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
        return new ScanResult([block], 5, 200, new Dictionary<string, int>(), new Dictionary<string, (int MaxDepth, IReadOnlyList<(int LineNumber, int Depth)> LineDepths)>(), new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>());
    }

    [Fact]
    public void Report_ContainsHeaderRow()
    {
        var reporter = new CsvReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("BlockNumber,LineCount,OccurrenceCount,FilePath,StartLine,EndLine", output);
    }

    [Fact]
    public void Report_CorrectRowCount()
    {
        var reporter = new CsvReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var blockHeaderIndex = Array.IndexOf(lines, "BlockNumber,LineCount,OccurrenceCount,FilePath,StartLine,EndLine");
        var dataLines = lines.Skip(blockHeaderIndex + 1)
            .TakeWhile(l => l.Contains(',') && !l.StartsWith("File,"))
            .ToArray();

        // 2 data rows (one per location)
        Assert.Equal(2, dataLines.Length);
    }

    [Fact]
    public void Report_CorrectColumnCount()
    {
        var reporter = new CsvReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();
        var blockSection = output.Split("BlockNumber,LineCount,OccurrenceCount,FilePath,StartLine,EndLine")[1];
        var dataLines = blockSection.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .TakeWhile(l => l.Contains(',') && !l.StartsWith("File,"))
            .ToArray();

        // Each block data row should have 6 columns
        foreach (var line in dataLines)
        {
            Assert.Equal(6, line.Split(',').Length);
        }
    }

    [Fact]
    public void Report_NoDuplicates_ContainsSummaryAndScore()
    {
        var reporter = new CsvReporter();
        using var writer = new StringWriter();
        reporter.Report(new ScanResult([], 3, 100, new Dictionary<string, int>(), new Dictionary<string, (int MaxDepth, IReadOnlyList<(int LineNumber, int Depth)> LineDepths)>(), new Dictionary<string, IReadOnlyList<EarlyReturnViolation>>()), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("FilesScanned,TotalLines,TechDebtScore,Grade", output);
        Assert.Contains("3,100,", output);
        Assert.Contains("Rule,High,Medium,Low", output);
        Assert.Contains("TechDebtScore,Grade", output);
        Assert.Contains("0.0,A", output);
    }

    [Fact]
    public void Report_ContainsSummaryStats()
    {
        var reporter = new CsvReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("FilesScanned,TotalLines,TechDebtScore,Grade", output);
        Assert.Contains("5,200,", output);
    }

    [Fact]
    public void Report_ContainsSeverityCounts()
    {
        var reporter = new CsvReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("Rule,High,Medium,Low", output);
        Assert.Contains("Duplication,", output);
        Assert.Contains("LineLimit,", output);
        Assert.Contains("Indentation,", output);
        Assert.Contains("EarlyReturn,", output);
    }

    [Fact]
    public void Report_WithDuplicates_ContainsFilePairs()
    {
        var reporter = new CsvReporter();
        using var writer = new StringWriter();
        reporter.Report(CreateSampleResult(), "/src", writer, new ReportOptions());
        var output = writer.ToString();

        Assert.Contains("FileA,FileB,SharedBlocks,SharedLines", output);
    }
}
