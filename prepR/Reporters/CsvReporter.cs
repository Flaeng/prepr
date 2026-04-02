namespace Prepr.Reporters;

public class CsvReporter : IReporter
{
    public string FileExtension => ".csv";
    public void Report(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        writer.WriteLine("BlockNumber,LineCount,OccurrenceCount,FilePath,StartLine,EndLine");

        for (int i = 0; i < result.Duplicates.Count; i++)
        {
            var block = result.Duplicates[i];

            foreach (var loc in block.Locations)
            {
                var relativePath = Path.GetRelativePath(rootPath, loc.FilePath);
                writer.WriteLine($"{i + 1},{block.Lines.Length},{block.Locations.Count},{CsvEscape(relativePath)},{loc.StartLine},{loc.EndLine}");
            }
        }

        // Per-file severity summary
        var fileInfos = FileDuplicationInfo.ComputePerFile(result, options);
        if (fileInfos.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("File,Blocks,DuplicatedLines,TotalLines,DuplicationPct,Severity");
            foreach (var info in fileInfos)
            {
                var relativePath = Path.GetRelativePath(rootPath, info.FilePath);
                writer.WriteLine($"{CsvEscape(relativePath)},{info.DuplicateBlockCount},{info.DuplicatedLineCount},{info.TotalLineCount},{info.DuplicationPercentage.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},{info.Severity}");
            }
        }

        // Files exceeding line limit
        var overLimit = OverLimitFileInfo.Compute(result, options, rootPath);
        if (overLimit.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("File,LineCount,Limit");
            foreach (var v in overLimit)
            {
                var relativePath = Path.GetRelativePath(rootPath, v.FilePath);
                writer.WriteLine($"{CsvEscape(relativePath)},{v.LineCount},{v.Limit}");
            }
        }
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
