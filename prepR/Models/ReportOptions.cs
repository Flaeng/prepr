using System.CommandLine;

using static Prepr.CliSymbols;

namespace Prepr.Models;

public record ReportOptions(
    Verbosity Verbosity = Verbosity.Normal,
    int HighSeverityThreshold = 50,
    int MediumSeverityThreshold = 25,
    LineLimitRule? LineLimitRule = null,
    IndentationRule? IndentationRule = null,
    bool EarlyReturn = false,
    MinCommentDensityRule? MinCommentDensityRule = null,
    MaxCommentDensityRule? MaxCommentDensityRule = null,
    int TechDebtWeightDuplication = 30,
    int TechDebtWeightLineLimit = 15,
    int TechDebtWeightIndentation = 15,
    int TechDebtWeightEarlyReturn = 15,
    int TechDebtWeightCommentDensity = 25)
{
    public static ReportOptions Create(
        PreprConfig config,
        ParseResult parse,
        TextWriter? errorWriter = null)
    {
        var explicitVerbosity = parse.GetResult(VerbosityOption) is not null;
        var verbosityStr = explicitVerbosity ? parse.GetValue(VerbosityOption) : config.Verbosity;
        var verbose = parse.GetValue(VerboseOption);
        var quiet = parse.GetValue(QuietOption);

        Verbosity verbosity;
        if (explicitVerbosity)
        {
            verbosity = ParseVerbosity(verbosityStr);
        }
        else if (quiet)
        {
            if (verbose)
                errorWriter?.WriteLine("Warning: Both --verbose and --quiet specified. Using --quiet.");
            verbosity = Verbosity.Quiet;
        }
        else if (verbose)
        {
            verbosity = Verbosity.Detailed;
        }
        else
        {
            verbosity = ParseVerbosity(verbosityStr);
        }

        var maxFileLinesCliValue = parse.GetResult(MaxFileLinesOption) is not null
            ? parse.GetValue(MaxFileLinesOption) : null;
        var lineLimitRule = new LineLimitRule(config.MaxFileLines, maxFileLinesCliValue);

        var maxIndentationCliValue = parse.GetResult(MaxIndentationOption) is not null
            ? parse.GetValue(MaxIndentationOption) : null;
        var indentationRule = new IndentationRule(config.MaxIndentation, maxIndentationCliValue);

        var earlyReturnExplicit = parse.GetResult(EarlyReturnOption) is not null;
        var earlyReturn = earlyReturnExplicit
            ? parse.GetValue(EarlyReturnOption) ?? true
            : config.EarlyReturn ?? true;

        var minCommentDensityCliValue = parse.GetResult(MinCommentDensityOption) is not null
            ? parse.GetValue(MinCommentDensityOption) : null;
        var minCommentDensityRule = new MinCommentDensityRule(config.MinCommentDensity, minCommentDensityCliValue);

        var maxCommentDensityCliValue = parse.GetResult(MaxCommentDensityOption) is not null
            ? parse.GetValue(MaxCommentDensityOption) : null;
        var maxCommentDensityRule = new MaxCommentDensityRule(config.MaxCommentDensity, maxCommentDensityCliValue);

        return new ReportOptions(
            verbosity,
            config.HighSeverityThreshold ?? 50,
            config.MediumSeverityThreshold ?? 25,
            lineLimitRule.HasRules ? lineLimitRule : null,
            indentationRule.HasRules ? indentationRule : null,
            earlyReturn,
            minCommentDensityRule.HasRules ? minCommentDensityRule : null,
            maxCommentDensityRule.HasRules ? maxCommentDensityRule : null,
            config.TechDebtWeightDuplication ?? 30,
            config.TechDebtWeightLineLimit ?? 15,
            config.TechDebtWeightIndentation ?? 15,
            config.TechDebtWeightEarlyReturn ?? 15,
            config.TechDebtWeightCommentDensity ?? 25);
    }

    private static Verbosity ParseVerbosity(string? value) => value?.ToLowerInvariant() switch
    {
        "quiet" => Verbosity.Quiet,
        "detailed" => Verbosity.Detailed,
        _ => Verbosity.Normal
    };
}
