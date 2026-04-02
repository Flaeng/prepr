using System.Text.Json;
using prepr;

namespace prepr.Tests;

public class ConfigLoaderTests
{
    [Fact]
    public void LoadConfig_ConfigInScannedDirectory_ReturnsValues()
    {
        using var tree = new TempFileTree();
        tree.AddFile(".preprrc", """{"threshold": 3, "output": ["html"]}""");

        var config = ConfigLoader.LoadConfig(tree.RootPath);

        Assert.Equal(3, config.Threshold);
        Assert.Equal(["html"], config.Output);
    }

    [Fact]
    public void LoadConfig_ConfigInParentDirectory_Found()
    {
        using var tree = new TempFileTree();
        tree.AddFile(".preprrc", """{"threshold": 10}""");
        var childDir = Path.Combine(tree.RootPath, "child");
        Directory.CreateDirectory(childDir);

        var config = ConfigLoader.LoadConfig(childDir);

        Assert.Equal(10, config.Threshold);
    }

    [Fact]
    public void LoadConfig_NoConfigFile_ReturnsAllNulls()
    {
        using var tree = new TempFileTree();

        var config = ConfigLoader.LoadConfig(tree.RootPath);

        Assert.Null(config.Threshold);
        Assert.Null(config.Output);
        Assert.Null(config.OutputFile);
        Assert.Null(config.IncludeExtensions);
        Assert.Null(config.ExcludeExtensions);
        Assert.Null(config.IgnorePaths);
    }

    [Fact]
    public void LoadConfig_InvalidJson_ReturnsDefaultConfig()
    {
        using var tree = new TempFileTree();
        tree.AddFile(".preprrc", "{ this is not valid json }");

        var config = ConfigLoader.LoadConfig(tree.RootPath);

        Assert.Null(config.Threshold);
    }

    [Fact]
    public void LoadConfig_ThresholdZero_ReturnsNullThresholdWithWarning()
    {
        using var tree = new TempFileTree();
        tree.AddFile(".preprrc", """{"threshold": 0}""");

        var config = ConfigLoader.LoadConfig(tree.RootPath);

        Assert.Null(config.Threshold);
    }

    [Fact]
    public void LoadConfig_NegativeThreshold_ReturnsNullThresholdWithWarning()
    {
        using var tree = new TempFileTree();
        tree.AddFile(".preprrc", """{"threshold": -1}""");

        var config = ConfigLoader.LoadConfig(tree.RootPath);

        Assert.Null(config.Threshold);
    }

    [Fact]
    public void LoadConfig_AllOptions_ParsedCorrectly()
    {
        using var tree = new TempFileTree();
        var json = """
        {
            "output": ["console", "csv"],
            "outputFile": "my-report",
            "threshold": 7,
            "includeExtensions": [".cs", ".ts"],
            "excludeExtensions": [".json"],
            "ignorePaths": ["bin", "obj", "*.generated.cs"]
        }
        """;
        tree.AddFile(".preprrc", json);

        var config = ConfigLoader.LoadConfig(tree.RootPath);

        Assert.Equal(["console", "csv"], config.Output);
        Assert.Equal("my-report", config.OutputFile);
        Assert.Equal(7, config.Threshold);
        Assert.Equal([".cs", ".ts"], config.IncludeExtensions);
        Assert.Equal([".json"], config.ExcludeExtensions);
        Assert.Equal(["bin", "obj", "*.generated.cs"], config.IgnorePaths);
    }

    [Fact]
    public void LoadConfig_CaseInsensitivePropertyNames()
    {
        using var tree = new TempFileTree();
        tree.AddFile(".preprrc", """{"Threshold": 8, "OUTPUT": ["md"]}""");

        var config = ConfigLoader.LoadConfig(tree.RootPath);

        Assert.Equal(8, config.Threshold);
        Assert.Equal(["md"], config.Output);
    }

    [Fact]
    public void DefaultConfigJson_IsValidJson()
    {
        var config = JsonSerializer.Deserialize<preprConfig>(preprConfig.DefaultConfigJson);
        Assert.NotNull(config);
        Assert.Equal(5, config.Threshold);
        Assert.Equal("report.prepr", config.OutputFile);
        Assert.Equal(["console", "html", "md", "csv", "prompt"], config.Output);
    }
}
