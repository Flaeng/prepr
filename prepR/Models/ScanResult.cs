namespace prepr;

public record ScanResult(List<DuplicateBlock> Duplicates, int TotalFilesScanned, int TotalLinesScanned, IReadOnlyDictionary<string, int> FileLineCounts);
