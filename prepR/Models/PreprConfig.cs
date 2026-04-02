using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prepr.Models;

public class PreprConfig
{
    [JsonPropertyName("output")]
    public string[]? Output { get; set; }

    [JsonPropertyName("outputFile")]
    public string? OutputFile { get; set; }

    [JsonPropertyName("threshold")]
    public int? Threshold { get; set; }

    [JsonPropertyName("includeExtensions")]
    public string[]? IncludeExtensions { get; set; }

    [JsonPropertyName("excludeExtensions")]
    public string[]? ExcludeExtensions { get; set; }

    [JsonPropertyName("ignorePaths")]
    public string[]? IgnorePaths { get; set; }

    [JsonPropertyName("verbosity")]
    public string? Verbosity { get; set; }

    [JsonPropertyName("highSeverityThreshold")]
    public int? HighSeverityThreshold { get; set; }

    [JsonPropertyName("mediumSeverityThreshold")]
    public int? MediumSeverityThreshold { get; set; }

    [JsonPropertyName("maxDuplicates")]
    public int? MaxDuplicates { get; set; }

    [JsonPropertyName("maxFileLines")]
    public Dictionary<string, int>? MaxFileLines { get; set; }

    [JsonPropertyName("maxIndentation")]
    public Dictionary<string, int>? MaxIndentation { get; set; }

    [JsonPropertyName("earlyReturn")]
    public bool? EarlyReturn { get; set; }

    [JsonPropertyName("techDebtWeightDuplication")]
    public int? TechDebtWeightDuplication { get; set; }

    [JsonPropertyName("techDebtWeightLineLimit")]
    public int? TechDebtWeightLineLimit { get; set; }

    [JsonPropertyName("techDebtWeightIndentation")]
    public int? TechDebtWeightIndentation { get; set; }

    [JsonPropertyName("techDebtWeightEarlyReturn")]
    public int? TechDebtWeightEarlyReturn { get; set; }

    [JsonPropertyName("maxTechDebtScore")]
    public int? MaxTechDebtScore { get; set; }

    public static readonly string DefaultConfigJson = JsonSerializer.Serialize(new PreprConfig
    {
        Output = ["console", "html", "md", "csv", "prompt"],
        OutputFile = "report.prepr",
        Threshold = 5,
        IncludeExtensions = [],
        ExcludeExtensions = [],
        IgnorePaths = ["node_modules", "bin", "obj", ".git", ".prepr-cache"],
        Verbosity = "normal",
        HighSeverityThreshold = 50,
        MediumSeverityThreshold = 25,
        MaxDuplicates = null,
        MaxFileLines = new Dictionary<string, int> { { "*", 200 } },
        MaxIndentation = new Dictionary<string, int> { { "*", 4 } },
        EarlyReturn = true,
        TechDebtWeightDuplication = 40,
        TechDebtWeightLineLimit = 20,
        TechDebtWeightIndentation = 20,
        TechDebtWeightEarlyReturn = 20,
        MaxTechDebtScore = null
    }, new JsonSerializerOptions { WriteIndented = true });
}
