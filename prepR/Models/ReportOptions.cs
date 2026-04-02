using System.CommandLine;

using static Prepr.CliSymbols;

namespace Prepr.Models;

public record ReportOptions(
    Verbosity Verbosity = Verbosity.Normal,
    int HighSeverityThreshold = 50,
    int MediumSeverityThreshold = 25,
    LineLimitRule? LineLimitRule = null)
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

        return new ReportOptions(
            verbosity,
            config.HighSeverityThreshold ?? 50,
            config.MediumSeverityThreshold ?? 25,
            lineLimitRule.HasRules ? lineLimitRule : null);
    }

    private static Verbosity ParseVerbosity(string? value) => value?.ToLowerInvariant() switch
    {
        "quiet" => Verbosity.Quiet,
        "detailed" => Verbosity.Detailed,
        _ => Verbosity.Normal
    };
}
