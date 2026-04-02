namespace prepr;

public interface IReporter
{
    void Report(ScanResult result, string rootPath, TextWriter writer, ReportOptions options);
}
