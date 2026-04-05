namespace Prepr.Models;

public record TechDebtScore(
    double Score,
    char Grade,
    double DuplicationDensity,
    double LineLimitDensity,
    double IndentationDensity,
    double EarlyReturnDensity,
    double CommentDensityDensity,
    double MagicNumberDensity,
    double MagicStringDensity,
    double FolderFilesDensity)
{
    public static TechDebtScore Compute(ScanResult result, ReportOptions options, string rootPath)
    {
        int totalLines = result.TotalLinesScanned;
        int totalFiles = result.TotalFilesScanned;

        // Duplication density: duplicated lines as % of total lines
        var stats = SummaryStatistics.Compute(result);
        double dupDensity = totalLines > 0
            ? Math.Min(100.0, (double)stats.TotalDuplicatedLines / totalLines * 100)
            : 0;

        // Line limit density: excess lines as % of total lines
        double lineLimitDensity = 0;
        if (options.LineLimitRule is not null && totalLines > 0)
        {
            var overLimit = OverLimitFileInfo.Compute(result, options, rootPath);
            int totalExcess = overLimit.Sum(v => v.LineCount - v.Limit);
            lineLimitDensity = Math.Min(100.0, (double)totalExcess / totalLines * 100);
        }

        // Indentation density: % of files exceeding indentation limit
        double indentDensity = 0;
        if (options.IndentationRule is not null && totalFiles > 0)
        {
            var overIndented = OverIndentedFileInfo.Compute(result, options, rootPath);
            indentDensity = Math.Min(100.0, (double)overIndented.Count / totalFiles * 100);
        }

        // Early return density: % of files with early return violations
        double earlyReturnDensity = 0;
        if (options.EarlyReturn && totalFiles > 0)
        {
            var violations = EarlyReturnFileInfo.Compute(result, options);
            earlyReturnDensity = Math.Min(100.0, (double)violations.Count / totalFiles * 100);
        }

        // Comment density: % of files violating comment density limits
        double commentDensityDensity = 0;
        if ((options.MinCommentDensityRule is not null || options.MaxCommentDensityRule is not null) && totalFiles > 0)
        {
            var commentViolations = CommentDensityFileInfo.Compute(result, options, rootPath);
            commentDensityDensity = Math.Min(100.0, (double)commentViolations.Count / totalFiles * 100);
        }

        // Magic number density: % of files with magic number violations
        double magicNumberDensity = 0;
        if (options.MagicNumberRule is not null && totalFiles > 0)
        {
            var magicNumberViolations = MagicNumberFileInfo.Compute(result, options, rootPath);
            magicNumberDensity = Math.Min(100.0, (double)magicNumberViolations.Count / totalFiles * 100);
        }

        // Magic string density: % of files with magic string violations
        double magicStringDensity = 0;
        if (options.MagicStringRule is not null && totalFiles > 0)
        {
            var magicStringViolations = MagicStringFileInfo.Compute(result, options, rootPath);
            magicStringDensity = Math.Min(100.0, (double)magicStringViolations.Count / totalFiles * 100);
        }

        // Folder files density: % of folders exceeding file count limit
        double folderFilesDensity = 0;
        if (options.MaxFolderFilesRule is not null && result.FolderFileCounts.Count > 0)
        {
            var overCrowded = OverCrowdedFolderInfo.Compute(result, options, rootPath);
            folderFilesDensity = Math.Min(100.0, (double)overCrowded.Count / result.FolderFileCounts.Count * 100);
        }

        double score = options.TechDebtWeightDuplication / 100.0 * dupDensity
                     + options.TechDebtWeightLineLimit / 100.0 * lineLimitDensity
                     + options.TechDebtWeightIndentation / 100.0 * indentDensity
                     + options.TechDebtWeightEarlyReturn / 100.0 * earlyReturnDensity
                     + options.TechDebtWeightCommentDensity / 100.0 * commentDensityDensity
                     + options.TechDebtWeightMagicNumber / 100.0 * magicNumberDensity
                     + options.TechDebtWeightMagicString / 100.0 * magicStringDensity
                     + options.TechDebtWeightFolderFiles / 100.0 * folderFilesDensity;

        score = Math.Round(Math.Min(100.0, score), 1);

        return new TechDebtScore(score, GetGrade(score), dupDensity, lineLimitDensity, indentDensity, earlyReturnDensity, commentDensityDensity, magicNumberDensity, magicStringDensity, folderFilesDensity);
    }

    public static char GetGrade(double score) => score switch
    {
        <= 10 => 'A',
        <= 25 => 'B',
        <= 50 => 'C',
        <= 75 => 'D',
        _ => 'F'
    };
}
