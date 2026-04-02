using System.Net;

namespace Prepr.Reporters;

internal static class HtmlDuplicationWriter
{
    internal static void WritePerFileSummary(ScanResult result, string rootPath, TextWriter writer, List<DuplicationFileInfo> fileInfos)
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
                <td class="px-6 py-4">{info.DuplicatedLineCount} lines ({info.DuplicationPercentage:F1}%)</td>
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
                writer.Write($"""<tr id="{codeId}" class="hidden"><td colspan="5" class="px-6 py-3" style="max-width:0"><div class="space-y-3">""");
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

    internal static void WriteFilePairs(string rootPath, TextWriter writer, List<FilePairGroup> pairs)
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
            writer.WriteLine($"""<tr><td class="px-6 py-3 font-mono text-xs text-primary">{relA}</td><td class="px-6 py-3 font-mono text-xs text-primary">{relB}</td><td class="px-6 py-3">{pair.SharedBlocks.Count}</td><td class="px-6 py-3">{pair.SharedLineCount}</td><td class="px-6 py-3 text-right whitespace-nowrap"><button onclick="document.getElementById('{codeId}').classList.toggle('hidden')" class="px-2 py-1 rounded text-[10px] font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-colors" style="cursor:pointer">Show code</button></td></tr>""");

            writer.Write($"""<tr id="{codeId}" class="hidden"><td class="px-6 py-3" colspan="5" style="max-width:0"><div class="space-y-3">""");
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
}
