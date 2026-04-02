using System.CommandLine;

using static Prepr.CliSymbols;

namespace Prepr.Models;

public class RunOptions
{
    public string[]? Extensions { get; set; }
    public string[]? ExcludeExtensions { get; set; }
    public string[]? IgnorePaths { get; set; }
    public string[] Outputs { get; set; } = [];
    public string? OutputFile { get; set; }
    public int Threshold { get; set; }
    public int? MaxDuplicates { get; set; }
    public int? MaxTechDebtScore { get; set; }
    public bool UseCache { get; set; }

    public static RunOptions Create(PreprConfig config, ParseResult parse)
    {
        var extensions = parse.GetResult(ExtensionsOption) is not null
            ? parse.GetValue(ExtensionsOption) : config.IncludeExtensions;

        var excludeExtensions = parse.GetResult(ExcludeExtensionsOption) is not null
            ? parse.GetValue(ExcludeExtensionsOption) : config.ExcludeExtensions;

        var ignorePaths = parse.GetResult(IgnorePathsOption) is not null
            ? parse.GetValue(IgnorePathsOption) : config.IgnorePaths;

        var outputs = parse.GetResult(OutputOption) is not null
            ? (parse.GetValue(OutputOption) ?? ["console"]) : (config.Output ?? ["console"]);

        var outputFile = parse.GetResult(OutputFileOption) is not null
            ? parse.GetValue(OutputFileOption) : config.OutputFile;

        var threshold = parse.GetResult(ThresholdOption) is not null
            ? (parse.GetValue(ThresholdOption) ?? 5) : (config.Threshold ?? 5);

        var maxDuplicates = parse.GetResult(MaxDuplicatesOption) is not null
            ? parse.GetValue(MaxDuplicatesOption) : config.MaxDuplicates;

        var maxTechDebtScore = parse.GetResult(MaxTechDebtScoreOption) is not null
            ? parse.GetValue(MaxTechDebtScoreOption) : config.MaxTechDebtScore;

        var useCache = parse.GetValue(CacheOption);

        return new RunOptions
        {
            Extensions = extensions is { Length: > 0 } ? extensions : null,
            ExcludeExtensions = excludeExtensions,
            IgnorePaths = ignorePaths,
            Outputs = outputs,
            OutputFile = outputFile,
            Threshold = threshold,
            MaxDuplicates = maxDuplicates,
            MaxTechDebtScore = maxTechDebtScore,
            UseCache = useCache
        };
    }
}
