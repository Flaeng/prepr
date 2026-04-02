using System.CommandLine;

namespace Prepr;

public static class CliSymbols
{
    public static readonly Argument<DirectoryInfo> PathArgument = new("path")
    {
        Description = "The root folder to scan for duplicate blocks. Defaults to the current directory.",
        Arity = ArgumentArity.ZeroOrOne,
        DefaultValueFactory = _ => new DirectoryInfo(Directory.GetCurrentDirectory())
    };

    public static readonly Option<string[]> ExtensionsOption = new("--extensions")
    {
        Description = "File extensions to include (e.g. .cs .ts .js). Defaults to common text file extensions.",
        AllowMultipleArgumentsPerToken = true
    };

    public static readonly Option<string[]> ExcludeExtensionsOption = new("--exclude-extensions")
    {
        Description = "File extensions to exclude (e.g. .json .xml).",
        AllowMultipleArgumentsPerToken = true
    };

    public static readonly Option<string[]> IgnorePathsOption = new("--ignore-paths")
    {
        Description = "Directory names or glob patterns to ignore (e.g. bin obj .git **/*.generated.cs).",
        AllowMultipleArgumentsPerToken = true
    };

    public static readonly Option<string[]> OutputOption = new("--output")
    {
        Description = "Output format(s): console, html, md, csv, prompt. Defaults to console.",
        AllowMultipleArgumentsPerToken = true
    };

    public static readonly Option<string?> OutputFileOption = new("--output-file")
    {
        Description = "Base path for file outputs (extension auto-appended). Defaults to report.prepr in the current directory."
    };

    public static readonly Option<int?> ThresholdOption = new("--threshold")
    {
        Description = "Minimum number of consecutive duplicate lines to detect. Default: 5."
    };

    public static readonly Option<bool> InitOption = new("--init")
    {
        Description = "Generate a default .preprrc configuration file in the target directory."
    };

    public static readonly Option<string?> VerbosityOption = new("--verbosity")
    {
        Description = "Output verbosity: quiet, normal, detailed. Default: normal."
    };

    public static readonly Option<bool> VerboseOption = new("--verbose")
    {
        Description = "Shorthand for --verbosity detailed."
    };

    public static readonly Option<bool> QuietOption = new("--quiet")
    {
        Description = "Shorthand for --verbosity quiet."
    };

    public static readonly Option<int?> MaxDuplicatesOption = new("--max-duplicates")
    {
        Description = "Maximum allowed duplicate blocks. Exits with code 2 if exceeded. Use 0 to fail on any duplicates."
    };

    public static readonly Option<bool> CacheOption = new("--cache")
    {
        Description = "Enable caching of scan results."
    };

    public static readonly Option<int?> MaxFileLinesOption = new("--max-file-lines")
    {
        Description = "Maximum allowed lines per file. Files exceeding this limit are reported. Exits with code 2 if any file exceeds the limit."
    };

    public static readonly Option<int?> MaxIndentationOption = new("--max-indentation")
    {
        Description = "Maximum allowed brace nesting depth per file. Files exceeding this limit are reported. Exits with code 2 if any file exceeds the limit."
    };

    public static readonly Option<bool?> EarlyReturnOption = new("--early-return")
    {
        Description = "Enable or disable the early return rule. Detects else blocks that could be replaced with guard clauses. Exits with code 2 if violations are found."
    };

    public static readonly Option<int?> MaxTechDebtScoreOption = new("--max-tech-debt-score")
    {
        Description = "Maximum allowed tech debt score (0–100). Exits with code 2 if exceeded."
    };
    public static readonly Option<int?> MinCommentDensityOption = new("--min-comment-density")
    {
        Description = "Minimum required comment density (%). Files below this threshold are reported. Exits with code 2 if any file is below the limit."
    };

    public static readonly Option<int?> MaxCommentDensityOption = new("--max-comment-density")
    {
        Description = "Maximum allowed comment density (%). Files above this threshold are reported. Exits with code 2 if any file exceeds the limit."
    };}
