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
    MagicNumberRule? MagicNumberRule = null,
    MagicStringRule? MagicStringRule = null,
    int TechDebtWeightDuplication = 25,
    int TechDebtWeightLineLimit = 13,
    int TechDebtWeightIndentation = 13,
    int TechDebtWeightEarlyReturn = 13,
    int TechDebtWeightCommentDensity = 20,
    int TechDebtWeightMagicNumber = 8,
    int TechDebtWeightMagicString = 8)
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

        var maxMagicNumbersCliValue = parse.GetResult(MaxMagicNumbersOption) is not null
            ? parse.GetValue(MaxMagicNumbersOption) : null;
        var magicNumberRule = new MagicNumberRule(config.MaxMagicNumbers, maxMagicNumbersCliValue);

        var maxMagicStringsCliValue = parse.GetResult(MaxMagicStringsOption) is not null
            ? parse.GetValue(MaxMagicStringsOption) : null;
        var magicStringRule = new MagicStringRule(config.MaxMagicStrings, maxMagicStringsCliValue);

        return new ReportOptions(
            verbosity,
            config.HighSeverityThreshold ?? 50,
            config.MediumSeverityThreshold ?? 25,
            lineLimitRule.HasRules ? lineLimitRule : null,
            indentationRule.HasRules ? indentationRule : null,
            earlyReturn,
            minCommentDensityRule.HasRules ? minCommentDensityRule : null,
            maxCommentDensityRule.HasRules ? maxCommentDensityRule : null,
            magicNumberRule.HasRules ? magicNumberRule : null,
            magicStringRule.HasRules ? magicStringRule : null,
            config.TechDebtWeightDuplication ?? 25,
            config.TechDebtWeightLineLimit ?? 13,
            config.TechDebtWeightIndentation ?? 13,
            config.TechDebtWeightEarlyReturn ?? 13,
            config.TechDebtWeightCommentDensity ?? 20,
            config.TechDebtWeightMagicNumber ?? 8,
            config.TechDebtWeightMagicString ?? 8);
    }

    private static Verbosity ParseVerbosity(string? value) => value?.ToLowerInvariant() switch
    {
        "quiet" => Verbosity.Quiet,
        "detailed" => Verbosity.Detailed,
        _ => Verbosity.Normal
    };
}
