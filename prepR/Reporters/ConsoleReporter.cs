namespace Prepr.Reporters;

public class ConsoleReporter : IReporter
{
    public string FileExtension => string.Empty;
    public void Report(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        Print(result, rootPath, options);
    }

    public static void Print(ScanResult result, string rootPath, ReportOptions? options = null)
    {
        options ??= new ReportOptions();
        var stats = SummaryStatistics.Compute(result);

        if (options.Verbosity == Verbosity.Quiet)
        {
            PrintQuiet(stats);
            return;
        }

        var techDebt = TechDebtScore.Compute(result, options, rootPath);
        var gradeColor = techDebt.Grade switch
        {
            'A' or 'B' => ConsoleColor.Green,
            'C' => ConsoleColor.Yellow,
            _ => ConsoleColor.Red
        };

        Console.WriteLine();
        WriteLine("prepr report", ConsoleColor.White);
        Write($"Scanned: {result.TotalFilesScanned} files, {result.TotalLinesScanned} total lines", ConsoleColor.Gray);
        Write("  │  Tech Debt: ", ConsoleColor.DarkGray);
        Write($"{techDebt.Score:F1}/100", gradeColor);
        Write(" Grade: ", ConsoleColor.DarkGray);
        WriteLine($"{techDebt.Grade}", gradeColor);
        Console.WriteLine(new string('─', 60));

        if (result.Duplicates.Count == 0)
        {
            WriteLine("No duplicate blocks found.", ConsoleColor.Green);
            Console.WriteLine();
            WriteTechDebtScore(result, rootPath, options);
            return;
        }

        WriteDuplicates(result, rootPath);

        var fileInfos = DuplicationFileInfo.ComputePerFile(result, options);

        // Per-file summary with severity
        Console.WriteLine();
        Console.WriteLine(new string('─', 60));
        var highDup = fileInfos.Count(f => f.Severity == Severity.High);
        var medDup = fileInfos.Count(f => f.Severity == Severity.Medium);
        var lowDup = fileInfos.Count(f => f.Severity == Severity.Low);
        Write("Per-file Summary", ConsoleColor.White);
        WriteSeverityCounts(highDup, medDup, lowDup);
        Console.WriteLine();

        foreach (var info in fileInfos)
        {
            var relativePath = Path.GetRelativePath(rootPath, info.FilePath);
            var severityColor = info.Severity switch
            {
                Severity.High => ConsoleColor.Red,
                Severity.Medium => ConsoleColor.Yellow,
                _ => ConsoleColor.Green
            };
            Write("  ", ConsoleColor.DarkGray);
            Write($"[{info.Severity}] ", severityColor);
            Write($"{relativePath}", ConsoleColor.Cyan);
            WriteLine($"  {info.DuplicateBlockCount} block(s), {info.DuplicatedLineCount} duplicated line(s) ({info.DuplicationPercentage:F1}%)", ConsoleColor.Gray);
        }

        // File pairs
        var pairs = FilePairGroup.ComputeFilePairs(result);
        if (pairs.Count > 0)
        {
            WritePairs(rootPath, options, pairs);
        }

        Console.WriteLine();
        Console.WriteLine(new string('─', 60));
        WriteLine($"Total: {stats.TotalDuplicateBlocks} duplicate block(s) across {result.TotalFilesScanned} files", ConsoleColor.White);
        Console.WriteLine();
        // Files exceeding line limit
        if (options.LineLimitRule is not null)
        {
            WriteLineLimitRule(result, rootPath, options);
        }
        // Files exceeding indentation limit
        if (options.IndentationRule is not null)
        {
            WriteIndentationRule(result, rootPath, options);
        }
        // Early return violations
        if (options.EarlyReturn)
        {
            WriteEarlyReturnRule(result, rootPath);
        }

        // Comment density violations
        if (options.MinCommentDensityRule is not null || options.MaxCommentDensityRule is not null)
        {
            WriteCommentDensityRule(result, rootPath, options);
        }

        // Magic number violations
        if (options.MagicNumberRule is not null)
        {
            WriteMagicNumberRule(result, rootPath, options);
        }

        // Magic string violations
        if (options.MagicStringRule is not null)
        {
            WriteMagicStringRule(result, rootPath, options);
        }

        // Tech Debt Score
        WriteTechDebtScore(result, rootPath, options);
    }

    private static void WriteLineLimitRule(ScanResult result, string rootPath, ReportOptions options)
    {
        var overLimit = OverLimitFileInfo.Compute(result, options, rootPath);
        if (overLimit.Count <= 0)
            return;

        Console.WriteLine(new string('\u2500', 60));
        var highLL = overLimit.Count(v => v.Severity == Severity.High);
        var medLL = overLimit.Count(v => v.Severity == Severity.Medium);
        var lowLL = overLimit.Count(v => v.Severity == Severity.Low);
        Write($"Line Count Overage ({overLimit.Count} file(s))", ConsoleColor.White);
        WriteSeverityCounts(highLL, medLL, lowLL);
        Console.WriteLine();
        foreach (var v in overLimit)
        {
            var relativePath = Path.GetRelativePath(rootPath, v.FilePath);
            var severityColor = v.Severity switch
            {
                Severity.High => ConsoleColor.Red,
                Severity.Medium => ConsoleColor.Yellow,
                _ => ConsoleColor.Green
            };
            Write("  ", ConsoleColor.DarkGray);
            Write($"[{v.Severity}] ", severityColor);
            Write($"{relativePath}", ConsoleColor.Cyan);
            WriteLine($"  {v.LineCount} lines (limit: {v.Limit})", ConsoleColor.Gray);
        }
        Console.WriteLine();
    }

    private static void WriteIndentationRule(ScanResult result, string rootPath, ReportOptions options)
    {
        var overIndented = OverIndentedFileInfo.Compute(result, options, rootPath);
        if (overIndented.Count > 0)
        {
            Console.WriteLine(new string('\u2500', 60));
            var highInd = overIndented.Count(v => v.Severity == Severity.High);
            var medInd = overIndented.Count(v => v.Severity == Severity.Medium);
            var lowInd = overIndented.Count(v => v.Severity == Severity.Low);
            Write($"Indentation Overage ({overIndented.Count} file(s))", ConsoleColor.White);
            WriteSeverityCounts(highInd, medInd, lowInd);
            Console.WriteLine();
            foreach (var v in overIndented)
            {
                var relativePath = Path.GetRelativePath(rootPath, v.FilePath);
                var severityColor = v.Severity switch
                {
                    Severity.High => ConsoleColor.Red,
                    Severity.Medium => ConsoleColor.Yellow,
                    _ => ConsoleColor.Green
                };
                Write("  ", ConsoleColor.DarkGray);
                Write($"[{v.Severity}] ", severityColor);
                Write($"{relativePath}", ConsoleColor.Cyan);
                WriteLine($"  depth {v.MaxDepth} at {v.RangesDisplay} (limit: {v.Limit})", ConsoleColor.Gray);
            }
            Console.WriteLine();
        }
    }

    private static void WriteEarlyReturnRule(ScanResult result, string rootPath)
    {
        var violations = EarlyReturnFileInfo.Compute(result, new ReportOptions(EarlyReturn: true));
        if (violations.Count <= 0)
            return;

        Console.WriteLine(new string('\u2500', 60));
        var totalViolations = violations.Sum(f => f.Violations.Count);
        var highER = violations.Count(f => f.Severity == Severity.High);
        var medER = violations.Count(f => f.Severity == Severity.Medium);
        var lowER = violations.Count(f => f.Severity == Severity.Low);
        Write($"Early Return Opportunities ({totalViolations} in {violations.Count} file(s))", ConsoleColor.White);
        WriteSeverityCounts(highER, medER, lowER);
        Console.WriteLine();
        foreach (var file in violations)
        {
            var relativePath = Path.GetRelativePath(rootPath, file.FilePath);
            var severityColor = file.Severity switch
            {
                Severity.High => ConsoleColor.Red,
                Severity.Medium => ConsoleColor.Yellow,
                _ => ConsoleColor.Green
            };
            Write("  ", ConsoleColor.DarkGray);
            Write($"[{file.Severity}] ", severityColor);
            Write($"{relativePath}", ConsoleColor.Cyan);
            WriteLine($"  {file.Violations.Count} violation(s)", ConsoleColor.Gray);
            foreach (var v in file.Violations)
            {
                Write("      ", ConsoleColor.DarkGray);
                Write($"Line {v.LineNumber}: ", ConsoleColor.Yellow);
                WriteLine(v.Description, ConsoleColor.Gray);
            }
        }
        Console.WriteLine();
    }

    private static void WriteCommentDensityRule(ScanResult result, string rootPath, ReportOptions options)
    {
        var violations = CommentDensityFileInfo.Compute(result, options, rootPath);
        if (violations.Count <= 0)
            return;

        Console.WriteLine(new string('\u2500', 60));
        var highCD = violations.Count(v => v.Severity == Severity.High);
        var medCD = violations.Count(v => v.Severity == Severity.Medium);
        var lowCD = violations.Count(v => v.Severity == Severity.Low);
        Write($"Comment Density ({violations.Count} file(s))", ConsoleColor.White);
        WriteSeverityCounts(highCD, medCD, lowCD);
        Console.WriteLine();
        foreach (var v in violations)
        {
            var relativePath = Path.GetRelativePath(rootPath, v.FilePath);
            var severityColor = v.Severity switch
            {
                Severity.High => ConsoleColor.Red,
                Severity.Medium => ConsoleColor.Yellow,
                _ => ConsoleColor.Green
            };
            var direction = v.IsBelowMin ? "below min" : "above max";
            Write("  ", ConsoleColor.DarkGray);
            Write($"[{v.Severity}] ", severityColor);
            Write($"{relativePath}", ConsoleColor.Cyan);
            WriteLine($"  {v.DensityPercent:F1}% ({direction}: {v.LimitPercent:F1}%)", ConsoleColor.Gray);
        }
        Console.WriteLine();
    }

    private static void WriteMagicNumberRule(ScanResult result, string rootPath, ReportOptions options)
    {
        var violations = MagicNumberFileInfo.Compute(result, options, rootPath);
        if (violations.Count <= 0)
            return;

        Console.WriteLine(new string('\u2500', 60));
        var highMN = violations.Count(v => v.Severity == Severity.High);
        var medMN = violations.Count(v => v.Severity == Severity.Medium);
        var lowMN = violations.Count(v => v.Severity == Severity.Low);
        var totalViolations = violations.Sum(v => v.Violations.Count);
        Write($"Magic Numbers ({totalViolations} in {violations.Count} file(s))", ConsoleColor.White);
        WriteSeverityCounts(highMN, medMN, lowMN);
        Console.WriteLine();
        foreach (var file in violations)
        {
            var relativePath = Path.GetRelativePath(rootPath, file.FilePath);
            var severityColor = file.Severity switch
            {
                Severity.High => ConsoleColor.Red,
                Severity.Medium => ConsoleColor.Yellow,
                _ => ConsoleColor.Green
            };
            Write("  ", ConsoleColor.DarkGray);
            Write($"[{file.Severity}] ", severityColor);
            Write($"{relativePath}", ConsoleColor.Cyan);
            WriteLine($"  {file.Violations.Count} magic number(s) (limit: {file.Limit})", ConsoleColor.Gray);
            foreach (var v in file.Violations)
            {
                Write("      ", ConsoleColor.DarkGray);
                Write($"Line {v.LineNumber}: ", ConsoleColor.Yellow);
                WriteLine(v.Value, ConsoleColor.Gray);
            }
        }
        Console.WriteLine();
    }

    private static void WriteMagicStringRule(ScanResult result, string rootPath, ReportOptions options)
    {
        var violations = MagicStringFileInfo.Compute(result, options, rootPath);
        if (violations.Count <= 0)
            return;

        Console.WriteLine(new string('\u2500', 60));
        var highMS = violations.Count(v => v.Severity == Severity.High);
        var medMS = violations.Count(v => v.Severity == Severity.Medium);
        var lowMS = violations.Count(v => v.Severity == Severity.Low);
        var totalViolations = violations.Sum(v => v.Violations.Count);
        Write($"Magic Strings ({totalViolations} in {violations.Count} file(s))", ConsoleColor.White);
        WriteSeverityCounts(highMS, medMS, lowMS);
        Console.WriteLine();
        foreach (var file in violations)
        {
            var relativePath = Path.GetRelativePath(rootPath, file.FilePath);
            var severityColor = file.Severity switch
            {
                Severity.High => ConsoleColor.Red,
                Severity.Medium => ConsoleColor.Yellow,
                _ => ConsoleColor.Green
            };
            Write("  ", ConsoleColor.DarkGray);
            Write($"[{file.Severity}] ", severityColor);
            Write($"{relativePath}", ConsoleColor.Cyan);
            WriteLine($"  {file.Violations.Count} magic string(s) (limit: {file.Limit})", ConsoleColor.Gray);
            foreach (var v in file.Violations)
            {
                Write("      ", ConsoleColor.DarkGray);
                Write($"Line {v.LineNumber}: ", ConsoleColor.Yellow);
                WriteLine($"\"{v.Value}\"", ConsoleColor.Gray);
            }
        }
        Console.WriteLine();
    }

    private static void WriteTechDebtScore(ScanResult result, string rootPath, ReportOptions options)
    {
        var score = TechDebtScore.Compute(result, options, rootPath);
        Console.WriteLine(new string('─', 60));
        var gradeColor = score.Grade switch
        {
            'A' or 'B' => ConsoleColor.Green,
            'C' => ConsoleColor.Yellow,
            _ => ConsoleColor.Red
        };
        Write("Tech Debt Score: ", ConsoleColor.White);
        Write($"{score.Score:F1}/100", gradeColor);
        Write("  Grade: ", ConsoleColor.White);
        WriteLine($"{score.Grade}", gradeColor);
        Console.WriteLine();
    }

    private static void WritePairs(string rootPath, ReportOptions options, List<FilePairGroup> pairs)
    {
        Console.WriteLine();
        Console.WriteLine(new string('─', 60));
        WriteLine("File Pairs", ConsoleColor.White);
        Console.WriteLine();

        foreach (var pair in pairs)
        {
            var relA = Path.GetRelativePath(rootPath, pair.FileA);
            var relB = Path.GetRelativePath(rootPath, pair.FileB);
            Write("  ", ConsoleColor.DarkGray);
            Write($"{relA}", ConsoleColor.Cyan);
            Write(" ↔ ", ConsoleColor.DarkGray);
            Write($"{relB}", ConsoleColor.Cyan);
            WriteLine($"  {pair.SharedBlocks.Count} shared block(s), {pair.SharedLineCount} line(s)", ConsoleColor.Gray);

            if (options.Verbosity == Verbosity.Detailed)
            {
                foreach (var block in pair.SharedBlocks)
                {
                    var locA = block.Locations.FirstOrDefault(l => l.FilePath == pair.FileA);
                    var locB = block.Locations.FirstOrDefault(l => l.FilePath == pair.FileB);
                    Write("    ", ConsoleColor.DarkGray);
                    Write($"Block ({block.Lines.Length} lines): ", ConsoleColor.Yellow);
                    if (locA is not null) Write($"{relA}:{locA.StartLine}-{locA.EndLine}", ConsoleColor.Cyan);
                    Write(" ↔ ", ConsoleColor.DarkGray);
                    if (locB is not null) Write($"{relB}:{locB.StartLine}-{locB.EndLine}", ConsoleColor.Cyan);
                    Console.WriteLine();
                }
            }
        }
    }

    private static void WriteDuplicates(ScanResult result, string rootPath)
    {
        for (int i = 0; i < result.Duplicates.Count; i++)
        {
            var block = result.Duplicates[i];
            Console.WriteLine();
            WriteLine($"Block #{i + 1}  ({block.Lines.Length} lines, {block.Locations.Count} occurrences)", ConsoleColor.Yellow);
            Console.WriteLine();

            foreach (var line in block.Lines)
            {
                Write("  │ ", ConsoleColor.DarkGray);
                Console.WriteLine(Truncate(line, 120));
            }

            Console.WriteLine();

            foreach (var loc in block.Locations)
            {
                var relativePath = Path.GetRelativePath(rootPath, loc.FilePath);
                Write("  → ", ConsoleColor.DarkGray);
                WriteLine($"{relativePath}:{loc.StartLine}-{loc.EndLine}", ConsoleColor.Cyan);
            }

            if (i < result.Duplicates.Count - 1)
                Console.WriteLine(new string('─', 60));
        }
    }

    private static void PrintQuiet(SummaryStatistics stats)
    {
        if (stats.TotalDuplicateBlocks == 0)
        {
            Console.WriteLine("No duplicates found.");
            return;
        }

        Console.WriteLine($"{stats.TotalDuplicateBlocks} duplicate block(s), {stats.TotalDuplicatedLines} duplicated line(s)");
    }

    private static string Truncate(string text, int maxLength)
    {
        return text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
    }

    private static void Write(string text, ConsoleColor color)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = prev;
    }

    private static void WriteLine(string text, ConsoleColor color)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = prev;
    }

    private static void WriteSeverityCounts(int high, int medium, int low)
    {
        Write("  ", ConsoleColor.DarkGray);
        Write($"{high} HIGH", ConsoleColor.Red);
        Write("  ", ConsoleColor.DarkGray);
        Write($"{medium} MEDIUM", ConsoleColor.Yellow);
        Write("  ", ConsoleColor.DarkGray);
        WriteLine($"{low} LOW", ConsoleColor.Green);
    }
}
