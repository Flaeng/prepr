using System.Net;

using static Prepr.Reporters.HtmlReporterStyles;

namespace Prepr.Reporters;

public class HtmlReporter : IReporter
{
    public string FileExtension => ".html";

    public void Report(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var stats = SummaryStatistics.Compute(result);

        writer.WriteLine($"""
            <!DOCTYPE html>
            <html lang="en">
            {HEAD}
            <body>
                <h1>prepr — Duplicate Block Report</h1>
                <div class="stats">
                <span><strong>Files scanned:</strong> {result.TotalFilesScanned}</span>
                <span><strong>Total lines:</strong> {result.TotalLinesScanned}</span>
                <span><strong>Duplicate blocks:</strong> {stats.TotalDuplicateBlocks}</span>
                <span><strong>Duplicated lines:</strong> {stats.TotalDuplicatedLines}</span>
            """);

        if (stats.MostDuplicatedFile is not null)
        {
            var relMost = WebUtility.HtmlEncode(Path.GetRelativePath(rootPath, stats.MostDuplicatedFile));
            writer.WriteLine($"<span><strong>Most duplicated:</strong> {relMost} ({stats.MostDuplicatedFileBlockCount} block(s))</span>");
        }
        writer.WriteLine("</div>");

        if (result.Duplicates.Count == 0)
        {
            writer.WriteLine("<p class=\"none\">No duplicate blocks found.</p>");
            writer.Write(FOOTER);
            return;
        }

        WriteDuplicates(result, rootPath, writer);

        // Per-file summary with severity
        writer.Write("""
            <h2>Per-file Summary</h2>
            <table>
            <thead><tr><th>File</th><th>Blocks</th><th>Duplicated Lines</th><th>Duplication %</th><th>Severity</th></tr></thead>
            <tbody>
        """);

        var fileInfos = FileDuplicationInfo.ComputePerFile(result, options);
        foreach (var info in fileInfos)
        {
            var relativePath = Path.GetRelativePath(rootPath, info.FilePath);
            var severityClass = info.Severity switch
            {
                Severity.High => "severity-high",
                Severity.Medium => "severity-medium",
                _ => "severity-low"
            };
            writer.WriteLine($"<tr><td>{WebUtility.HtmlEncode(relativePath)}</td><td>{info.DuplicateBlockCount}</td><td>{info.DuplicatedLineCount}</td><td>{info.DuplicationPercentage:F1}%</td><td class=\"{severityClass}\">{info.Severity}</td></tr>");
        }

        writer.Write("""
            </tbody>
            </table>
        """);

        // File pairs
        var pairs = FilePairGroup.ComputeFilePairs(result);
        if (pairs.Count > 0)
        {
            WriteDuplicatePairs(rootPath, writer, pairs);
        }

        writer.WriteLine($"<p class=\"total\">Total: {stats.TotalDuplicateBlocks} duplicate block(s) across {result.TotalFilesScanned} files</p>");

        // Files exceeding line limit
        WriteLineLimitRule(result, rootPath, writer, options);

        writer.Write(FOOTER);
    }

    private static void WriteDuplicates(ScanResult result, string rootPath, TextWriter writer)
    {
        writer.WriteLine("<h2>Duplicate Blocks</h2>");

        for (int i = 0; i < result.Duplicates.Count; i++)
        {
            var block = result.Duplicates[i];
            writer.WriteLine("<div class=\"block\">");
            writer.WriteLine($"<div class=\"block-header\" onclick=\"this.nextElementSibling.classList.toggle('open')\">");
            writer.WriteLine($"Block #{i + 1} ({block.Lines.Length} lines, {block.Locations.Count} occurrences)");
            writer.Write("""
                </div>
                <div class="block-body">
                <pre><code>
            """);
            foreach (var line in block.Lines)
                writer.WriteLine(WebUtility.HtmlEncode(line));
            writer.WriteLine("</code></pre>");

            writer.WriteLine("<p><strong>Locations:</strong></p>");
            foreach (var loc in block.Locations)
            {
                var relativePath = Path.GetRelativePath(rootPath, loc.FilePath);
                writer.WriteLine($"<div class=\"location\">{WebUtility.HtmlEncode(relativePath)}:{loc.StartLine}-{loc.EndLine}</div>");
            }

            writer.Write("""
                </div>
                </div>
            """);
        }
    }

    private static void WriteDuplicatePairs(string rootPath, TextWriter writer, List<FilePairGroup> pairs)
    {
        writer.Write("""
                <h2>File Pairs</h2>
                <table>
                <thead><tr><th>File A</th><th>File B</th><th>Shared Blocks</th><th>Shared Lines</th></tr></thead>
                <tbody>
            """);

        foreach (var pair in pairs)
        {
            var relA = WebUtility.HtmlEncode(Path.GetRelativePath(rootPath, pair.FileA));
            var relB = WebUtility.HtmlEncode(Path.GetRelativePath(rootPath, pair.FileB));
            writer.WriteLine($"<tr><td>{relA}</td><td>{relB}</td><td>{pair.SharedBlocks.Count}</td><td>{pair.SharedLineCount}</td></tr>");

            foreach (var block in pair.SharedBlocks)
            {
                var locA = block.Locations.FirstOrDefault(l => l.FilePath == pair.FileA);
                var locB = block.Locations.FirstOrDefault(l => l.FilePath == pair.FileB);
                var detail = $"{relA}:{locA?.StartLine}-{locA?.EndLine} ↔ {relB}:{locB?.StartLine}-{locB?.EndLine}";
                writer.WriteLine($"<tr><td colspan=\"4\" class=\"pair-detail\">Block ({block.Lines.Length} lines): {detail}</td></tr>");
            }
        }

        writer.Write("""
                </tbody>
                </table>
            """);
    }

    private static void WriteLineLimitRule(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var overLimit = OverLimitFileInfo.Compute(result, options, rootPath);
        if (overLimit.Count > 0)
        {
            writer.Write("""
                    <h2>Files Exceeding Line Limit</h2>
                    <table>
                    <thead><tr><th>File</th><th>Lines</th><th>Limit</th></tr></thead>
                    <tbody>
                """);
            foreach (var v in overLimit)
            {
                var relativePath = WebUtility.HtmlEncode(Path.GetRelativePath(rootPath, v.FilePath));
                writer.WriteLine($"<tr><td>{relativePath}</td><td class=\"severity-high\">{v.LineCount}</td><td>{v.Limit}</td></tr>");
            }
            writer.Write("""
                    </tbody>
                    </table>
                """);
        }
    }
}
