namespace Prepr.Reporters;

public interface IReporter
{
    string FileExtension { get; }
    void Report(ScanResult result, string rootPath, TextWriter writer, ReportOptions options);
}
