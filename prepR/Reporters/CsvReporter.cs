namespace Prepr.Reporters;

public class CsvReporter : IReporter
{
    public string FileExtension => ".csv";
    public void Report(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var score = TechDebtScore.Compute(result, options, rootPath);
        var fileInfos = DuplicationFileInfo.ComputePerFile(result, options);
        var overLimit = OverLimitFileInfo.Compute(result, options, rootPath);
        var overIndented = OverIndentedFileInfo.Compute(result, options, rootPath);
        var earlyReturnViolations = EarlyReturnFileInfo.Compute(result, options);
        var commentDensityViolations = CommentDensityFileInfo.Compute(result, options, rootPath);
        var magicNumberViolations = MagicNumberFileInfo.Compute(result, options, rootPath);
        var magicStringViolations = MagicStringFileInfo.Compute(result, options, rootPath);
        var pairs = FilePairGroup.ComputeFilePairs(result);

        // Summary stats
        writer.WriteLine("FilesScanned,TotalLines,TechDebtScore,Grade");
        writer.WriteLine($"{result.TotalFilesScanned},{result.TotalLinesScanned},{score.Score.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},{score.Grade}");

        // Severity counts
        writer.WriteLine();
        writer.WriteLine("Rule,High,Medium,Low");
        writer.WriteLine($"Duplication,{fileInfos.Count(f => f.Severity == Severity.High)},{fileInfos.Count(f => f.Severity == Severity.Medium)},{fileInfos.Count(f => f.Severity == Severity.Low)}");
        writer.WriteLine($"LineLimit,{overLimit.Count(v => v.Severity == Severity.High)},{overLimit.Count(v => v.Severity == Severity.Medium)},{overLimit.Count(v => v.Severity == Severity.Low)}");
        writer.WriteLine($"Indentation,{overIndented.Count(v => v.Severity == Severity.High)},{overIndented.Count(v => v.Severity == Severity.Medium)},{overIndented.Count(v => v.Severity == Severity.Low)}");
        writer.WriteLine($"EarlyReturn,{earlyReturnViolations.Count(f => f.Severity == Severity.High)},{earlyReturnViolations.Count(f => f.Severity == Severity.Medium)},{earlyReturnViolations.Count(f => f.Severity == Severity.Low)}");
        writer.WriteLine($"CommentDensity,{commentDensityViolations.Count(v => v.Severity == Severity.High)},{commentDensityViolations.Count(v => v.Severity == Severity.Medium)},{commentDensityViolations.Count(v => v.Severity == Severity.Low)}");
        writer.WriteLine($"MagicNumber,{magicNumberViolations.Count(v => v.Severity == Severity.High)},{magicNumberViolations.Count(v => v.Severity == Severity.Medium)},{magicNumberViolations.Count(v => v.Severity == Severity.Low)}");
        writer.WriteLine($"MagicString,{magicStringViolations.Count(v => v.Severity == Severity.High)},{magicStringViolations.Count(v => v.Severity == Severity.Medium)},{magicStringViolations.Count(v => v.Severity == Severity.Low)}");

        // Duplicate blocks
        writer.WriteLine();
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

        // File pairs
        if (pairs.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("FileA,FileB,SharedBlocks,SharedLines");
            foreach (var pair in pairs)
            {
                var relA = Path.GetRelativePath(rootPath, pair.FileA);
                var relB = Path.GetRelativePath(rootPath, pair.FileB);
                writer.WriteLine($"{CsvEscape(relA)},{CsvEscape(relB)},{pair.SharedBlocks.Count},{pair.SharedLineCount}");
            }
        }

        // Files exceeding line limit
        if (overLimit.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("File,LineCount,Limit,Severity");
            foreach (var v in overLimit)
            {
                var relativePath = Path.GetRelativePath(rootPath, v.FilePath);
                writer.WriteLine($"{CsvEscape(relativePath)},{v.LineCount},{v.Limit},{v.Severity}");
            }
        }

        // Files exceeding indentation limit
        if (overIndented.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("File,MaxDepth,Lines,Limit,Severity");
            foreach (var v in overIndented)
            {
                var relativePath = Path.GetRelativePath(rootPath, v.FilePath);
                writer.WriteLine($"{CsvEscape(relativePath)},{v.MaxDepth},{CsvEscape(v.RangesDisplay)},{v.Limit},{v.Severity}");
            }
        }

        // Early return violations
        if (earlyReturnViolations.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("File,Line,Description,Severity");
            foreach (var file in earlyReturnViolations)
            {
                var relativePath = Path.GetRelativePath(rootPath, file.FilePath);
                foreach (var v in file.Violations)
                {
                    writer.WriteLine($"{CsvEscape(relativePath)},{v.LineNumber},{CsvEscape(v.Description)},{file.Severity}");
                }
            }
        }

        // Comment density violations
        if (commentDensityViolations.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("File,CommentLines,TotalLines,DensityPct,LimitPct,Direction,Severity");
            foreach (var v in commentDensityViolations)
            {
                var relativePath = Path.GetRelativePath(rootPath, v.FilePath);
                var direction = v.IsBelowMin ? "BelowMin" : "AboveMax";
                writer.WriteLine($"{CsvEscape(relativePath)},{v.CommentLines},{v.TotalLines},{v.DensityPercent.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},{v.LimitPercent.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},{direction},{v.Severity}");
            }
        }

        // Magic number violations
        if (magicNumberViolations.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("File,Line,Value,Severity");
            foreach (var file in magicNumberViolations)
            {
                var relativePath = Path.GetRelativePath(rootPath, file.FilePath);
                foreach (var v in file.Violations)
                {
                    writer.WriteLine($"{CsvEscape(relativePath)},{v.LineNumber},{CsvEscape(v.Value)},{file.Severity}");
                }
            }
        }

        // Magic string violations
        if (magicStringViolations.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("File,Line,Value,Severity");
            foreach (var file in magicStringViolations)
            {
                var relativePath = Path.GetRelativePath(rootPath, file.FilePath);
                foreach (var v in file.Violations)
                {
                    writer.WriteLine($"{CsvEscape(relativePath)},{v.LineNumber},{CsvEscape(v.Value)},{file.Severity}");
                }
            }
        }

        // Tech Debt Score (detailed)
        writer.WriteLine();
        writer.WriteLine("TechDebtScore,Grade");
        writer.WriteLine($"{score.Score.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},{score.Grade}");
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
