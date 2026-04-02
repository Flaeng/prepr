using System.CommandLine;

using static Prepr.CliSymbols;

var rootCommand = new RootCommand("prepr \u2014 Detect duplicated code blocks across files in a folder.")
{
    PathArgument,
    ExtensionsOption,
    ExcludeExtensionsOption,
    IgnorePathsOption,
    OutputOption,
    OutputFileOption,
    ThresholdOption,
    InitOption,
    VerbosityOption,
    VerboseOption,
    QuietOption,
    MaxDuplicatesOption,
    CacheOption,
    MaxFileLinesOption,
    MaxIndentationOption,
    EarlyReturnOption,
    MaxTechDebtScoreOption,
    MinCommentDensityOption,
    MaxCommentDensityOption
};

rootCommand.SetAction((ParseResult parse) =>
{
    var path = parse.GetValue(PathArgument)!;
    var init = parse.GetValue(InitOption);

    if (init)
    {
        ConfigInitializer.TryInit(path.FullName);
        return;
    }

    if (!path.Exists)
    {
        Console.Error.WriteLine($"Error: Directory '{path.FullName}' does not exist.");
        Environment.ExitCode = 1;
        return;
    }

    // Load config file (walks up from scanned directory)
    var config = ConfigLoader.LoadConfig(path.FullName);

    var runOptions = RunOptions.Create(config, parse);

    var reportOptions = ReportOptions.Create(config, parse, Console.Error);

    var showProgress = reportOptions.Verbosity != Verbosity.Quiet && !Console.IsErrorRedirected;
    TextWriter? progressWriter = showProgress ? Console.Error : null;

    var files = new FileDiscovery(path.FullName, runOptions, progressWriter).DiscoverFiles();

    if (files.Count == 0)
    {
        Console.WriteLine("No matching files found in the specified directory.");
        return;
    }

    ScanCache? cache = null;
    if (runOptions.UseCache)
        cache = ScanCache.Load(path.FullName);

    var result = RuleChecker.Run(files, runOptions.Threshold, progressWriter, cache);

    if (runOptions.UseCache)
        cache?.Save(path.FullName);

    ReportWriter.WriteAll(result, path.FullName, runOptions, reportOptions);

    // CI exit code: exit 2 if duplicates exceed threshold
    if (runOptions.MaxDuplicates is not null && result.Duplicates.Count > runOptions.MaxDuplicates.Value)
    {
        Console.Error.WriteLine($"Error: Found {result.Duplicates.Count} duplicate blocks, exceeding max-duplicates threshold of {runOptions.MaxDuplicates.Value}.");
        Environment.ExitCode = 2;
    }

    // CI exit code: exit 2 if any file exceeds line limit
    var overLimit = OverLimitFileInfo.Compute(result, reportOptions, path.FullName);
    if (overLimit.Count > 0)
    {
        Console.Error.WriteLine($"Error: {overLimit.Count} file(s) exceed the maximum line limit.");
        Environment.ExitCode = 2;
    }

    // CI exit code: exit 2 if any file exceeds indentation limit
    var overIndented = OverIndentedFileInfo.Compute(result, reportOptions, path.FullName);
    if (overIndented.Count > 0)
    {
        Console.Error.WriteLine($"Error: {overIndented.Count} file(s) exceed the maximum indentation depth.");
        Environment.ExitCode = 2;
    }

    // CI exit code: exit 2 if any early return violations found
    var earlyReturnViolations = EarlyReturnFileInfo.Compute(result, reportOptions);
    if (earlyReturnViolations.Count > 0)
    {
        var totalViolations = earlyReturnViolations.Sum(f => f.Violations.Count);
        Console.Error.WriteLine($"Error: {totalViolations} early return opportunity(ies) found in {earlyReturnViolations.Count} file(s).");
        Environment.ExitCode = 2;
    }

    // CI exit code: exit 2 if any comment density violations found
    var commentDensityViolations = CommentDensityFileInfo.Compute(result, reportOptions, path.FullName);
    if (commentDensityViolations.Count > 0)
    {
        Console.Error.WriteLine($"Error: {commentDensityViolations.Count} file(s) violate comment density limits.");
        Environment.ExitCode = 2;
    }

    // CI exit code: exit 2 if tech debt score exceeds threshold
    if (runOptions.MaxTechDebtScore is not null)
    {
        var techDebtScore = TechDebtScore.Compute(result, reportOptions, path.FullName);
        if (techDebtScore.Score > runOptions.MaxTechDebtScore.Value)
        {
            Console.Error.WriteLine($"Error: Tech debt score {techDebtScore.Score:F1}/100 (Grade: {techDebtScore.Grade}) exceeds maximum of {runOptions.MaxTechDebtScore.Value}.");
            Environment.ExitCode = 2;
        }
    }
});

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
