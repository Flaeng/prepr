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
    int TechDebtWeightDuplication = 40,
    int TechDebtWeightLineLimit = 20,
    int TechDebtWeightIndentation = 20,
    int TechDebtWeightEarlyReturn = 20)
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

        return new ReportOptions(
            verbosity,
            config.HighSeverityThreshold ?? 50,
            config.MediumSeverityThreshold ?? 25,
            lineLimitRule.HasRules ? lineLimitRule : null,
            indentationRule.HasRules ? indentationRule : null,
            earlyReturn,
            config.TechDebtWeightDuplication ?? 40,
            config.TechDebtWeightLineLimit ?? 20,
            config.TechDebtWeightIndentation ?? 20,
            config.TechDebtWeightEarlyReturn ?? 20);
    }

    private static Verbosity ParseVerbosity(string? value) => value?.ToLowerInvariant() switch
    {
        "quiet" => Verbosity.Quiet,
        "detailed" => Verbosity.Detailed,
        _ => Verbosity.Normal
    };
}
