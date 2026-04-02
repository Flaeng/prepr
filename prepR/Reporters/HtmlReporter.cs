using System.Net;

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

        if (result.Duplicates.Count == 0)
        {
            writer.WriteLine("""<p class="text-secondary italic mt-8">No duplicate blocks found.</p>""");
            writer.Write(FOOTER);
            return;
        }

        // Code Duplication section
        var fileInfos = DuplicationFileInfo.ComputePerFile(result, options);
        var highCount = fileInfos.Count(f => f.Severity == Severity.High);
        var mediumCount = fileInfos.Count(f => f.Severity == Severity.Medium);
        var lowCount = fileInfos.Count(f => f.Severity == Severity.Low);

        writer.Write("""
            <details class="group border border-outline-variant/20 rounded-xl bg-surface-container-low overflow-hidden">
            <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-surface-container-high/30 transition-colors flex-row">
            <div class="flex items-center gap-4">
            <div class="h-8 w-1 bg-primary"></div>
            <h2 class="font-headline text-3xl font-bold text-on-surface uppercase tracking-tight">Code Duplication</h2>
            </div>
            <div class="flex items-center gap-2 mr-4">
            """);
        writer.Write($"""<span class="px-2 py-0.5 rounded text-[10px] font-bold bg-error-container/20 text-error border border-error/20">{highCount} HIGH</span>""");
        writer.Write($"""<span class="px-2 py-0.5 rounded text-[10px] font-bold bg-orange-500/10 text-orange-400 border border-orange-500/20">{mediumCount} MEDIUM</span>""");
        writer.Write($"""<span class="px-2 py-0.5 rounded text-[10px] font-bold bg-secondary/10 text-secondary border border-secondary/20">{lowCount} LOW</span>""");
        writer.Write("""
            </div><span class="material-symbols-outlined transition-transform group-open:rotate-180">expand_more</span>
            </summary>
            <div class="p-8 pt-0 space-y-12">
            """);

        // Per-file Summary
        WritePerFileSummary(result, rootPath, writer, fileInfos);

        // File Pairs
        var pairs = FilePairGroup.ComputeFilePairs(result);
        if (pairs.Count > 0)
            WriteDuplicatePairs(rootPath, writer, pairs);

        writer.WriteLine($"""<p class="text-sm font-bold text-primary">Total: {stats.TotalDuplicateBlocks} duplicate block(s) across {result.TotalFilesScanned} files</p>""");

        // Duplicate Blocks
        WriteDuplicates(result, rootPath, writer);

        writer.Write("""
            </div>
            </details>
            """);

        // Files exceeding line limit
        WriteLineLimitRule(result, rootPath, writer, options);

        // Files exceeding indentation limit
        WriteIndentationRule(result, rootPath, writer, options);

        // Early return violations
        WriteEarlyReturnRule(result, writer, rootPath, options);

        writer.Write(FOOTER);
    }

    private static void WritePerFileSummary(ScanResult result, string rootPath, TextWriter writer, List<DuplicationFileInfo> fileInfos)
    {
        writer.Write("""
            <div class="space-y-6">
            <h3 class="font-headline text-xl font-bold text-on-surface-variant/80">Per-file Summary</h3>
            <div class="rounded-xl overflow-hidden border border-outline-variant/20 bg-surface-container">
            <table class="w-full text-left text-sm">
            <thead>
            <tr class="bg-surface-container-high/50 text-on-surface-variant font-label uppercase text-[10px] tracking-widest border-b border-outline-variant/20">
            <th class="px-6 py-4">File</th>
            <th class="px-6 py-4">Blocks</th>
            <th class="px-6 py-4">Duplicated Lines</th>
            <th class="px-6 py-4">Duplication %</th>
            <th class="px-6 py-4">Severity</th>
            <th class="px-6 py-4 text-right">Actions</th>
            </tr>
            </thead>
            <tbody class="divide-y divide-outline-variant/10">
            """);

        for (int fi = 0; fi < fileInfos.Count; fi++)
        {
            var info = fileInfos[fi];
            var relativePath = Path.GetRelativePath(rootPath, info.FilePath);
            var (badgeClass, label) = info.Severity switch
            {
                Severity.High => ("bg-error-container/20 text-error", "HIGH"),
                Severity.Medium => ("bg-orange-500/10 text-orange-400", "MEDIUM"),
                _ => ("bg-secondary/10 text-secondary", "LOW")
            };
            var prompt = WebUtility.HtmlEncode(info.GetPrompt(relativePath, result.Duplicates, rootPath));
            var codeId = $"dup-code-{fi}";
            writer.Write($"""
                <tr class="hover:bg-primary/5 transition-colors">
                <td class="px-6 py-4 font-mono text-xs text-primary">{WebUtility.HtmlEncode(relativePath)}</td>
                <td class="px-6 py-4">{info.DuplicateBlockCount}</td>
                <td class="px-6 py-4">{info.DuplicatedLineCount}</td>
                <td class="px-6 py-4">{info.DuplicationPercentage:F1}%</td>
                <td class="px-6 py-4"><span class="px-2 py-0.5 rounded text-[10px] font-bold {badgeClass}">{label}</span></td>
                <td class="px-6 py-4 text-right whitespace-nowrap"><button onclick="document.getElementById('{codeId}').classList.toggle('hidden')" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors mr-1" style="cursor:pointer">Show code</button><button data-prompt="{prompt}" onclick="showPromptModal(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors mr-1" style="cursor:pointer">Show prompt</button><button data-prompt="{prompt}" onclick="copyPrompt(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors" style="cursor:pointer">Copy prompt</button></td>
                </tr>
                """);

            var fileBlocks = result.Duplicates
                .Select((block, idx) => (block, idx))
                .Where(x => x.block.Locations.Any(l => l.FilePath == info.FilePath))
                .ToList();

            if (fileBlocks.Count > 0)
            {
                writer.Write($"""<tr id="{codeId}" class="hidden"><td colspan="6" class="px-6 py-3"><div class="space-y-3">""");
                foreach (var (block, idx) in fileBlocks)
                {
                    var loc = block.Locations.First(l => l.FilePath == info.FilePath);
                    writer.Write($"""<div class="text-[10px] font-bold uppercase tracking-widest text-on-surface-variant mt-2">Block #{idx + 1} — Lines {loc.StartLine}-{loc.EndLine} ({block.Lines.Length} lines)</div>""");
                    writer.Write("""<pre class="code-block p-4 rounded-lg font-mono text-xs text-secondary leading-relaxed overflow-x-auto border border-outline-variant/10"><code>""");
                    for (int li = 0; li < block.Lines.Length; li++)
                        writer.WriteLine($"<span><span class=\"text-on-surface-variant/50\">{loc.StartLine + li,4} │ </span>{WebUtility.HtmlEncode(block.Lines[li])}</span>");
                    writer.Write("</code></pre>");
                }
                writer.Write("</div></td></tr>");
            }
        }

        writer.Write("""
            </tbody>
            </table>
            </div>
            </div>
            """);
    }

    private static void WriteDuplicates(ScanResult result, string rootPath, TextWriter writer)
    {
        writer.Write("""
            <div class="space-y-6">
            <h3 class="font-headline text-xl font-bold text-on-surface-variant/80">Duplicate Blocks</h3>
            <div class="space-y-4">
            """);

        for (int i = 0; i < result.Duplicates.Count; i++)
        {
            var block = result.Duplicates[i];
            var contentId = $"block{i + 1}-content";
            writer.Write($"""
                <div class="rounded-xl overflow-hidden bg-surface-container border border-outline-variant/20">
                <div class="p-4 flex items-center justify-between bg-surface-container-high/50 cursor-pointer hover:bg-surface-container-high transition-colors" onclick="document.getElementById('{contentId}').classList.toggle('hidden')">
                <span class="text-sm font-bold text-on-surface">Block #{i + 1} ({block.Lines.Length} lines, {block.Locations.Count} occurrences)</span>
                <span class="material-symbols-outlined text-on-surface-variant">expand_more</span>
                </div>
                <div class="hidden p-6 space-y-4" id="{contentId}">
                <pre class="code-block p-4 rounded-lg font-mono text-xs text-secondary leading-relaxed overflow-x-auto border border-outline-variant/10"><code>
                """);
            foreach (var line in block.Lines)
                writer.WriteLine(WebUtility.HtmlEncode(line));
            writer.Write("""
                </code></pre>
                <div class="space-y-1">
                <p class="text-[10px] font-bold uppercase tracking-widest text-on-surface-variant">Locations:</p>
                """);
            foreach (var loc in block.Locations)
            {
                var relativePath = WebUtility.HtmlEncode(Path.GetRelativePath(rootPath, loc.FilePath));
                writer.WriteLine($"""<div class="text-xs font-mono text-primary">{relativePath}:{loc.StartLine}-{loc.EndLine}</div>""");
            }
            writer.Write("""
                </div>
                </div>
                </div>
                """);
        }

        writer.Write("""
            </div>
            </div>
            """);
    }

    private static void WriteDuplicatePairs(string rootPath, TextWriter writer, List<FilePairGroup> pairs)
    {
        writer.Write("""
            <div class="space-y-6">
            <h3 class="font-headline text-xl font-bold text-on-surface-variant/80">File Pairs</h3>
            <div class="rounded-xl overflow-hidden border border-outline-variant/20 bg-surface-container">
            <table class="w-full text-left text-sm">
            <thead>
            <tr class="bg-surface-container-high/50 text-on-surface-variant font-label uppercase text-[10px] tracking-widest border-b border-outline-variant/20">
            <th class="px-6 py-4">File A</th>
            <th class="px-6 py-4">File B</th>
            <th class="px-6 py-4">Shared Blocks</th>
            <th class="px-6 py-4">Shared Lines</th>
            <th class="px-6 py-4 text-right">Actions</th>
            </tr>
            </thead>
            <tbody class="divide-y divide-outline-variant/10">
            """);

        for (int pi = 0; pi < pairs.Count; pi++)
        {
            var pair = pairs[pi];
            var relA = WebUtility.HtmlEncode(Path.GetRelativePath(rootPath, pair.FileA));
            var relB = WebUtility.HtmlEncode(Path.GetRelativePath(rootPath, pair.FileB));
            var codeId = $"pair-code-{pi}";
            writer.WriteLine($"""<tr><td class="px-6 py-3 font-mono text-xs text-primary">{relA}</td><td class="px-6 py-3 font-mono text-xs text-primary">{relB}</td><td class="px-6 py-3">{pair.SharedBlocks.Count}</td><td class="px-6 py-3">{pair.SharedLineCount}</td><td class="px-6 py-3 text-right"><button onclick="document.getElementById('{codeId}').classList.toggle('hidden')" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors" style="cursor:pointer">Show code</button></td></tr>""");

            writer.Write($"""<tr id="{codeId}" class="hidden"><td class="px-6 py-3" colspan="5"><div class="space-y-3">""");
            foreach (var block in pair.SharedBlocks)
            {
                var locA = block.Locations.FirstOrDefault(l => l.FilePath == pair.FileA);
                var locB = block.Locations.FirstOrDefault(l => l.FilePath == pair.FileB);
                writer.Write($"""<div class="text-[10px] font-bold uppercase tracking-widest text-on-surface-variant mt-2">Shared block ({block.Lines.Length} lines): {relA}:{locA?.StartLine}-{locA?.EndLine} ↔ {relB}:{locB?.StartLine}-{locB?.EndLine}</div>""");
                writer.Write("""<pre class="code-block p-4 rounded-lg font-mono text-xs text-secondary leading-relaxed overflow-x-auto border border-outline-variant/10"><code>""");
                var startLine = locA?.StartLine ?? 1;
                for (int li = 0; li < block.Lines.Length; li++)
                    writer.WriteLine($"<span><span class=\"text-on-surface-variant/50\">{startLine + li,4} │ </span>{WebUtility.HtmlEncode(block.Lines[li])}</span>");
                writer.Write("</code></pre>");
            }
            writer.Write("</div></td></tr>");
        }

        writer.Write("""
            </tbody>
            </table>
            </div>
            </div>
            """);
    }

    private static void WriteLineLimitRule(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var overLimit = OverLimitFileInfo.Compute(result, options, rootPath);
        if (overLimit.Count == 0) return;

        writer.Write($"""
            <details class="group border border-outline-variant/20 rounded-xl bg-surface-container-low overflow-hidden">
            <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-surface-container-high/30 transition-colors">
            <div class="flex items-center gap-4">
            <div class="h-8 w-1 bg-tertiary"></div>
            <h2 class="font-headline text-3xl font-bold text-on-surface uppercase tracking-tight">Line Count Overage</h2>
            <span class="ml-4 px-2 py-0.5 rounded text-[10px] font-bold bg-tertiary/10 text-tertiary border border-tertiary/20">{overLimit.Count} VIOLATIONS</span>
            </div>
            <span class="material-symbols-outlined transition-transform group-open:rotate-180">expand_more</span>
            </summary>
            <div class="p-8 pt-0">
            <div class="rounded-xl overflow-hidden border border-outline-variant/20 bg-surface-container">
            <table class="w-full text-left text-sm">
            <thead>
            <tr class="bg-surface-container-high/50 text-on-surface-variant font-label uppercase text-[10px] tracking-widest border-b border-outline-variant/20">
            <th class="px-6 py-3">File</th>
            <th class="px-6 py-3 text-right">Lines</th>
            <th class="px-6 py-3 text-right">Limit</th>
            <th class="px-6 py-3 text-right">Actions</th>
            </tr>
            </thead>
            <tbody class="divide-y divide-outline-variant/10">
            """);

        foreach (var v in overLimit)
        {
            var relativePath = Path.GetRelativePath(rootPath, v.FilePath);
            var prompt = WebUtility.HtmlEncode(v.GetPrompt(relativePath));
            writer.WriteLine($"""<tr><td class="px-6 py-4 font-mono text-xs text-primary">{WebUtility.HtmlEncode(relativePath)}</td><td class="px-6 py-4 text-right font-bold text-tertiary">{v.LineCount}</td><td class="px-6 py-4 text-right text-on-surface-variant">{v.Limit}</td><td class="px-6 py-4 text-right whitespace-nowrap"><button data-prompt="{prompt}" onclick="showPromptModal(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-tertiary/10 text-tertiary border border-tertiary/20 hover:bg-tertiary/20 transition-colors mr-1" style="cursor:pointer">Show prompt</button><button data-prompt="{prompt}" onclick="copyPrompt(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-tertiary/10 text-tertiary border border-tertiary/20 hover:bg-tertiary/20 transition-colors" style="cursor:pointer">Copy prompt</button></td></tr>""");
        }

        writer.Write("""
            </tbody>
            </table>
            </div>
            </div>
            </details>
            """);
    }

    private static void WriteIndentationRule(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var overIndented = OverIndentedFileInfo.Compute(result, options, rootPath);
        if (overIndented.Count == 0) return;

        writer.Write($"""
            <details class="group border border-outline-variant/20 rounded-xl bg-surface-container-low overflow-hidden">
            <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-surface-container-high/30 transition-colors">
            <div class="flex items-center gap-4">
            <div class="h-8 w-1 bg-secondary"></div>
            <h2 class="font-headline text-3xl font-bold text-on-surface uppercase tracking-tight">Indentation Overage</h2>
            <span class="ml-4 px-2 py-0.5 rounded text-[10px] font-bold bg-secondary/10 text-secondary border border-secondary/20">{overIndented.Count} VIOLATIONS</span>
            </div>
            <span class="material-symbols-outlined transition-transform group-open:rotate-180">expand_more</span>
            </summary>
            <div class="p-8 pt-0">
            <div class="rounded-xl overflow-hidden border border-outline-variant/20 bg-surface-container">
            <table class="w-full text-left text-sm">
            <thead>
            <tr class="bg-surface-container-high/50 text-on-surface-variant font-label uppercase text-[10px] tracking-widest border-b border-outline-variant/20">
            <th class="px-6 py-3">File</th>
            <th class="px-6 py-3 text-right">Depth</th>
            <th class="px-6 py-3 text-right">Line</th>
            <th class="px-6 py-3 text-right">Limit</th>
            <th class="px-6 py-3 text-right">Actions</th>
            </tr>
            </thead>
            <tbody class="divide-y divide-outline-variant/10">
            """);

        for (int i = 0; i < overIndented.Count; i++)
        {
            var v = overIndented[i];
            var relativePath = Path.GetRelativePath(rootPath, v.FilePath);
            var prompt = WebUtility.HtmlEncode(v.GetPrompt(relativePath));
            var codeId = $"indent-code-{i}";
            writer.WriteLine($"""<tr><td class="px-6 py-3 font-mono text-[11px] text-primary">{WebUtility.HtmlEncode(relativePath)}</td><td class="px-6 py-3 text-right font-bold text-tertiary">{v.MaxDepth}</td><td class="px-6 py-3 text-right text-on-surface-variant">{v.LineNumber}</td><td class="px-6 py-3 text-right text-on-surface-variant">{v.Limit}</td><td class="px-6 py-3 text-right whitespace-nowrap"><button onclick="document.getElementById('{codeId}').classList.toggle('hidden')" class="px-2 py-1 rounded text-[10px] font-bold bg-secondary/10 text-secondary border border-secondary/20 hover:bg-secondary/20 transition-colors mr-1" style="cursor:pointer">Show code</button><button data-prompt="{prompt}" onclick="showPromptModal(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-secondary/10 text-secondary border border-secondary/20 hover:bg-secondary/20 transition-colors mr-1" style="cursor:pointer">Show prompt</button><button data-prompt="{prompt}" onclick="copyPrompt(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-secondary/10 text-secondary border border-secondary/20 hover:bg-secondary/20 transition-colors" style="cursor:pointer">Copy prompt</button></td></tr>""");

            var codeSnippet = ReadCodeSnippet(v.FilePath, v.LineNumber, contextLines: 5);
            if (codeSnippet is not null)
            {
                writer.Write($"""<tr id="{codeId}" class="hidden"><td colspan="5" class="px-6 py-3"><pre class="code-block p-4 rounded-lg font-mono text-xs text-secondary leading-relaxed overflow-x-auto border border-outline-variant/10"><code>""");
                foreach (var (lineNum, line) in codeSnippet)
                {
                    var highlight = lineNum == v.LineNumber ? " style=\"background:rgba(var(--md-sys-color-secondary-rgb,160,140,255),.15)\"" : "";
                    writer.WriteLine($"<span{highlight}><span class=\"text-on-surface-variant/50\">{lineNum,4} │ </span>{WebUtility.HtmlEncode(line)}</span>");
                }
                writer.Write("""</code></pre></td></tr>""");
            }
        }

        writer.Write("""
            </tbody>
            </table>
            </div>
            </div>
            </details>
            """);
    }

    private static void WriteEarlyReturnRule(ScanResult result, TextWriter writer, string rootPath, ReportOptions options)
    {
        var violations = EarlyReturnFileInfo.Compute(result, options);
        if (violations.Count == 0) return;

        var totalViolations = violations.Sum(f => f.Violations.Count);

        writer.Write($"""
            <details class="group border border-outline-variant/20 rounded-xl bg-surface-container-low overflow-hidden">
            <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-surface-container-high/30 transition-colors">
            <div class="flex items-center gap-4">
            <div class="h-8 w-1 bg-orange-400"></div>
            <h2 class="font-headline text-3xl font-bold text-on-surface uppercase tracking-tight">Early Return Opportunities</h2>
            <span class="ml-4 px-2 py-0.5 rounded text-[10px] font-bold bg-orange-500/10 text-orange-400 border border-orange-500/20">{totalViolations} VIOLATIONS</span>
            </div>
            <span class="material-symbols-outlined transition-transform group-open:rotate-180">expand_more</span>
            </summary>
            <div class="p-8 pt-0">
            <div class="rounded-xl overflow-hidden border border-outline-variant/20 bg-surface-container">
            <table class="w-full text-left text-sm">
            <thead>
            <tr class="bg-surface-container-high/50 text-on-surface-variant font-label uppercase text-[10px] tracking-widest border-b border-outline-variant/20">
            <th class="px-6 py-3">File</th>
            <th class="px-6 py-3 text-right">Line</th>
            <th class="px-6 py-3">Description</th>
            </tr>
            </thead>
            <tbody class="divide-y divide-outline-variant/10">
            """);

        foreach (var file in violations)
        {
            var relativePath = WebUtility.HtmlEncode(Path.GetRelativePath(rootPath, file.FilePath));
            foreach (var v in file.Violations)
            {
                writer.WriteLine($"""<tr><td class="px-6 py-3 font-mono text-[11px] text-primary">{relativePath}</td><td class="px-6 py-3 text-right font-bold text-tertiary">{v.LineNumber}</td><td class="px-6 py-3 text-on-surface-variant">{WebUtility.HtmlEncode(v.Description)}</td></tr>""");
            }
        }

        writer.Write("""
            </tbody>
            </table>
            </div>
            </div>
            </details>
            """);
    }

    private static List<(int LineNumber, string Line)>? ReadCodeSnippet(string filePath, int targetLine, int contextLines)
    {
        try
        {
            var allLines = File.ReadAllLines(filePath);
            var start = Math.Max(0, targetLine - 1 - contextLines);
            var end = Math.Min(allLines.Length - 1, targetLine - 1 + contextLines);
            var snippet = new List<(int, string)>();
            for (int i = start; i <= end; i++)
                snippet.Add((i + 1, allLines[i]));
            return snippet;
        }
        catch
        {
            return null;
        }
    }
}
