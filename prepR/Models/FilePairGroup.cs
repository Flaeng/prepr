namespace prepr;

public record FilePairGroup(
    string FileA,
    string FileB,
    List<DuplicateBlock> SharedBlocks,
    int SharedLineCount)
{
    public static List<FilePairGroup> ComputeFilePairs(ScanResult result)
    {
        if (result.Duplicates.Count == 0)
            return [];

        var pairMap = new Dictionary<(string, string), List<DuplicateBlock>>();

        foreach (var block in result.Duplicates)
        {
            var files = block.Locations.Select(l => l.FilePath).Distinct().OrderBy(f => f).ToList();
            for (int i = 0; i < files.Count; i++)
            {
                for (int j = i + 1; j < files.Count; j++)
                {
                    var key = (files[i], files[j]);
                    if (!pairMap.TryGetValue(key, out var list))
                    {
                        list = [];
                        pairMap[key] = list;
                    }
                    list.Add(block);
                }
            }
        }

        return pairMap
            .Select(kvp => new FilePairGroup(
                kvp.Key.Item1,
                kvp.Key.Item2,
                kvp.Value,
                kvp.Value.Sum(b => b.Lines.Length)))
            .OrderByDescending(p => p.SharedBlocks.Count)
            .ThenByDescending(p => p.SharedLineCount)
            .ThenBy(p => p.FileA)
            .ToList();
    }
}
