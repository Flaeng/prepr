using System.Net;

namespace Prepr.Reporters;

internal static class HtmlRuleSectionWriter
{
    private static string SeverityBadges(int highCount, int mediumCount, int lowCount) =>
        $"""<span class="px-2 py-0.5 rounded text-[10px] font-bold bg-error-container/20 text-error border border-error/20">{highCount} HIGH</span><span class="px-2 py-0.5 rounded text-[10px] font-bold bg-orange-500/10 text-orange-400 border border-orange-500/20">{mediumCount} MEDIUM</span><span class="px-2 py-0.5 rounded text-[10px] font-bold bg-secondary/10 text-secondary border border-secondary/20">{lowCount} LOW</span>""";
    private static string SeverityCell(Severity severity)
    {
        var (badgeClass, label) = severity switch
        {
            Severity.High => ("bg-error-container/20 text-error", "HIGH"),
            Severity.Medium => ("bg-orange-500/10 text-orange-400", "MEDIUM"),
            _ => ("bg-secondary/10 text-secondary", "LOW")
        };
        return $"""<td class="px-6 py-4"><span class="px-2 py-0.5 rounded text-[10px] font-bold {badgeClass}">{label}</span></td>""";
    }
    internal static void WriteLineLimitRule(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var overLimit = OverLimitFileInfo.Compute(result, options, rootPath);
        var highCount = overLimit.Count(v => v.Severity == Severity.High);
        var mediumCount = overLimit.Count(v => v.Severity == Severity.Medium);
        var lowCount = overLimit.Count(v => v.Severity == Severity.Low);

        writer.Write($"""
            <details class="group border border-outline-variant/20 rounded-xl bg-surface-container-low overflow-hidden">
            <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-surface-container-high/30 transition-colors">
            <div class="flex items-center gap-4">
            <div class="h-8 w-1 bg-primary"></div>
            <h2 class="font-headline text-3xl font-bold text-on-surface uppercase tracking-tight">Line Count Overage</h2>
            {SeverityBadges(highCount, mediumCount, lowCount)}
            </div>
            <span class="material-symbols-outlined transition-transform group-open:rotate-180">expand_more</span>
            </summary>
            <div class="p-8 pt-0">
            """);

        if (overLimit.Count == 0)
        {
            writer.WriteLine("""<p class="text-secondary italic">No violations found.</p>""");
        }
        else
        {
            writer.Write("""
            <div class="rounded-xl overflow-hidden border border-outline-variant/20 bg-surface-container">
            <table class="w-full text-left text-sm">
            <thead>
            <tr class="bg-surface-container-high/50 text-on-surface-variant font-label uppercase text-[10px] tracking-widest border-b border-outline-variant/20">
            <th class="px-6 py-3">File</th>
            <th class="px-6 py-3 text-right">Lines</th>
            <th class="px-6 py-3 text-right">Limit</th>
            <th class="px-6 py-3">Severity</th>
            <th class="px-6 py-3 text-right">Actions</th>
            </tr>
            </thead>
            <tbody class="divide-y divide-outline-variant/10">
            """);

            foreach (var v in overLimit)
            {
                var relativePath = Path.GetRelativePath(rootPath, v.FilePath);
                var prompt = WebUtility.HtmlEncode(v.GetPrompt(relativePath));
                writer.WriteLine($"""<tr><td class="px-6 py-4 font-mono text-xs text-primary">{WebUtility.HtmlEncode(relativePath)}</td><td class="px-6 py-4 text-right font-bold text-tertiary">{v.LineCount}</td><td class="px-6 py-4 text-right text-on-surface-variant">{v.Limit}</td>{SeverityCell(v.Severity)}<td class="px-6 py-4 text-right whitespace-nowrap"><button data-prompt="{prompt}" onclick="showPromptModal(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors mr-1" style="cursor:pointer">Show prompt</button><button data-prompt="{prompt}" onclick="copyPrompt(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors" style="cursor:pointer">Copy prompt</button></td></tr>""");
            }

            writer.Write("""
            </tbody>
            </table>
            </div>
            """);
        }

        writer.Write("""
            </div>
            </details>
            """);
    }

    internal static void WriteIndentationRule(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var overIndented = OverIndentedFileInfo.Compute(result, options, rootPath);
        var highCount = overIndented.Count(v => v.Severity == Severity.High);
        var mediumCount = overIndented.Count(v => v.Severity == Severity.Medium);
        var lowCount = overIndented.Count(v => v.Severity == Severity.Low);

        writer.Write($"""
            <details class="group border border-outline-variant/20 rounded-xl bg-surface-container-low overflow-hidden">
            <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-surface-container-high/30 transition-colors">
            <div class="flex items-center gap-4">
            <div class="h-8 w-1 bg-primary"></div>
            <h2 class="font-headline text-3xl font-bold text-on-surface uppercase tracking-tight">Indentation Overage</h2>
            {SeverityBadges(highCount, mediumCount, lowCount)}
            </div>
            <span class="material-symbols-outlined transition-transform group-open:rotate-180">expand_more</span>
            </summary>
            <div class="p-8 pt-0">
            """);

        if (overIndented.Count == 0)
        {
            writer.WriteLine("""<p class="text-secondary italic">No violations found.</p>""");
        }
        else
        {
            writer.Write("""
            <div class="rounded-xl overflow-hidden border border-outline-variant/20 bg-surface-container">
            <table class="w-full text-left text-sm">
            <thead>
            <tr class="bg-surface-container-high/50 text-on-surface-variant font-label uppercase text-[10px] tracking-widest border-b border-outline-variant/20">
            <th class="px-6 py-3">File</th>
            <th class="px-6 py-3 text-right">Depth</th>
            <th class="px-6 py-3 text-right">Lines</th>
            <th class="px-6 py-3 text-right">Limit</th>
            <th class="px-6 py-3">Severity</th>
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
                writer.WriteLine($"""<tr><td class="px-6 py-3 font-mono text-[11px] text-primary">{WebUtility.HtmlEncode(relativePath)}</td><td class="px-6 py-3 text-right font-bold text-tertiary">{v.MaxDepth}</td><td class="px-6 py-3 text-right text-on-surface-variant">{v.RangesDisplay}</td><td class="px-6 py-3 text-right text-on-surface-variant">{v.Limit}</td>{SeverityCell(v.Severity)}<td class="px-6 py-3 text-right whitespace-nowrap"><button onclick="document.getElementById('{codeId}').classList.toggle('hidden')" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors mr-1" style="cursor:pointer">Show code</button><button data-prompt="{prompt}" onclick="showPromptModal(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors mr-1" style="cursor:pointer">Show prompt</button><button data-prompt="{prompt}" onclick="copyPrompt(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors" style="cursor:pointer">Copy prompt</button></td></tr>""");

                var overLimitLines = new HashSet<int>(v.OverLimitRanges.SelectMany(r => Enumerable.Range(r.StartLine, r.EndLine - r.StartLine + 1)));
                var codeSnippets = v.OverLimitRanges
                    .SelectMany(r => ReadCodeSnippet(v.FilePath, r.StartLine, r.EndLine, contextLines: 2) ?? [])
                    .DistinctBy(s => s.LineNumber)
                    .ToList();
                if (codeSnippets.Count > 0)
                {
                    writer.Write($"""<tr id="{codeId}" class="hidden"><td colspan="6" class="px-6 py-3" style="max-width:0"><pre class="code-block p-4 rounded-lg font-mono text-xs text-secondary leading-relaxed overflow-x-auto border border-outline-variant/10"><code>""");
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
            """);
        }

        writer.Write("""
            </div>
            </details>
            """);
    }

    internal static void WriteEarlyReturnRule(ScanResult result, TextWriter writer, string rootPath, ReportOptions options)
    {
        var violations = EarlyReturnFileInfo.Compute(result, options);
        var highCount = violations.Sum(f => f.Severity == Severity.High ? f.Violations.Count : 0);
        var mediumCount = violations.Sum(f => f.Severity == Severity.Medium ? f.Violations.Count : 0);
        var lowCount = violations.Sum(f => f.Severity == Severity.Low ? f.Violations.Count : 0);

        writer.Write($"""
            <details class="group border border-outline-variant/20 rounded-xl bg-surface-container-low overflow-hidden">
            <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-surface-container-high/30 transition-colors">
            <div class="flex items-center gap-4">
            <div class="h-8 w-1 bg-primary"></div>
            <h2 class="font-headline text-3xl font-bold text-on-surface uppercase tracking-tight">Early Return Opportunities</h2>
            {SeverityBadges(highCount, mediumCount, lowCount)}
            </div>
            <span class="material-symbols-outlined transition-transform group-open:rotate-180">expand_more</span>
            </summary>
            <div class="p-8 pt-0">
            """);

        if (violations.Count == 0)
        {
            writer.WriteLine("""<p class="text-secondary italic">No violations found.</p>""");
        }
        else
        {
            writer.Write("""
            <div class="rounded-xl overflow-hidden border border-outline-variant/20 bg-surface-container">
            <table class="w-full text-left text-sm">
            <thead>
            <tr class="bg-surface-container-high/50 text-on-surface-variant font-label uppercase text-[10px] tracking-widest border-b border-outline-variant/20">
            <th class="px-6 py-3">File</th>
            <th class="px-6 py-3 text-right">Violations</th>
            <th class="px-6 py-3">Severity</th>
            <th class="px-6 py-3 text-right">Actions</th>
            </tr>
            </thead>
            <tbody class="divide-y divide-outline-variant/10">
            """);

            for (int fi = 0; fi < violations.Count; fi++)
            {
                var file = violations[fi];
                var relativePath = WebUtility.HtmlEncode(Path.GetRelativePath(rootPath, file.FilePath));
                var codeId = $"early-return-{fi}";
                var prompt = WebUtility.HtmlEncode(file.GetPrompt(Path.GetRelativePath(rootPath, file.FilePath)));
                writer.WriteLine($"""<tr class="hover:bg-primary/5 transition-colors"><td class="px-6 py-3 font-mono text-[11px] text-primary">{relativePath}</td><td class="px-6 py-3 text-right font-bold text-tertiary">{file.Violations.Count}</td>{SeverityCell(file.Severity)}<td class="px-6 py-3 text-right whitespace-nowrap"><button onclick="document.getElementById('{codeId}').classList.toggle('hidden')" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors mr-1" style="cursor:pointer">Show code</button><button data-prompt="{prompt}" onclick="showPromptModal(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors mr-1" style="cursor:pointer">Show prompt</button><button data-prompt="{prompt}" onclick="copyPrompt(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors" style="cursor:pointer">Copy prompt</button></td></tr>""");

                // Read code snippets around each violation
                var snippets = file.Violations
                    .SelectMany(v => ReadCodeSnippet(file.FilePath, v.LineNumber, contextLines: 3) ?? [])
                    .DistinctBy(s => s.LineNumber)
                    .OrderBy(s => s.LineNumber)
                    .ToList();
                var violationLines = new HashSet<int>(file.Violations.Select(v => v.LineNumber));

                if (snippets.Count > 0)
                {
                    writer.Write($"""<tr id="{codeId}" class="hidden"><td colspan="4" class="px-6 py-3" style="max-width:0"><div class="space-y-3">""");
                    foreach (var v in file.Violations)
                    {
                        writer.Write($"""<div class="text-[10px] font-bold uppercase tracking-widest text-on-surface-variant mt-2">Line {v.LineNumber} — {WebUtility.HtmlEncode(v.Description)}</div>""");
                        var vSnippet = ReadCodeSnippet(file.FilePath, v.LineNumber, contextLines: 3);
                        if (vSnippet is not null)
                        {
                            writer.Write("""<pre class="code-block p-4 rounded-lg font-mono text-xs text-secondary leading-relaxed overflow-x-auto border border-outline-variant/10"><code>""");
                            foreach (var (lineNum, line) in vSnippet)
                            {
                                var highlight = lineNum == v.LineNumber ? " style=\"background:rgba(255,111,126,.2);border-left:3px solid #ff6f7e\"" : "";
                                writer.WriteLine($"<span{highlight}><span class=\"text-on-surface-variant/50\">{lineNum,4} │ </span>{WebUtility.HtmlEncode(line)}</span>");
                            }
                            writer.Write("</code></pre>");
                        }
                    }
                    writer.Write("</div></td></tr>");
                }
            }

            writer.Write("""
            </tbody>
            </table>
            </div>
            """);
        }

        writer.Write("""
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

    internal static void WriteCommentDensityRule(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var violations = CommentDensityFileInfo.Compute(result, options, rootPath);
        var highCount = violations.Count(v => v.Severity == Severity.High);
        var mediumCount = violations.Count(v => v.Severity == Severity.Medium);
        var lowCount = violations.Count(v => v.Severity == Severity.Low);

        writer.Write($"""
            <details class="group border border-outline-variant/20 rounded-xl bg-surface-container-low overflow-hidden">
            <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-surface-container-high/30 transition-colors">
            <div class="flex items-center gap-4">
            <div class="h-8 w-1 bg-primary"></div>
            <h2 class="font-headline text-3xl font-bold text-on-surface uppercase tracking-tight">Comment Density</h2>
            {SeverityBadges(highCount, mediumCount, lowCount)}
            </div>
            <span class="material-symbols-outlined transition-transform group-open:rotate-180">expand_more</span>
            </summary>
            <div class="p-8 pt-0">
            """);

        if (violations.Count == 0)
        {
            writer.WriteLine("""<p class="text-secondary italic">No violations found.</p>""");
        }
        else
        {
            writer.Write("""
            <div class="rounded-xl overflow-hidden border border-outline-variant/20 bg-surface-container">
            <table class="w-full text-left text-sm">
            <thead>
            <tr class="bg-surface-container-high/50 text-on-surface-variant font-label uppercase text-[10px] tracking-widest border-b border-outline-variant/20">
            <th class="px-6 py-3">File</th>
            <th class="px-6 py-3 text-right">Comments</th>
            <th class="px-6 py-3 text-right">Total Lines</th>
            <th class="px-6 py-3">Severity</th>
            <th class="px-6 py-3 text-right">Actions</th>
            </tr>
            </thead>
            <tbody class="divide-y divide-outline-variant/10">
            """);

            foreach (var v in violations)
            {
                var relativePath = Path.GetRelativePath(rootPath, v.FilePath);
                var prompt = WebUtility.HtmlEncode(v.GetPrompt(relativePath));
                writer.WriteLine($"""<tr><td class="px-6 py-4 font-mono text-xs text-primary">{WebUtility.HtmlEncode(relativePath)}</td><td class="px-6 py-4 text-right font-bold text-tertiary">{v.CommentLines}</td><td class="px-6 py-4 text-right text-on-surface-variant">{v.TotalLines}</td>{SeverityCell(v.Severity)}<td class="px-6 py-4 text-right whitespace-nowrap"><button data-prompt="{prompt}" onclick="showPromptModal(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors mr-1" style="cursor:pointer">Show prompt</button><button data-prompt="{prompt}" onclick="copyPrompt(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors" style="cursor:pointer">Copy prompt</button></td></tr>""");
            }

            writer.Write("""
            </tbody>
            </table>
            </div>
            """);
        }

        writer.Write("""
            </div>
            </details>
            """);
    }

    internal static void WriteMagicNumberRule(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var violations = MagicNumberFileInfo.Compute(result, options, rootPath);
        var highCount = violations.Count(v => v.Severity == Severity.High);
        var mediumCount = violations.Count(v => v.Severity == Severity.Medium);
        var lowCount = violations.Count(v => v.Severity == Severity.Low);

        writer.Write($"""
            <details class="group border border-outline-variant/20 rounded-xl bg-surface-container-low overflow-hidden">
            <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-surface-container-high/30 transition-colors">
            <div class="flex items-center gap-4">
            <div class="h-8 w-1 bg-primary"></div>
            <h2 class="font-headline text-3xl font-bold text-on-surface uppercase tracking-tight">Magic Numbers</h2>
            {SeverityBadges(highCount, mediumCount, lowCount)}
            </div>
            <span class="material-symbols-outlined transition-transform group-open:rotate-180">expand_more</span>
            </summary>
            <div class="p-8 pt-0">
            """);

        if (violations.Count == 0)
        {
            writer.WriteLine("""<p class="text-secondary italic">No violations found.</p>""");
        }
        else
        {
            writer.Write("""
            <div class="rounded-xl overflow-hidden border border-outline-variant/20 bg-surface-container">
            <table class="w-full text-left text-sm">
            <thead>
            <tr class="bg-surface-container-high/50 text-on-surface-variant font-label uppercase text-[10px] tracking-widest border-b border-outline-variant/20">
            <th class="px-6 py-3">File</th>
            <th class="px-6 py-3 text-right">Count</th>
            <th class="px-6 py-3 text-right">Limit</th>
            <th class="px-6 py-3">Severity</th>
            <th class="px-6 py-3 text-right">Actions</th>
            </tr>
            </thead>
            <tbody class="divide-y divide-outline-variant/10">
            """);

            for (int i = 0; i < violations.Count; i++)
            {
                var file = violations[i];
                var relativePath = Path.GetRelativePath(rootPath, file.FilePath);
                var prompt = WebUtility.HtmlEncode(file.GetPrompt(relativePath));
                var codeId = $"magic-number-{i}";
                writer.WriteLine($"""<tr><td class="px-6 py-4 font-mono text-xs text-primary">{WebUtility.HtmlEncode(relativePath)}</td><td class="px-6 py-4 text-right font-bold text-tertiary">{file.Violations.Count}</td><td class="px-6 py-4 text-right text-on-surface-variant">{file.Limit}</td>{SeverityCell(file.Severity)}<td class="px-6 py-4 text-right whitespace-nowrap"><button onclick="document.getElementById('{codeId}').classList.toggle('hidden')" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors mr-1" style="cursor:pointer">Show details</button><button data-prompt="{prompt}" onclick="showPromptModal(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors mr-1" style="cursor:pointer">Show prompt</button><button data-prompt="{prompt}" onclick="copyPrompt(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors" style="cursor:pointer">Copy prompt</button></td></tr>""");

                writer.Write($"""<tr id="{codeId}" class="hidden"><td colspan="5" class="px-6 py-3"><div class="space-y-1">""");
                foreach (var v in file.Violations)
                {
                    writer.WriteLine($"""<div class="text-xs"><span class="text-on-surface-variant">Line {v.LineNumber}:</span> <span class="font-mono text-tertiary">{WebUtility.HtmlEncode(v.Value)}</span></div>""");
                }
                writer.Write("</div></td></tr>");
            }

            writer.Write("""
            </tbody>
            </table>
            </div>
            """);
        }

        writer.Write("""
            </div>
            </details>
            """);
    }

    internal static void WriteMagicStringRule(ScanResult result, string rootPath, TextWriter writer, ReportOptions options)
    {
        var violations = MagicStringFileInfo.Compute(result, options, rootPath);
        var highCount = violations.Count(v => v.Severity == Severity.High);
        var mediumCount = violations.Count(v => v.Severity == Severity.Medium);
        var lowCount = violations.Count(v => v.Severity == Severity.Low);

        writer.Write($"""
            <details class="group border border-outline-variant/20 rounded-xl bg-surface-container-low overflow-hidden">
            <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-surface-container-high/30 transition-colors">
            <div class="flex items-center gap-4">
            <div class="h-8 w-1 bg-primary"></div>
            <h2 class="font-headline text-3xl font-bold text-on-surface uppercase tracking-tight">Magic Strings</h2>
            {SeverityBadges(highCount, mediumCount, lowCount)}
            </div>
            <span class="material-symbols-outlined transition-transform group-open:rotate-180">expand_more</span>
            </summary>
            <div class="p-8 pt-0">
            """);

        if (violations.Count == 0)
        {
            writer.WriteLine("""<p class="text-secondary italic">No violations found.</p>""");
        }
        else
        {
            writer.Write("""
            <div class="rounded-xl overflow-hidden border border-outline-variant/20 bg-surface-container">
            <table class="w-full text-left text-sm">
            <thead>
            <tr class="bg-surface-container-high/50 text-on-surface-variant font-label uppercase text-[10px] tracking-widest border-b border-outline-variant/20">
            <th class="px-6 py-3">File</th>
            <th class="px-6 py-3 text-right">Count</th>
            <th class="px-6 py-3 text-right">Limit</th>
            <th class="px-6 py-3">Severity</th>
            <th class="px-6 py-3 text-right">Actions</th>
            </tr>
            </thead>
            <tbody class="divide-y divide-outline-variant/10">
            """);

            for (int i = 0; i < violations.Count; i++)
            {
                var file = violations[i];
                var relativePath = Path.GetRelativePath(rootPath, file.FilePath);
                var prompt = WebUtility.HtmlEncode(file.GetPrompt(relativePath));
                var codeId = $"magic-string-{i}";
                writer.WriteLine($"""<tr><td class="px-6 py-4 font-mono text-xs text-primary">{WebUtility.HtmlEncode(relativePath)}</td><td class="px-6 py-4 text-right font-bold text-tertiary">{file.Violations.Count}</td><td class="px-6 py-4 text-right text-on-surface-variant">{file.Limit}</td>{SeverityCell(file.Severity)}<td class="px-6 py-4 text-right whitespace-nowrap"><button onclick="document.getElementById('{codeId}').classList.toggle('hidden')" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors mr-1" style="cursor:pointer">Show details</button><button data-prompt="{prompt}" onclick="showPromptModal(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors mr-1" style="cursor:pointer">Show prompt</button><button data-prompt="{prompt}" onclick="copyPrompt(this)" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors" style="cursor:pointer">Copy prompt</button></td></tr>""");

                writer.Write($"""<tr id="{codeId}" class="hidden"><td colspan="5" class="px-6 py-3"><div class="space-y-1">""");
                foreach (var v in file.Violations)
                {
                    writer.WriteLine($"""<div class="text-xs"><span class="text-on-surface-variant">Line {v.LineNumber}:</span> <span class="font-mono text-tertiary">"{WebUtility.HtmlEncode(v.Value)}"</span></div>""");
                }
                writer.Write("</div></td></tr>");
            }

            writer.Write("""
            </tbody>
            </table>
            </div>
            """);
        }

        writer.Write("""
            </div>
            </details>
            """);
    }
}
