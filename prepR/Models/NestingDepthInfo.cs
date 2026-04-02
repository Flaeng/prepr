namespace Prepr.Models;

public record NestingDepthInfo(int MaxDepth, IReadOnlyList<(int LineNumber, int Depth)> LineDepths);
