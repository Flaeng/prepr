namespace Prepr.Models;

public record ViolationScore(
    int RawScore,
    double NormalizedScore,
    char Grade,
    int DuplicateBlockCount,
    int LineLimitFileCount,
    int IndentationFileCount,
    int EarlyReturnFileCount,
    int CommentDensityFileCount,
    int MagicNumberFileCount,
    int MagicStringFileCount)
{
    public const int PointsPerDuplicateBlock = 5;
    public const int PointsPerEarlyReturnFile = 3;
    public const int PointsPerLineLimitFile = 2;
    public const int PointsPerIndentationFile = 1;
    public const int PointsPerCommentDensityFile = 1;
    public const int PointsPerMagicNumberFile = 1;
    public const int PointsPerMagicStringFile = 1;

    public static ViolationScore Compute(ScanResult result, ReportOptions options, string rootPath)
    {
        int totalLines = result.TotalLinesScanned;

        int dupCount = result.Duplicates.Count;

        int lineLimitCount = 0;
        if (options.LineLimitRule is not null)
            lineLimitCount = OverLimitFileInfo.Compute(result, options, rootPath).Count;

        int indentCount = 0;
        if (options.IndentationRule is not null)
            indentCount = OverIndentedFileInfo.Compute(result, options, rootPath).Count;

        int earlyReturnCount = 0;
        if (options.EarlyReturn)
            earlyReturnCount = EarlyReturnFileInfo.Compute(result, options).Count;

        int commentCount = 0;
        if (options.MinCommentDensityRule is not null || options.MaxCommentDensityRule is not null)
            commentCount = CommentDensityFileInfo.Compute(result, options, rootPath).Count;

        int magicNumCount = 0;
        if (options.MagicNumberRule is not null)
            magicNumCount = MagicNumberFileInfo.Compute(result, options, rootPath).Count;

        int magicStrCount = 0;
        if (options.MagicStringRule is not null)
            magicStrCount = MagicStringFileInfo.Compute(result, options, rootPath).Count;

        int raw = dupCount * PointsPerDuplicateBlock
                + earlyReturnCount * PointsPerEarlyReturnFile
                + lineLimitCount * PointsPerLineLimitFile
                + indentCount * PointsPerIndentationFile
                + commentCount * PointsPerCommentDensityFile
                + magicNumCount * PointsPerMagicNumberFile
                + magicStrCount * PointsPerMagicStringFile;

        double normalized = totalLines > 0
            ? Math.Round((double)raw / totalLines * 1000, 1)
            : 0;

        return new ViolationScore(raw, normalized, GetGrade(normalized),
            dupCount, lineLimitCount, indentCount, earlyReturnCount,
            commentCount, magicNumCount, magicStrCount);
    }

    public static char GetGrade(double normalizedScore) => normalizedScore switch
    {
        <= 50 => 'A',
        <= 150 => 'B',
        <= 300 => 'C',
        <= 500 => 'D',
        _ => 'F'
    };
}
