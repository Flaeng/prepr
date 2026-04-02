namespace prepr;

public class CsvReporter : IReporter
{
    public void Report(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        writer.WriteLine("BlockNumber,LineCount,OccurrenceCount,FilePath,StartLine,EndLine,Content");

        for (int i = 0; i < result.Duplicates.Count; i++)
        {
            var block = result.Duplicates[i];
            var content = CsvEscape(block.Lines.Length > 0 ? block.Lines[0] : "");

            foreach (var loc in block.Locations)
            {
                var relativePath = Path.GetRelativePath(rootPath, loc.FilePath);
                writer.WriteLine($"{i + 1},{block.Lines.Length},{block.Locations.Count},{CsvEscape(relativePath)},{loc.StartLine},{loc.EndLine},{content}");
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
                writer.WriteLine($"{CsvEscape(relativePath)},{info.DuplicateBlockCount},{info.DuplicatedLineCount},{info.TotalLineCount},{info.DuplicationPercentage:F1},{info.Severity}");
            }
        }

        // Files exceeding line limit
        if (options.LineLimitRule is not null)
        {
            var overLimit = OverLimitFileInfo.Compute(result, options.LineLimitRule, rootPath);
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
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
