using Microsoft.Extensions.FileSystemGlobbing;

namespace Prepr;

public class FileDiscovery
{
    private static readonly HashSet<string> DefaultExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".ts", ".js", ".tsx", ".jsx", ".txt", ".json", ".xml", ".yaml", ".yml",
        ".md", ".html", ".css", ".scss", ".sql", ".py", ".java", ".rb", ".go", ".rs",
        ".cpp", ".c", ".h", ".hpp", ".sh", ".ps1", ".bat", ".cfg", ".ini", ".toml",
        ".razor", ".cshtml", ".vue", ".svelte", ".php"
    };

    private readonly string _rootPath;
    private readonly RunOptions _runOptions;
    private readonly TextWriter? _progressWriter;
    private readonly Verbosity _verbosity;

    private readonly HashSet<string> _allowedExtensions;
    private readonly HashSet<string> _excludedExtensions;

    public FileDiscovery(string rootPath, RunOptions runOptions, TextWriter? progressWriter = null, Verbosity verbosity = Verbosity.Normal)
    {
        _rootPath = rootPath;
        _runOptions = runOptions;
        _progressWriter = progressWriter;
        _verbosity = verbosity;

        _allowedExtensions = _runOptions.Extensions is not null
            ? new HashSet<string>(_runOptions.Extensions.Select(e => e.StartsWith('.') ? e : "." + e), StringComparer.OrdinalIgnoreCase)
            : DefaultExtensions;

        _excludedExtensions = _runOptions.ExcludeExtensions is not null
            ? new HashSet<string>(_runOptions.ExcludeExtensions.Select(e => e.StartsWith('.') ? e : "." + e), StringComparer.OrdinalIgnoreCase)
            : [];
    }

    public IReadOnlyList<string> DiscoverFiles()
    {
        var ignoredDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Matcher? globMatcher = null;
        if (_runOptions.IgnorePaths is not null)
        {
            var globPatterns = new List<string>();
            foreach (var pattern in _runOptions.IgnorePaths)
            {
                // Patterns containing glob characters or path separators are treated as globs
                if (pattern.Contains('*') || pattern.Contains('?') || pattern.Contains('/') || pattern.Contains('\\'))
                    globPatterns.Add(pattern);
                else
                    ignoredDirs.Add(pattern);
            }
            if (globPatterns.Count > 0)
            {
                globMatcher = new Matcher();
                foreach (var g in globPatterns)
                {
                    // Patterns without a directory separator match at any depth
                    var normalized = (!g.Contains('/') && !g.Contains('\\')) ? "**/" + g : g;
                    globMatcher.AddInclude(normalized);
                }
            }
        }

        // Count directories for progress reporting
        int processedDirs = 0;
        ProgressBar? bar = null;
        if (_progressWriter is not null)
        {
            var totalDirs = CountDirectories(_rootPath, ignoredDirs, globMatcher);
            if (totalDirs > 0)
                bar = new ProgressBar(_progressWriter, totalDirs);
        }

        var files = new List<string>();
        List<string>? ignoredDirectories = null;
        List<string>? ignoredFiles = null;
        bool log = _verbosity == Verbosity.Detailed;
        if (log)
        {
            ignoredDirectories = [];
            ignoredFiles = [];
        }

        CollectFiles(_rootPath, ignoredDirs, globMatcher, files, bar, ref processedDirs, ignoredDirectories, ignoredFiles);

        bar?.Complete();

        if (log)
        {
            PrintList("Ignored directories", ignoredDirectories!);
            PrintList("Ignored files", ignoredFiles!);
        }

        return files;
    }

    private static void PrintList(string label, List<string> items)
    {
        if (items.Count == 0)
            return;

        Console.Error.WriteLine($"{label} ({items.Count}):");
        foreach (var item in items)
            Console.Error.WriteLine($"  {item}");
    }

    private int CountDirectories(string directory, HashSet<string> ignoredDirs, Matcher? globMatcher)
    {
        int count = 1; // count self
        try
        {
            foreach (var subDir in GetUnignoredDirectories(directory, ignoredDirs, globMatcher))
            {
                count += CountDirectories(subDir, ignoredDirs, globMatcher);
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
        return count;
    }

    private IEnumerable<string> GetUnignoredDirectories(string directory, HashSet<string> ignoredDirs, Matcher? globMatcher, List<string>? ignoredDirectories = null)
    {
        foreach (var subDir in Directory.EnumerateDirectories(directory))
        {
            var dirName = Path.GetFileName(subDir);
            if (ignoredDirs.Contains(dirName))
            {
                ignoredDirectories?.Add(Path.GetRelativePath(_rootPath, subDir));
                continue;
            }

            if (globMatcher is not null)
            {
                var relativePath = Path.GetRelativePath(_rootPath, subDir);
                var probePath = Path.Combine(relativePath, "___probe___");
                if (globMatcher.Match(probePath).HasMatches)
                {
                    ignoredDirectories?.Add(relativePath);
                    continue;
                }
            }

            yield return subDir;
        }
    }

    private void CollectFiles(
        string directory,
        HashSet<string> ignoredDirs,
        Matcher? globMatcher,
        List<string> results,
        ProgressBar? bar,
        ref int processedDirs,
        List<string>? ignoredDirectories,
        List<string>? ignoredFiles)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                var ext = Path.GetExtension(file);
                if (!_allowedExtensions.Contains(ext))
                    continue;

                if (_excludedExtensions is not null && _excludedExtensions.Contains(ext))
                {
                    ignoredFiles?.Add(Path.GetRelativePath(_rootPath, file));
                    continue;
                }

                if (globMatcher is not null)
                {
                    var relativePath = Path.GetRelativePath(_rootPath, file);
                    if (globMatcher.Match(relativePath).HasMatches)
                    {
                        ignoredFiles?.Add(relativePath);
                        continue;
                    }
                }
                results.Add(file);
            }

            processedDirs++;
            bar?.Update(processedDirs, "Discovering files...");

            foreach (var subDir in GetUnignoredDirectories(directory, ignoredDirs, globMatcher, ignoredDirectories))
            {
                CollectFiles(subDir, ignoredDirs, globMatcher, results, bar, ref processedDirs, ignoredDirectories, ignoredFiles);
            }
        }
        catch (UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Warning: Access denied to '{directory}', skipping.");
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Warning: Could not read '{directory}': {ex.Message}");
        }
    }
}
