namespace Prepr.Models;

public record ScanResult(
    List<DuplicateBlock> Duplicates,
    int TotalFilesScanned,
    int TotalLinesScanned,
    IReadOnlyDictionary<string, int> FileLineCounts,
    IReadOnlyDictionary<string, NestingDepthInfo> FileMaxNestingDepths,
    IReadOnlyDictionary<string, IReadOnlyList<EarlyReturnViolation>> EarlyReturnViolations,
    IReadOnlyDictionary<string, int> FileCommentLineCounts);
