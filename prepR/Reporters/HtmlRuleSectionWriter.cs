using System.Net;

namespace Prepr.Reporters;

internal static class HtmlRuleSectionWriter
{
    private static string ViolationBadge(int count) => count switch
    {
        >= 16 => $"""<span class="ml-4 px-2 py-0.5 rounded text-[10px] font-bold bg-error-container/20 text-error border border-error/20">{count} VIOLATIONS</span>""",
        >= 6 => $"""<span class="ml-4 px-2 py-0.5 rounded text-[10px] font-bold bg-orange-500/10 text-orange-400 border border-orange-500/20">{count} VIOLATIONS</span>""",
        _ => $"""<span class="ml-4 px-2 py-0.5 rounded text-[10px] font-bold bg-secondary/10 text-secondary border border-secondary/20">{count} VIOLATIONS</span>"""
    };
    internal static void WriteLineLimitRule(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var overLimit = OverLimitFileInfo.Compute(result, options, rootPath);
        if (overLimit.Count == 0) return;

        writer.Write($"""
            <details class="group border border-outline-variant/20 rounded-xl bg-surface-container-low overflow-hidden">
            <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-surface-container-high/30 transition-colors">
            <div class="flex items-center gap-4">
            <div class="h-8 w-1 bg-primary"></div>
            <h2 class="font-headline text-3xl font-bold text-on-surface uppercase tracking-tight">Line Count Overage</h2>
            {ViolationBadge(overLimit.Count)}
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
            writer.WriteLine($"""<tr><td class="px-6 py-4 font-mono text-xs text-primary">{WebUtility.HtmlEncode(relativePath)}</td><td class="px-6 py-4 text-right font-bold text-tertiary">{v.LineCount}</td><td class="px-6 py-4 text-right text-on-surface-variant">{v.Limit}</td><td class="px-6 py-4 text-right whitespace-nowrap"><button data-prompt="{prompt}" onclick="showPromptModal(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors mr-1" style="cursor:pointer">Show prompt</button><button data-prompt="{prompt}" onclick="copyPrompt(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors" style="cursor:pointer">Copy prompt</button></td></tr>""");
        }

        writer.Write("""
            </tbody>
            </table>
            </div>
            </div>
            </details>
            """);
    }

    internal static void WriteIndentationRule(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var overIndented = OverIndentedFileInfo.Compute(result, options, rootPath);
        if (overIndented.Count == 0) return;

        writer.Write($"""
            <details class="group border border-outline-variant/20 rounded-xl bg-surface-container-low overflow-hidden">
            <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-surface-container-high/30 transition-colors">
            <div class="flex items-center gap-4">
            <div class="h-8 w-1 bg-primary"></div>
            <h2 class="font-headline text-3xl font-bold text-on-surface uppercase tracking-tight">Indentation Overage</h2>
            {ViolationBadge(overIndented.Count)}
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
            <th class="px-6 py-3 text-right">Lines</th>
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
            writer.WriteLine($"""<tr><td class="px-6 py-3 font-mono text-[11px] text-primary">{WebUtility.HtmlEncode(relativePath)}</td><td class="px-6 py-3 text-right font-bold text-tertiary">{v.MaxDepth}</td><td class="px-6 py-3 text-right text-on-surface-variant">{v.RangesDisplay}</td><td class="px-6 py-3 text-right text-on-surface-variant">{v.Limit}</td><td class="px-6 py-3 text-right whitespace-nowrap"><button onclick="document.getElementById('{codeId}').classList.toggle('hidden')" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors mr-1" style="cursor:pointer">Show code</button><button data-prompt="{prompt}" onclick="showPromptModal(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors mr-1" style="cursor:pointer">Show prompt</button><button data-prompt="{prompt}" onclick="copyPrompt(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors" style="cursor:pointer">Copy prompt</button></td></tr>""");

            var overLimitLines = new HashSet<int>(v.OverLimitRanges.SelectMany(r => Enumerable.Range(r.StartLine, r.EndLine - r.StartLine + 1)));
            var codeSnippets = v.OverLimitRanges
                .SelectMany(r => ReadCodeSnippet(v.FilePath, r.StartLine, r.EndLine, contextLines: 2) ?? [])
                .DistinctBy(s => s.LineNumber)
                .ToList();
            if (codeSnippets.Count > 0)
            {
                writer.Write($"""<tr id="{codeId}" class="hidden"><td colspan="5" class="px-6 py-3" style="max-width:0"><pre class="code-block p-4 rounded-lg font-mono text-xs text-secondary leading-relaxed overflow-x-auto border border-outline-variant/10"><code>""");
                foreach (var (lineNum, line) in codeSnippets)
                {
                    var highlight = overLimitLines.Contains(lineNum) ? " style=\"background:rgba(255,111,126,.2);border-left:3px solid #ff6f7e\"" : "";
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

    internal static void WriteEarlyReturnRule(ScanResult result, TextWriter writer, string rootPath, ReportOptions options)
    {
        var violations = EarlyReturnFileInfo.Compute(result, options);
        if (violations.Count == 0) return;

        var totalViolations = violations.Sum(f => f.Violations.Count);

        writer.Write($"""
            <details class="group border border-outline-variant/20 rounded-xl bg-surface-container-low overflow-hidden">
            <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-surface-container-high/30 transition-colors">
            <div class="flex items-center gap-4">
            <div class="h-8 w-1 bg-primary"></div>
            <h2 class="font-headline text-3xl font-bold text-on-surface uppercase tracking-tight">Early Return Opportunities</h2>
            {ViolationBadge(totalViolations)}
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
        => ReadCodeSnippet(filePath, targetLine, targetLine, contextLines);

    private static List<(int LineNumber, string Line)>? ReadCodeSnippet(string filePath, int startLine, int endLine, int contextLines)
    {
        try
        {
            var allLines = File.ReadAllLines(filePath);
            var start = Math.Max(0, startLine - 1 - contextLines);
            var end = Math.Min(allLines.Length - 1, endLine - 1 + contextLines);
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
