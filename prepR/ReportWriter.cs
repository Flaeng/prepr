using System.Text;

namespace PrepR;

public static class ReportWriter
{
    public static void WriteAll(ScanResult result, string rootPath, RunOptions runOptions, ReportOptions reportOptions)
    {
        var basePath = runOptions.OutputFile ?? Path.Combine(Directory.GetCurrentDirectory(), "report.prepr");

        foreach (var format in runOptions.Outputs.Select(o => o.ToLowerInvariant()).Distinct())
        {
            switch (format)
            {
                case "console":
                    ConsoleReporter.Print(result, rootPath, reportOptions);
                    break;
                case "html":
                    WriteToFile(new HtmlReporter(), result, rootPath, basePath + ".html", reportOptions);
                    break;
                case "md":
                    WriteToFile(new MarkdownReporter(), result, rootPath, basePath + ".md", reportOptions);
                    break;
                case "csv":
                    WriteToFile(new CsvReporter(), result, rootPath, basePath + ".csv", reportOptions);
                    break;
                case "prompt":
                    WriteToFile(new PromptReporter(), result, rootPath, basePath + ".prompt.md", reportOptions);
                    break;
                default:
                    Console.Error.WriteLine($"Warning: Unknown output format '{format}', skipping.");
                    break;
            }
        }
    }

    private static void WriteToFile(IReporter reporter, ScanResult result, string rootPath, string filePath, ReportOptions options)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        reporter.Report(result, rootPath, writer, options);
        Console.WriteLine($"Report written to: {filePath}");
    }
}
