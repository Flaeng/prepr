namespace Prepr.Models;

public record DuplicationFileInfo(
    string FilePath,
    int DuplicateBlockCount,
    int DuplicatedLineCount,
    int TotalLineCount,
    double DuplicationPercentage,
    Severity Severity)
{
    public static List<DuplicationFileInfo> ComputePerFile(ScanResult result, ReportOptions options)
    {
        if (result.Duplicates.Count == 0)
            return [];

        var perFile = result.Duplicates
            .SelectMany(b => b.Locations.Select(loc => new { loc.FilePath, loc.StartLine, loc.EndLine, Block = b }))
            .GroupBy(x => x.FilePath);

        var infos = new List<DuplicationFileInfo>();
        foreach (var group in perFile)
        {
            int blockCount = group.Select(x => x.Block).Distinct().Count();
            int duplicatedLines = group.Sum(x => x.EndLine - x.StartLine + 1);
            int totalLines = CountFileLines(group.Key);
            double pct = totalLines > 0 ? (double)duplicatedLines / totalLines * 100 : 0;
            var severity = pct >= options.HighSeverityThreshold ? Severity.High
                         : pct >= options.MediumSeverityThreshold ? Severity.Medium
                         : Severity.Low;

            infos.Add(new DuplicationFileInfo(group.Key, blockCount, duplicatedLines, totalLines, pct, severity));
        }

        return infos.OrderByDescending(f => f.DuplicationPercentage).ThenBy(f => f.FilePath).ToList();
    }

    private static int CountFileLines(string filePath)
    {
        try { return File.ReadAllLines(filePath).Length; }
        catch { return 0; }
    }

    internal string? GetPrompt(string relativePath, IEnumerable<DuplicateBlock>? blocks = null, string? rootPath = null)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"Refactor the file `{relativePath}` to eliminate code duplication. The file has {DuplicateBlockCount} duplicate block(s) with {DuplicatedLineCount} duplicated lines ({DuplicationPercentage:F1}% duplication). Extract shared logic into common methods, base classes, or shared utilities to keep the code DRY.");

        if (blocks is not null && rootPath is not null)
        {
            var fileBlocks = blocks.Where(b => b.Locations.Any(l => l.FilePath == FilePath)).ToList();
            if (fileBlocks.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.Append("Duplicate locations:");
                foreach (var block in fileBlocks)
                {
                    sb.AppendLine();
                    var thisLoc = block.Locations.First(l => l.FilePath == FilePath);
                    sb.Append($"- Lines {thisLoc.StartLine}-{thisLoc.EndLine} ({block.Lines.Length} lines) also found in:");
                    foreach (var loc in block.Locations.Where(l => l.FilePath != FilePath))
                    {
                        var otherRel = Path.GetRelativePath(rootPath, loc.FilePath);
                        sb.Append($" `{otherRel}` lines {loc.StartLine}-{loc.EndLine};");
                    }
                }
            }
        }

        return sb.ToString();
    }
}
