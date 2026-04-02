using static Prepr.Reporters.HtmlReporterStyles;

namespace Prepr.Reporters;

public class HtmlReporter : IReporter
{
    public string FileExtension => ".html";

    public void Report(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var stats = SummaryStatistics.Compute(result);
        var techDebt = TechDebtScore.Compute(result, options, rootPath);
        var (gradeColor, gradeBg) = techDebt.Grade switch
        {
            'A' or 'B' => ("text-secondary", "bg-secondary/10 border-secondary/20"),
            'C' => ("text-orange-400", "bg-orange-500/10 border-orange-500/20"),
            _ => ("text-error", "bg-error-container/20 border-error/20")
        };

        writer.WriteLine($"""
            <!DOCTYPE html>
            <html class="dark" lang="en">
            {HEAD}
            <body class="bg-background text-on-surface selection:bg-primary/30 min-h-screen">
            <main class="max-w-6xl mx-auto p-8 md:p-12 space-y-12">
            <header class="border-b border-outline-variant/30 pb-8">
            <h1 class="font-headline text-4xl font-bold tracking-tight text-primary">prepr report</h1>
            </header>
            <div class="space-y-4">
            <div class="grid grid-cols-3 md:grid-cols-3 gap-4">
            <div class="p-4 rounded-xl bg-surface-container-low border border-outline-variant/10">
            <p class="text-[10px] uppercase tracking-widest text-on-surface-variant mb-1">Files scanned</p>
            <p class="text-2xl font-headline font-bold">{result.TotalFilesScanned}</p>
            </div>
            <div class="p-4 rounded-xl bg-surface-container-low border border-outline-variant/10">
            <p class="text-[10px] uppercase tracking-widest text-on-surface-variant mb-1">Total lines</p>
            <p class="text-2xl font-headline font-bold">{result.TotalLinesScanned}</p>
            </div>
            <div class="p-4 rounded-xl bg-surface-container-low border border-outline-variant/10">
            <p class="text-[10px] uppercase tracking-widest text-on-surface-variant mb-1">Tech Debt Score</p>
            <p class="text-2xl font-headline font-bold {gradeColor}">{techDebt.Score:F1}/100 <span class="text-sm {gradeBg} px-2 py-0.5 rounded border">{techDebt.Grade}</span></p>
            </div>
            </div>
            </div>
            """);

        WriteDuplicationSection(result, rootPath, writer, options, stats);

        HtmlRuleSectionWriter.WriteLineLimitRule(result, rootPath, writer, options);
        HtmlRuleSectionWriter.WriteIndentationRule(result, rootPath, writer, options);
        HtmlRuleSectionWriter.WriteEarlyReturnRule(result, writer, rootPath, options);
        HtmlRuleSectionWriter.WriteCommentDensityRule(result, rootPath, writer, options);
        HtmlRuleSectionWriter.WriteMagicNumberRule(result, rootPath, writer, options);
        HtmlRuleSectionWriter.WriteMagicStringRule(result, rootPath, writer, options);

        writer.Write(FOOTER);
    }

    private static void WriteDuplicationSection(ScanResult result, string rootPath, TextWriter writer, ReportOptions options, SummaryStatistics stats)
    {
        var fileInfos = DuplicationFileInfo.ComputePerFile(result, options);
        var highCount = fileInfos.Count(f => f.Severity == Severity.High);
        var mediumCount = fileInfos.Count(f => f.Severity == Severity.Medium);
        var lowCount = fileInfos.Count(f => f.Severity == Severity.Low);

        writer.Write($"""
            <details class="group border border-outline-variant/20 rounded-xl bg-surface-container-low overflow-hidden">
            <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-surface-container-high/30 transition-colors flex-row">
            <div class="flex items-center gap-4">
            <div class="h-8 w-1 bg-primary"></div>
            <h2 class="font-headline text-3xl font-bold text-on-surface uppercase tracking-tight">Code Duplication</h2>
            <span class="px-2 py-0.5 rounded text-[10px] font-bold bg-error-container/20 text-error border border-error/20">{highCount} HIGH</span>
            <span class="px-2 py-0.5 rounded text-[10px] font-bold bg-orange-500/10 text-orange-400 border border-orange-500/20">{mediumCount} MEDIUM</span>
            <span class="px-2 py-0.5 rounded text-[10px] font-bold bg-secondary/10 text-secondary border border-secondary/20">{lowCount} LOW</span>
            </div><span class="material-symbols-outlined transition-transform group-open:rotate-180">expand_more</span>
            </summary>
            <div class="p-8 pt-0 space-y-12">
            """);

        if (result.Duplicates.Count == 0)
        {
            writer.WriteLine("""<p class="text-secondary italic">No duplicate blocks found.</p>""");
        }
        else
        {
            HtmlDuplicationWriter.WritePerFileSummary(result, rootPath, writer, fileInfos);

            var pairs = FilePairGroup.ComputeFilePairs(result);
            if (pairs.Count > 0)
                HtmlDuplicationWriter.WriteFilePairs(rootPath, writer, pairs);

            writer.WriteLine($"""<p class="text-sm font-bold text-primary">Total: {stats.TotalDuplicateBlocks} duplicate block(s) across {result.TotalFilesScanned} files</p>""");
        }

        writer.Write("""
            </div>
            </details>
            """);
    }
}
