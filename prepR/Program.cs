using System.CommandLine;
using System.CommandLine.Parsing;

using prepr;
using static prepr.CliSymbols;

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
    MaxFileLinesOption
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

    var files = FileDiscovery.DiscoverFiles(
        path.FullName,
        runOptions,
        progressWriter);

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
    if (reportOptions.LineLimitRule is not null)
    {
        var overLimit = OverLimitFileInfo.Compute(result, reportOptions.LineLimitRule, path.FullName);
        if (overLimit.Count > 0)
        {
            Console.Error.WriteLine($"Error: {overLimit.Count} file(s) exceed the maximum line limit.");
            Environment.ExitCode = 2;
        }
    }
});

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
