using System.Net;

namespace PrepR;

public class HtmlReporter : IReporter
{
    public void Report(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var stats = SummaryStatistics.Compute(result);

        writer.WriteLine("<!DOCTYPE html>");
        writer.WriteLine("<html lang=\"en\">");
        writer.WriteLine("<head>");
        writer.WriteLine("<meta charset=\"UTF-8\">");
        writer.WriteLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        writer.WriteLine("<title>prepR — Duplicate Block Report</title>");
        writer.WriteLine("<style>");
        writer.WriteLine("""
            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 960px; margin: 0 auto; padding: 2rem; background: #1e1e2e; color: #cdd6f4; }
            h1 { color: #89b4fa; border-bottom: 2px solid #45475a; padding-bottom: 0.5rem; }
            h2 { color: #a6adc8; margin-top: 2rem; }
            .stats { background: #313244; padding: 1rem; border-radius: 8px; margin: 1rem 0; }
            .stats span { margin-right: 2rem; }
            .block { background: #313244; border: 1px solid #45475a; border-radius: 8px; margin: 1.5rem 0; overflow: hidden; }
            .block-header { background: #45475a; padding: 0.75rem 1rem; cursor: pointer; font-weight: bold; color: #f9e2af; }
            .block-header:hover { background: #585b70; }
            .block-body { padding: 1rem; display: none; }
            .block-body.open { display: block; }
            pre { background: #1e1e2e; padding: 1rem; border-radius: 4px; overflow-x: auto; margin: 0.5rem 0; }
            code { color: #a6e3a1; }
            .location { color: #89dceb; margin: 0.25rem 0; }
            table { width: 100%; border-collapse: collapse; margin: 1rem 0; }
            th { background: #45475a; padding: 0.5rem; text-align: left; }
            td { padding: 0.5rem; border-bottom: 1px solid #45475a; }
            .none { color: #a6e3a1; font-style: italic; }
            .total { margin-top: 1rem; font-weight: bold; color: #89b4fa; }
            .severity-high { color: #f38ba8; font-weight: bold; }
            .severity-medium { color: #f9e2af; font-weight: bold; }
            .severity-low { color: #a6e3a1; }
            .pair-detail { margin-left: 2rem; color: #89dceb; font-size: 0.9em; }
        """);
        writer.WriteLine("</style>");
        writer.WriteLine("</head>");
        writer.WriteLine("<body>");

        writer.WriteLine("<h1>prepR — Duplicate Block Report</h1>");
        writer.WriteLine("<div class=\"stats\">");
        writer.WriteLine($"<span><strong>Files scanned:</strong> {result.TotalFilesScanned}</span>");
        writer.WriteLine($"<span><strong>Total lines:</strong> {result.TotalLinesScanned}</span>");
        writer.WriteLine($"<span><strong>Duplicate blocks:</strong> {stats.TotalDuplicateBlocks}</span>");
        writer.WriteLine($"<span><strong>Duplicated lines:</strong> {stats.TotalDuplicatedLines}</span>");
        if (stats.MostDuplicatedFile is not null)
        {
            var relMost = WebUtility.HtmlEncode(Path.GetRelativePath(rootPath, stats.MostDuplicatedFile));
            writer.WriteLine($"<span><strong>Most duplicated:</strong> {relMost} ({stats.MostDuplicatedFileBlockCount} block(s))</span>");
        }
        writer.WriteLine("</div>");

        if (result.Duplicates.Count == 0)
        {
            writer.WriteLine("<p class=\"none\">No duplicate blocks found.</p>");
            WriteFooter(writer);
            return;
        }

        if (options.Verbosity != Verbosity.Quiet)
        {
            writer.WriteLine("<h2>Duplicate Blocks</h2>");

            for (int i = 0; i < result.Duplicates.Count; i++)
            {
                var block = result.Duplicates[i];
                writer.WriteLine("<div class=\"block\">");
                writer.WriteLine($"<div class=\"block-header\" onclick=\"this.nextElementSibling.classList.toggle('open')\">");
                writer.WriteLine($"Block #{i + 1} ({block.Lines.Length} lines, {block.Locations.Count} occurrences)");
                writer.WriteLine("</div>");
                writer.WriteLine("<div class=\"block-body\">");
                writer.WriteLine("<pre><code>");
                foreach (var line in block.Lines)
                    writer.WriteLine(WebUtility.HtmlEncode(line));
                writer.WriteLine("</code></pre>");

                writer.WriteLine("<p><strong>Locations:</strong></p>");
                foreach (var loc in block.Locations)
                {
                    var relativePath = Path.GetRelativePath(rootPath, loc.FilePath);
                    writer.WriteLine($"<div class=\"location\">{WebUtility.HtmlEncode(relativePath)}:{loc.StartLine}-{loc.EndLine}</div>");
                }

                writer.WriteLine("</div>");
                writer.WriteLine("</div>");
            }
        }

        // Per-file summary with severity
        writer.WriteLine("<h2>Per-file Summary</h2>");
        writer.WriteLine("<table>");
        writer.WriteLine("<thead><tr><th>File</th><th>Blocks</th><th>Duplicated Lines</th><th>Duplication %</th><th>Severity</th></tr></thead>");
        writer.WriteLine("<tbody>");

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

        writer.WriteLine("</tbody>");
        writer.WriteLine("</table>");

        // File pairs
        var pairs = FilePairGroup.ComputeFilePairs(result);
        if (pairs.Count > 0)
        {
            writer.WriteLine("<h2>File Pairs</h2>");
            writer.WriteLine("<table>");
            writer.WriteLine("<thead><tr><th>File A</th><th>File B</th><th>Shared Blocks</th><th>Shared Lines</th></tr></thead>");
            writer.WriteLine("<tbody>");

            foreach (var pair in pairs)
            {
                var relA = WebUtility.HtmlEncode(Path.GetRelativePath(rootPath, pair.FileA));
                var relB = WebUtility.HtmlEncode(Path.GetRelativePath(rootPath, pair.FileB));
                writer.WriteLine($"<tr><td>{relA}</td><td>{relB}</td><td>{pair.SharedBlocks.Count}</td><td>{pair.SharedLineCount}</td></tr>");

                if (options.Verbosity == Verbosity.Detailed)
                {
                    foreach (var block in pair.SharedBlocks)
                    {
                        var locA = block.Locations.FirstOrDefault(l => l.FilePath == pair.FileA);
                        var locB = block.Locations.FirstOrDefault(l => l.FilePath == pair.FileB);
                        var detail = $"{relA}:{locA?.StartLine}-{locA?.EndLine} ↔ {relB}:{locB?.StartLine}-{locB?.EndLine}";
                        writer.WriteLine($"<tr><td colspan=\"4\" class=\"pair-detail\">Block ({block.Lines.Length} lines): {detail}</td></tr>");
                    }
                }
            }

            writer.WriteLine("</tbody>");
            writer.WriteLine("</table>");
        }

        writer.WriteLine($"<p class=\"total\">Total: {stats.TotalDuplicateBlocks} duplicate block(s) across {result.TotalFilesScanned} files</p>");

        // Files exceeding line limit
        if (options.LineLimitRule is not null)
        {
            var overLimit = OverLimitFileInfo.Compute(result, options.LineLimitRule, rootPath);
            if (overLimit.Count > 0)
            {
                writer.WriteLine("<h2>Files Exceeding Line Limit</h2>");
                writer.WriteLine("<table>");
                writer.WriteLine("<thead><tr><th>File</th><th>Lines</th><th>Limit</th></tr></thead>");
                writer.WriteLine("<tbody>");
                foreach (var v in overLimit)
                {
                    var relativePath = WebUtility.HtmlEncode(Path.GetRelativePath(rootPath, v.FilePath));
                    writer.WriteLine($"<tr><td>{relativePath}</td><td class=\"severity-high\">{v.LineCount}</td><td>{v.Limit}</td></tr>");
                }
                writer.WriteLine("</tbody>");
                writer.WriteLine("</table>");
            }
        }

        WriteFooter(writer);
    }

    private static void WriteFooter(TextWriter writer)
    {
        writer.WriteLine("</body>");
        writer.WriteLine("</html>");
    }
}
