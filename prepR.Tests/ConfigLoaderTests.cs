using System.Text.Json;

namespace Prepr.Tests;

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
    public void LoadConfig_NoConfigFile_ReturnsDefaults()
    {
        using var tree = new TempFileTree();

        var config = ConfigLoader.LoadConfig(tree.RootPath);

        Assert.Equal(5, config.Threshold);
        Assert.NotNull(config.Output);
        Assert.Equal("report.prepr", config.OutputFile);
    }

    [Fact]
    public void LoadConfig_InvalidJson_ReturnsDefaultConfig()
    {
        using var tree = new TempFileTree();
        tree.AddFile(".preprrc", "{ this is not valid json }");

        var config = ConfigLoader.LoadConfig(tree.RootPath);

        Assert.Equal(5, config.Threshold);
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
        var config = JsonSerializer.Deserialize<PreprConfig>(PreprConfig.DefaultConfigJson);
        Assert.NotNull(config);
        Assert.Equal(5, config.Threshold);
        Assert.Equal("report.prepr", config.OutputFile);
        Assert.Equal(["console", "html", "md", "csv", "prompt"], config.Output);
    }

    [Fact]
    public void MergeWithDefaults_MissingProperty_KeepsDefault()
    {
        var result = ConfigLoader.MergeWithDefaults("""{ "threshold": 10 }""");

        Assert.NotNull(result);
        Assert.Equal(10, result.Threshold);
        // Properties not in user JSON keep their defaults
        Assert.Equal("report.prepr", result.OutputFile);
        Assert.Equal(["console", "html", "md", "csv", "prompt"], result.Output);
        Assert.True(result.EarlyReturn);
        Assert.Equal(50, result.HighSeverityThreshold);
        Assert.Equal(25, result.MediumSeverityThreshold);
        Assert.NotNull(result.MaxFileLines);
        Assert.Equal(200, result.MaxFileLines!["*"]);
        Assert.NotNull(result.MaxIndentation);
        Assert.Equal(4, result.MaxIndentation!["*"]);
        Assert.NotNull(result.MaxCommentDensity);
        Assert.NotNull(result.MaxMagicNumbers);
        Assert.NotNull(result.MaxMagicStrings);
    }

    [Fact]
    public void MergeWithDefaults_ExplicitNull_DisablesRule()
    {
        var json = """
        {
            "maxCommentDensity": null,
            "maxMagicNumbers": null,
            "maxMagicStrings": null,
            "maxFileLines": null,
            "maxIndentation": null,
            "earlyReturn": false,
            "maxDuplicates": null,
            "maxTechDebtScore": null
        }
        """;

        var result = ConfigLoader.MergeWithDefaults(json);

        Assert.NotNull(result);
        Assert.Null(result.MaxCommentDensity);
        Assert.Null(result.MaxMagicNumbers);
        Assert.Null(result.MaxMagicStrings);
        Assert.Null(result.MaxFileLines);
        Assert.Null(result.MaxIndentation);
        Assert.False(result.EarlyReturn);
        Assert.Null(result.MaxDuplicates);
        Assert.Null(result.MaxTechDebtScore);
    }

    [Fact]
    public void MergeWithDefaults_UserOverridesOnly_DefaultsPreserved()
    {
        var json = """
        {
            "maxFileLines": { "*.cs": 300 },
            "ignorePaths": ["node_modules"]
        }
        """;

        var result = ConfigLoader.MergeWithDefaults(json);

        Assert.NotNull(result);
        // Overridden values
        Assert.Equal(300, result.MaxFileLines!["*.cs"]);
        Assert.Single(result.MaxFileLines);
        Assert.Equal(["node_modules"], result.IgnorePaths);
        // Untouched defaults
        Assert.Equal(5, result.Threshold);
        Assert.True(result.EarlyReturn);
        Assert.NotNull(result.MaxCommentDensity);
        Assert.NotNull(result.MaxMagicNumbers);
        Assert.Equal(25, result.TechDebtWeightDuplication);
    }

    [Fact]
    public void LoadConfig_PartialConfig_MergesWithDefaults()
    {
        using var tree = new TempFileTree();
        tree.AddFile(".preprrc", """{ "threshold": 3 }""");

        var config = ConfigLoader.LoadConfig(tree.RootPath);

        Assert.Equal(3, config.Threshold);
        // Defaults filled in for missing properties
        Assert.Equal("report.prepr", config.OutputFile);
        Assert.True(config.EarlyReturn);
        Assert.NotNull(config.MaxFileLines);
        Assert.NotNull(config.MaxCommentDensity);
        Assert.NotNull(config.IgnorePaths);
    }
}
