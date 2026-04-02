using System.Text;

namespace Prepr;

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
                    WriteToFile(new HtmlReporter(), result, rootPath, basePath, reportOptions);
                    break;
                case "md":
                    WriteToFile(new MarkdownReporter(), result, rootPath, basePath, reportOptions);
                    break;
                case "csv":
                    WriteToFile(new CsvReporter(), result, rootPath, basePath, reportOptions);
                    break;
                case "prompt":
                    WriteToFile(new PromptReporter(), result, rootPath, basePath, reportOptions);
                    break;
                default:
                    Console.Error.WriteLine($"Warning: Unknown output format '{format}', skipping.");
                    break;
            }
        }
    }

    private static void WriteToFile(IReporter reporter, ScanResult result, string rootPath, string basePath, ReportOptions options)
    {
        var filePath = basePath + reporter.FileExtension;
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        reporter.Report(result, rootPath, writer, options);
        Console.WriteLine($"Report written to: {filePath}");
    }
}
