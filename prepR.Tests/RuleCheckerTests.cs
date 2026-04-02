namespace Prepr.Tests;

public class RuleCheckerTests
{
    private static readonly string[] SharedBlock =
    [
        "public void DoWork()",
        "{",
        "    var x = 1;",
        "    var y = 2;",
        "    Console.WriteLine(x + y);",
        "}"
    ];

    [Fact]
    public void Detect_Exact5LineDuplicate_ReturnsOneBlockWithTwoLocations()
    {
        using var tree = new TempFileTree();
        var lines = SharedBlock[..5]; // exactly 5 lines
        tree.AddFile("a.cs", lines);
        tree.AddFile("b.cs", lines);

        var files = new[] { Path.Combine(tree.RootPath, "a.cs"), Path.Combine(tree.RootPath, "b.cs") };
        var result = RuleChecker.Run(files);

        Assert.Single(result.Duplicates);
        Assert.Equal(2, result.Duplicates[0].Locations.Count);
    }

    [Fact]
    public void Detect_Only4MatchingLines_ReturnsNoDuplicates()
    {
        using var tree = new TempFileTree();
        var lines = SharedBlock[..4]; // below the 5-line threshold
        tree.AddFile("a.cs", lines);
        tree.AddFile("b.cs", lines);

        var files = new[] { Path.Combine(tree.RootPath, "a.cs"), Path.Combine(tree.RootPath, "b.cs") };
        var result = RuleChecker.Run(files);

        Assert.Empty(result.Duplicates);
    }

    [Fact]
    public void Detect_BlockLongerThan5Lines_DetectsOverlappingWindows()
    {
        using var tree = new TempFileTree();
        tree.AddFile("a.cs", SharedBlock); // 6 lines
        tree.AddFile("b.cs", SharedBlock);

        var files = new[] { Path.Combine(tree.RootPath, "a.cs"), Path.Combine(tree.RootPath, "b.cs") };
        var result = RuleChecker.Run(files);

        // Should produce a single block covering all 6 lines, not multiple overlapping 5-line blocks
        Assert.Single(result.Duplicates);
        Assert.Equal(6, result.Duplicates[0].Lines.Length);
        Assert.Equal(2, result.Duplicates[0].Locations.Count);
    }

    [Fact]
    public void Detect_WhitespaceOnlyLinesIgnored_StillFindsBlock()
    {
        using var tree = new TempFileTree();
        // Interleave blank lines — they should be skipped, and the 5 real lines should match
        var withBlanks = new[]
        {
            "public void DoWork()",
            "",
            "{",
            "    var x = 1;",
            "",
            "    var y = 2;",
            "    Console.WriteLine(x + y);",
        };
        tree.AddFile("a.cs", withBlanks);
        tree.AddFile("b.cs", withBlanks);

        var files = new[] { Path.Combine(tree.RootPath, "a.cs"), Path.Combine(tree.RootPath, "b.cs") };
        var result = RuleChecker.Run(files);

        Assert.Single(result.Duplicates);
        Assert.Equal(2, result.Duplicates[0].Locations.Count);
    }

    [Fact]
    public void Detect_NoDuplicates_ReturnsEmptyListWithCorrectCounts()
    {
        using var tree = new TempFileTree();
        tree.AddFile("a.cs", ["line 1", "line 2", "line 3", "line 4", "line 5"]);
        tree.AddFile("b.cs", ["other 1", "other 2", "other 3", "other 4", "other 5"]);

        var files = new[] { Path.Combine(tree.RootPath, "a.cs"), Path.Combine(tree.RootPath, "b.cs") };
        var result = RuleChecker.Run(files);

        Assert.Empty(result.Duplicates);
        Assert.Equal(2, result.TotalFilesScanned);
        Assert.Equal(10, result.TotalLinesScanned);
    }

    [Fact]
    public void Detect_MultipleDistinctBlocks_AllDetected()
    {
        using var tree = new TempFileTree();
        var block1 = new[] { "aaa", "bbb", "ccc", "ddd", "eee" };
        var block2 = new[] { "111", "222", "333", "444", "555" };

        tree.AddFile("a.cs", [.. block1, "---separator---", .. block2]);
        tree.AddFile("b.cs", [.. block1, "---other---", .. block2]);

        var files = new[] { Path.Combine(tree.RootPath, "a.cs"), Path.Combine(tree.RootPath, "b.cs") };
        var result = RuleChecker.Run(files);

        Assert.Equal(2, result.Duplicates.Count);
    }

    [Fact]
    public void Detect_SameBlockIn3Files_SingleBlockWith3Locations()
    {
        using var tree = new TempFileTree();
        var lines = SharedBlock[..5];
        tree.AddFile("a.cs", lines);
        tree.AddFile("b.cs", lines);
        tree.AddFile("c.cs", lines);

        var files = new[]
        {
            Path.Combine(tree.RootPath, "a.cs"),
            Path.Combine(tree.RootPath, "b.cs"),
            Path.Combine(tree.RootPath, "c.cs"),
        };
        var result = RuleChecker.Run(files);

        Assert.Single(result.Duplicates);
        Assert.Equal(3, result.Duplicates[0].Locations.Count);
    }

    [Fact]
    public void Detect_UnreadableFile_SkippedGracefully()
    {
        using var tree = new TempFileTree();
        tree.AddFile("a.cs", SharedBlock[..5]);

        var files = new[]
        {
            Path.Combine(tree.RootPath, "a.cs"),
            Path.Combine(tree.RootPath, "nonexistent.cs"), // does not exist
        };

        // Should not throw
        var result = RuleChecker.Run(files);
        Assert.Empty(result.Duplicates);
    }

    [Fact]
    public void Detect_LineNumbers_AreOneBasedAndAccountForBlankLines()
    {
        using var tree = new TempFileTree();
        // File with a blank line before the matching block
        var fileA = new[] { "", "alpha", "beta", "gamma", "delta", "epsilon" };
        var fileB = new[] { "alpha", "beta", "gamma", "delta", "epsilon" };

        tree.AddFile("a.cs", fileA);
        tree.AddFile("b.cs", fileB);

        var files = new[] { Path.Combine(tree.RootPath, "a.cs"), Path.Combine(tree.RootPath, "b.cs") };
        var result = RuleChecker.Run(files);

        Assert.Single(result.Duplicates);
        var locA = result.Duplicates[0].Locations.First(l => l.FilePath.EndsWith("a.cs"));
        var locB = result.Duplicates[0].Locations.First(l => l.FilePath.EndsWith("b.cs"));

        // In a.cs, the blank line is line 1, so the block starts at line 2
        Assert.Equal(2, locA.StartLine);
        // In b.cs, the block starts at line 1
        Assert.Equal(1, locB.StartLine);
    }

    [Fact]
    public void Detect_CustomThreshold3_FindsShorterBlocks()
    {
        using var tree = new TempFileTree();
        var lines = SharedBlock[..3]; // only 3 lines
        tree.AddFile("a.cs", lines);
        tree.AddFile("b.cs", lines);

        var files = new[] { Path.Combine(tree.RootPath, "a.cs"), Path.Combine(tree.RootPath, "b.cs") };
        var result = RuleChecker.Run(files, minConsecutiveLines: 3);

        Assert.Single(result.Duplicates);
        Assert.Equal(2, result.Duplicates[0].Locations.Count);
    }

    [Fact]
    public void Detect_CustomThreshold3_DefaultThresholdMissesIt()
    {
        using var tree = new TempFileTree();
        var lines = SharedBlock[..3]; // only 3 lines — below default threshold of 5
        tree.AddFile("a.cs", lines);
        tree.AddFile("b.cs", lines);

        var files = new[] { Path.Combine(tree.RootPath, "a.cs"), Path.Combine(tree.RootPath, "b.cs") };
        var result = RuleChecker.Run(files); // default = 5

        Assert.Empty(result.Duplicates);
    }

    [Fact]
    public void Detect_HigherThreshold_IgnoresShorterBlocks()
    {
        using var tree = new TempFileTree();
        tree.AddFile("a.cs", SharedBlock[..5]); // exactly 5 lines
        tree.AddFile("b.cs", SharedBlock[..5]);

        var files = new[] { Path.Combine(tree.RootPath, "a.cs"), Path.Combine(tree.RootPath, "b.cs") };
        var result = RuleChecker.Run(files, minConsecutiveLines: 10);

        Assert.Empty(result.Duplicates);
    }

    [Fact]
    public void NestingDepth_InterpolatedString_BracesNotCounted()
    {
        using var tree = new TempFileTree();
        var lines = new[]
        {
            "public class C",
            "{",
            "    public void M()",
            "    {",
            "        writer.WriteLine($\"{CsvEscape(path)},{v.MaxDepth},{v.Limit}\");",
            "    }",
            "}",
        };
        tree.AddFile("a.cs", lines);

        var files = new[] { Path.Combine(tree.RootPath, "a.cs") };
        var result = RuleChecker.Run(files);

        var (maxDepth, _) = result.FileMaxNestingDepths[files[0]];
        Assert.Equal(2, maxDepth);
    }

    [Fact]
    public void NestingDepth_VerbatimString_BracesNotCounted()
    {
        using var tree = new TempFileTree();
        var lines = new[]
        {
            "public class C",
            "{",
            "    var s = @\"some { braces } here\";",
            "}",
        };
        tree.AddFile("a.cs", lines);

        var files = new[] { Path.Combine(tree.RootPath, "a.cs") };
        var result = RuleChecker.Run(files);

        var (maxDepth, _) = result.FileMaxNestingDepths[files[0]];
        Assert.Equal(1, maxDepth);
    }

    [Fact]
    public void NestingDepth_CharLiteral_BracesNotCounted()
    {
        using var tree = new TempFileTree();
        var lines = new[]
        {
            "public class C",
            "{",
            "    char a = '{';",
            "    char b = '}';",
            "}",
        };
        tree.AddFile("a.cs", lines);

        var files = new[] { Path.Combine(tree.RootPath, "a.cs") };
        var result = RuleChecker.Run(files);

        var (maxDepth, _) = result.FileMaxNestingDepths[files[0]];
        Assert.Equal(1, maxDepth);
    }

    [Fact]
    public void NestingDepth_LineComment_BracesNotCounted()
    {
        using var tree = new TempFileTree();
        var lines = new[]
        {
            "public class C",
            "{",
            "    // { not a scope }",
            "}",
        };
        tree.AddFile("a.cs", lines);

        var files = new[] { Path.Combine(tree.RootPath, "a.cs") };
        var result = RuleChecker.Run(files);

        var (maxDepth, _) = result.FileMaxNestingDepths[files[0]];
        Assert.Equal(1, maxDepth);
    }

    [Fact]
    public void NestingDepth_BlockComment_BracesNotCounted()
    {
        using var tree = new TempFileTree();
        var lines = new[]
        {
            "public class C",
            "{",
            "    /* { not a scope",
            "       } still comment */",
            "}",
        };
        tree.AddFile("a.cs", lines);

        var files = new[] { Path.Combine(tree.RootPath, "a.cs") };
        var result = RuleChecker.Run(files);

        var (maxDepth, _) = result.FileMaxNestingDepths[files[0]];
        Assert.Equal(1, maxDepth);
    }

    [Fact]
    public void NestingDepth_RealBraces_StillCounted()
    {
        using var tree = new TempFileTree();
        var lines = new[]
        {
            "public class C",
            "{",
            "    public void M()",
            "    {",
            "        if (true)",
            "        {",
            "        }",
            "    }",
            "}",
        };
        tree.AddFile("a.cs", lines);

        var files = new[] { Path.Combine(tree.RootPath, "a.cs") };
        var result = RuleChecker.Run(files);

        var (maxDepth, _) = result.FileMaxNestingDepths[files[0]];
        Assert.Equal(3, maxDepth);
    }
}
