using System.CommandLine;
using System.CommandLine.Parsing;
using static prepr.CliSymbols;

namespace prepr;

public record RunOptions(
    string[]? Extensions,
    string[]? ExcludeExtensions,
    string[]? IgnorePaths,
    string[] Outputs,
    string? OutputFile,
    int Threshold,
    int? MaxDuplicates,
    bool UseCache)
{
    public static RunOptions Create(preprConfig config, ParseResult parse)
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
        var useCache = parse.GetValue(CacheOption);

        return new RunOptions(
            extensions is { Length: > 0 } ? extensions : null,
            excludeExtensions,
            ignorePaths,
            outputs,
            outputFile,
            threshold,
            maxDuplicates,
            useCache);
    }
}
