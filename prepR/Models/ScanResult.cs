namespace Prepr.Models;

public record ScanResult(List<DuplicateBlock> Duplicates, int TotalFilesScanned, int TotalLinesScanned, IReadOnlyDictionary<string, int> FileLineCounts, IReadOnlyDictionary<string, (int MaxDepth, int LineNumber)> FileMaxNestingDepths, IReadOnlyDictionary<string, IReadOnlyList<EarlyReturnViolation>> EarlyReturnViolations);
