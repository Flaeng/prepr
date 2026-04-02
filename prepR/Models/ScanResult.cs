namespace Prepr.Models;

public record ScanResult(List<DuplicateBlock> Duplicates, int TotalFilesScanned, int TotalLinesScanned, IReadOnlyDictionary<string, int> FileLineCounts, IReadOnlyDictionary<string, (int MaxDepth, IReadOnlyList<(int LineNumber, int Depth)> LineDepths)> FileMaxNestingDepths, IReadOnlyDictionary<string, IReadOnlyList<EarlyReturnViolation>> EarlyReturnViolations);
