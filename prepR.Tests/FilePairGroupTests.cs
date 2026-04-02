using prepr;

namespace prepr.Tests;

public class FilePairGroupTests
{
    [Fact]
    public void ComputeFilePairs_NoDuplicates_ReturnsEmpty()
    {
        var result = new ScanResult([], 5, 100, new Dictionary<string, int>());
        var pairs = FilePairGroup.ComputeFilePairs(result);

        Assert.Empty(pairs);
    }

    [Fact]
    public void ComputeFilePairs_TwoFilesOneBlock_OnePair()
    {
        var block = new DuplicateBlock(
            ["a", "b", "c", "d", "e"],
            [
                new FileLocation("/src/A.cs", 1, 5),
                new FileLocation("/src/B.cs", 10, 14)
            ]);
        var result = new ScanResult([block], 2, 100, new Dictionary<string, int>());
        var pairs = FilePairGroup.ComputeFilePairs(result);

        Assert.Single(pairs);
        Assert.Equal("/src/A.cs", pairs[0].FileA);
        Assert.Equal("/src/B.cs", pairs[0].FileB);
        Assert.Single(pairs[0].SharedBlocks);
        Assert.Equal(5, pairs[0].SharedLineCount);
    }

    [Fact]
    public void ComputeFilePairs_ThreeFilesOneBlock_ThreePairs()
    {
        var block = new DuplicateBlock(
            ["a", "b", "c", "d", "e"],
            [
                new FileLocation("/src/A.cs", 1, 5),
                new FileLocation("/src/B.cs", 1, 5),
                new FileLocation("/src/C.cs", 1, 5)
            ]);
        var result = new ScanResult([block], 3, 150, new Dictionary<string, int>());
        var pairs = FilePairGroup.ComputeFilePairs(result);

        Assert.Equal(3, pairs.Count);
        // All pairs should have the same block
        Assert.All(pairs, p =>
        {
            Assert.Single(p.SharedBlocks);
            Assert.Equal(5, p.SharedLineCount);
        });
    }

    [Fact]
    public void ComputeFilePairs_TwoBlocksSameFiles_OnePairTwoBlocks()
    {
        var block1 = new DuplicateBlock(
            ["a", "b", "c", "d", "e"],
            [
                new FileLocation("/src/A.cs", 1, 5),
                new FileLocation("/src/B.cs", 10, 14)
            ]);
        var block2 = new DuplicateBlock(
            ["f", "g", "h", "i", "j"],
            [
                new FileLocation("/src/A.cs", 20, 24),
                new FileLocation("/src/B.cs", 30, 34)
            ]);
        var result = new ScanResult([block1, block2], 2, 100, new Dictionary<string, int>());
        var pairs = FilePairGroup.ComputeFilePairs(result);

        Assert.Single(pairs);
        Assert.Equal(2, pairs[0].SharedBlocks.Count);
        Assert.Equal(10, pairs[0].SharedLineCount);
    }

    [Fact]
    public void ComputeFilePairs_SortedBySharedBlockCountDescending()
    {
        var block1 = new DuplicateBlock(
            ["a", "b", "c", "d", "e"],
            [
                new FileLocation("/src/A.cs", 1, 5),
                new FileLocation("/src/B.cs", 1, 5)
            ]);
        var block2 = new DuplicateBlock(
            ["f", "g", "h", "i", "j"],
            [
                new FileLocation("/src/A.cs", 10, 14),
                new FileLocation("/src/B.cs", 10, 14)
            ]);
        var block3 = new DuplicateBlock(
            ["k", "l", "m", "n", "o"],
            [
                new FileLocation("/src/C.cs", 1, 5),
                new FileLocation("/src/D.cs", 1, 5)
            ]);
        var result = new ScanResult([block1, block2, block3], 4, 200, new Dictionary<string, int>());
        var pairs = FilePairGroup.ComputeFilePairs(result);

        Assert.Equal(2, pairs.Count);
        // A↔B has 2 blocks, C↔D has 1 block → A↔B first
        Assert.Equal(2, pairs[0].SharedBlocks.Count);
        Assert.Equal(1, pairs[1].SharedBlocks.Count);
    }

    [Fact]
    public void ComputeFilePairs_FileOrderIsAlphabetical()
    {
        var block = new DuplicateBlock(
            ["a", "b", "c", "d", "e"],
            [
                new FileLocation("/src/Z.cs", 1, 5),
                new FileLocation("/src/A.cs", 1, 5)
            ]);
        var result = new ScanResult([block], 2, 100, new Dictionary<string, int>());
        var pairs = FilePairGroup.ComputeFilePairs(result);

        Assert.Single(pairs);
        // FileA should be alphabetically first
        Assert.Equal("/src/A.cs", pairs[0].FileA);
        Assert.Equal("/src/Z.cs", pairs[0].FileB);
    }
}
