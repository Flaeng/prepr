namespace Prepr.Tests;

public class FileDiscoveryTests
{
    [Fact]
    public void DiscoverFiles_RecursivelyFindsNestedFiles()
    {
        using var tree = new TempFileTree();
        tree.AddFile("root.cs", "// root");
        tree.AddFile("sub/nested.cs", "// nested");
        tree.AddFile("sub/deep/deeper.cs", "// deeper");

        var files = new FileDiscovery(tree.RootPath, new RunOptions()).DiscoverFiles();

        Assert.Equal(3, files.Count);
    }

    [Fact]
    public void DiscoverFiles_DefaultExtensions_ExcludesBinaryFiles()
    {
        using var tree = new TempFileTree();
        tree.AddFile("code.cs", "// c#");
        tree.AddFile("app.exe", "binary");
        tree.AddFile("lib.dll", "binary");

        var files = new FileDiscovery(tree.RootPath, new RunOptions()).DiscoverFiles();

        Assert.Single(files);
        Assert.EndsWith(".cs", files[0]);
    }

    [Fact]
    public void DiscoverFiles_CustomExtensions_OnlyReturnsSpecified()
    {
        using var tree = new TempFileTree();
        tree.AddFile("code.cs", "// c#");
        tree.AddFile("script.ts", "// ts");
        tree.AddFile("page.html", "<!-- html -->");

        var files = new FileDiscovery(tree.RootPath, new RunOptions { Extensions = [".ts"] }).DiscoverFiles();

        Assert.Single(files);
        Assert.EndsWith(".ts", files[0]);
    }

    [Fact]
    public void DiscoverFiles_ExtensionNormalization_WorksWithoutDot()
    {
        using var tree = new TempFileTree();
        tree.AddFile("code.cs", "// c#");
        tree.AddFile("script.ts", "// ts");

        var files = new FileDiscovery(tree.RootPath, new RunOptions { Extensions = ["cs"] }).DiscoverFiles(); // no leading dot

        Assert.Single(files);
        Assert.EndsWith(".cs", files[0]);
    }

    [Fact]
    public void DiscoverFiles_EmptyDirectory_ReturnsEmptyList()
    {
        using var tree = new TempFileTree();

        var files = new FileDiscovery(tree.RootPath, new RunOptions()).DiscoverFiles();

        Assert.Empty(files);
    }

    [Fact]
    public void DiscoverFiles_MultipleDefaultExtensions_AllFound()
    {
        using var tree = new TempFileTree();
        tree.AddFile("app.cs", "// c#");
        tree.AddFile("app.ts", "// ts");
        tree.AddFile("app.py", "# python");
        tree.AddFile("app.json", "{}");

        var files = new FileDiscovery(tree.RootPath, new RunOptions()).DiscoverFiles();

        Assert.Equal(4, files.Count);
    }

    [Fact]
    public void DiscoverFiles_ExcludeExtensions_FiltersOut()
    {
        using var tree = new TempFileTree();
        tree.AddFile("code.cs", "// c#");
        tree.AddFile("data.json", "{}");
        tree.AddFile("config.xml", "<root/>");

        var files = new FileDiscovery(tree.RootPath, new RunOptions { ExcludeExtensions = [".json", ".xml"] }).DiscoverFiles();

        Assert.Single(files);
        Assert.EndsWith(".cs", files[0]);
    }

    [Fact]
    public void DiscoverFiles_IgnorePaths_SkipsDirectories()
    {
        using var tree = new TempFileTree();
        tree.AddFile("src/code.cs", "// src");
        tree.AddFile("bin/output.cs", "// bin");
        tree.AddFile("obj/temp.cs", "// obj");
        tree.AddFile("src/sub/bin/nested.cs", "// nested bin");

        var files = new FileDiscovery(tree.RootPath, new RunOptions { IgnorePaths = ["bin", "obj"] }).DiscoverFiles();

        Assert.Single(files);
        Assert.Contains("src", files[0]);
        Assert.DoesNotContain("bin", files[0]);
    }

    [Fact]
    public void DiscoverFiles_IgnorePaths_GlobPattern()
    {
        using var tree = new TempFileTree();
        tree.AddFile("code.cs", "// keep");
        tree.AddFile("code.generated.cs", "// skip");
        tree.AddFile("sub/other.generated.cs", "// skip nested");

        var files = new FileDiscovery(tree.RootPath, new RunOptions { IgnorePaths = ["**/*.generated.cs"] }).DiscoverFiles();

        Assert.Single(files);
        Assert.EndsWith("code.cs", files[0]);
    }

    [Fact]
    public void DiscoverFiles_CombinedFilters_AllApplied()
    {
        using var tree = new TempFileTree();
        tree.AddFile("src/app.cs", "// keep");
        tree.AddFile("src/app.json", "{}");         // excluded by extension
        tree.AddFile("bin/app.cs", "// bin");         // excluded by ignore path
        tree.AddFile("src/app.generated.cs", "// gen"); // excluded by glob

        var files = new FileDiscovery(tree.RootPath, new RunOptions { ExcludeExtensions = [".json"], IgnorePaths = ["bin", "**/*.generated.cs"] }).DiscoverFiles();

        Assert.Single(files);
        Assert.EndsWith("app.cs", files[0]);
        Assert.Contains("src", files[0]);
    }
}
