using Microsoft.Extensions.FileSystemGlobbing;

namespace PrepR;

public static class FileDiscovery
{
    private static readonly HashSet<string> DefaultExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".ts", ".js", ".tsx", ".jsx", ".txt", ".json", ".xml", ".yaml", ".yml",
        ".md", ".html", ".css", ".scss", ".sql", ".py", ".java", ".rb", ".go", ".rs",
        ".cpp", ".c", ".h", ".hpp", ".sh", ".ps1", ".bat", ".cfg", ".ini", ".toml",
        ".razor", ".cshtml", ".vue", ".svelte", ".php"
    };

    public static IReadOnlyList<string> DiscoverFiles(
        string rootPath,
        RunOptions runOptions,
        TextWriter? progressWriter = null)
        => DiscoverFiles(rootPath, runOptions.Extensions, runOptions.ExcludeExtensions, runOptions.IgnorePaths, progressWriter);

    public static IReadOnlyList<string> DiscoverFiles(
        string rootPath,
        IEnumerable<string>? extensions = null,
        IEnumerable<string>? excludeExtensions = null,
        IEnumerable<string>? ignorePaths = null,
        TextWriter? progressWriter = null)
    {
        var allowedExtensions = extensions is not null
            ? new HashSet<string>(extensions.Select(e => e.StartsWith('.') ? e : "." + e), StringComparer.OrdinalIgnoreCase)
            : DefaultExtensions;

        var excludedExtensions = excludeExtensions is not null
            ? new HashSet<string>(excludeExtensions.Select(e => e.StartsWith('.') ? e : "." + e), StringComparer.OrdinalIgnoreCase)
            : null;

        var ignoredDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Matcher? globMatcher = null;
        if (ignorePaths is not null)
        {
            var globPatterns = new List<string>();
            foreach (var pattern in ignorePaths)
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
                    globMatcher.AddInclude(g);
            }
        }

        // Count directories for progress reporting
        int totalDirs = 0;
        int processedDirs = 0;
        ProgressBar? bar = null;
        if (progressWriter is not null)
        {
            totalDirs = CountDirectories(rootPath, ignoredDirs);
            if (totalDirs > 0)
                bar = new ProgressBar(progressWriter, totalDirs);
        }

        var files = new List<string>();
        CollectFiles(rootPath, rootPath, allowedExtensions, excludedExtensions, ignoredDirs, globMatcher, files,
            bar, ref processedDirs);

        bar?.Complete();

        return files;
    }

    private static int CountDirectories(string directory, HashSet<string> ignoredDirs)
    {
        int count = 1; // count self
        try
        {
            foreach (var subDir in Directory.EnumerateDirectories(directory))
            {
                var dirName = Path.GetFileName(subDir);
                if (ignoredDirs.Contains(dirName))
                    continue;
                count += CountDirectories(subDir, ignoredDirs);
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
        return count;
    }

    private static void CollectFiles(
        string directory,
        string rootPath,
        HashSet<string> allowedExtensions,
        HashSet<string>? excludedExtensions,
        HashSet<string> ignoredDirs,
        Matcher? globMatcher,
        List<string> results,
        ProgressBar? bar,
        ref int processedDirs)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                var ext = Path.GetExtension(file);
                if (!allowedExtensions.Contains(ext))
                    continue;
                if (excludedExtensions is not null && excludedExtensions.Contains(ext))
                    continue;
                if (globMatcher is not null)
                {
                    var relativePath = Path.GetRelativePath(rootPath, file);
                    if (globMatcher.Match(relativePath).HasMatches)
                        continue;
                }
                results.Add(file);
            }

            processedDirs++;
            bar?.Update(processedDirs, "Discovering files...");

            foreach (var subDir in Directory.EnumerateDirectories(directory))
            {
                var dirName = Path.GetFileName(subDir);
                if (ignoredDirs.Contains(dirName))
                    continue;
                CollectFiles(subDir, rootPath, allowedExtensions, excludedExtensions, ignoredDirs, globMatcher, results,
                    bar, ref processedDirs);
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
