using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Prepr;

public static partial class RuleChecker
{
    private const int DefaultMinConsecutiveLines = 5;

    public static ScanResult Run(IReadOnlyList<string> filePaths, int minConsecutiveLines = DefaultMinConsecutiveLines,
        TextWriter? progressWriter = null,
        ScanCache? cache = null,
        HashSet<string>? safeMagicNumbers = null)
    {
        var (fileLines, fileLineCounts, fileMaxNestingDepths, earlyReturnViolations, fileCommentLineCounts, magicNumberViolations, magicStringViolations, totalLines) = ReadFiles(filePaths, progressWriter, cache, safeMagicNumbers);
        var duplicates = DuplicateDetector.Detect(fileLines, minConsecutiveLines, progressWriter);
        return new ScanResult(duplicates, fileLines.Count, totalLines, fileLineCounts, fileMaxNestingDepths, earlyReturnViolations, fileCommentLineCounts, magicNumberViolations, magicStringViolations);
    }

    private static (IReadOnlyDictionary<string, IndexedLine[]> fileLines, IReadOnlyDictionary<string, int> fileLineCounts, IReadOnlyDictionary<string, NestingDepthInfo> fileMaxNestingDepths, IReadOnlyDictionary<string, IReadOnlyList<EarlyReturnViolation>> earlyReturnViolations, IReadOnlyDictionary<string, int> fileCommentLineCounts, IReadOnlyDictionary<string, IReadOnlyList<MagicNumberViolation>> magicNumberViolations, IReadOnlyDictionary<string, IReadOnlyList<MagicStringViolation>> magicStringViolations, int totalLines)
        ReadFiles(IReadOnlyList<string> filePaths,
            TextWriter? progressWriter,
            ScanCache? cache,
            HashSet<string>? safeMagicNumbers)
    {
        var fileLines = new ConcurrentDictionary<string, IndexedLine[]>();
        var fileLineCounts = new ConcurrentDictionary<string, int>();
        var fileMaxNestingDepths = new ConcurrentDictionary<string, NestingDepthInfo>();
        var earlyReturnViolations = new ConcurrentDictionary<string, IReadOnlyList<EarlyReturnViolation>>();
        var fileCommentLineCounts = new ConcurrentDictionary<string, int>();
        var magicNumberViolations = new ConcurrentDictionary<string, IReadOnlyList<MagicNumberViolation>>();
        var magicStringViolations = new ConcurrentDictionary<string, IReadOnlyList<MagicStringViolation>>();
        int totalLines = 0;
        int fileCount = filePaths.Count;
        int filesRead = 0;

        ProgressBar? bar = (progressWriter is not null && fileCount > 0)
            ? new ProgressBar(progressWriter, fileCount)
            : null;

        Parallel.ForEach(filePaths, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, path =>
        {
            ReadSingleFile(path, cache, safeMagicNumbers, ref totalLines, fileLines, fileLineCounts, fileMaxNestingDepths, earlyReturnViolations, fileCommentLineCounts, magicNumberViolations, magicStringViolations);

            int current = Interlocked.Increment(ref filesRead);
            bar?.Update(current, "Reading files...");
        });

        bar?.Complete();

        return (fileLines, fileLineCounts, fileMaxNestingDepths, earlyReturnViolations, fileCommentLineCounts, magicNumberViolations, magicStringViolations, totalLines);
    }

    private static void ReadSingleFile(string path, ScanCache? cache, HashSet<string>? safeMagicNumbers, ref int totalLines,
        ConcurrentDictionary<string, IndexedLine[]> fileLines,
        ConcurrentDictionary<string, int> fileLineCounts,
        ConcurrentDictionary<string, NestingDepthInfo> fileMaxNestingDepths,
        ConcurrentDictionary<string, IReadOnlyList<EarlyReturnViolation>> earlyReturnViolations,
        ConcurrentDictionary<string, int> fileCommentLineCounts,
        ConcurrentDictionary<string, IReadOnlyList<MagicNumberViolation>> magicNumberViolations,
        ConcurrentDictionary<string, IReadOnlyList<MagicStringViolation>> magicStringViolations)
    {
        try
        {
            IndexedLine[]? indexed = null;
            int lineCount;

            if (cache is not null && cache.TryGetCached(path, out var cachedLines, out var cachedLineCount))
            {
                indexed = cachedLines;
                lineCount = cachedLineCount;
            }
            else
            {
                var raw = File.ReadAllLines(path);
                lineCount = raw.Length;
                indexed = raw
                    .Select((text, idx) => new IndexedLine(idx + 1, text))
                    .Where(l => !string.IsNullOrWhiteSpace(l.Text))
                    .ToArray();

                cache?.Update(path, indexed, lineCount);
            }

            Interlocked.Add(ref totalLines, lineCount);
            fileLines[path] = indexed;
            fileLineCounts[path] = lineCount;
            fileMaxNestingDepths[path] = ComputeMaxNestingDepth(indexed);
            earlyReturnViolations[path] = EarlyReturnAnalyzer.Analyze(indexed);
            fileCommentLineCounts[path] = CountCommentLines(indexed);
            magicNumberViolations[path] = FindMagicNumbers(indexed, safeMagicNumbers);
            magicStringViolations[path] = FindMagicStrings(indexed);
        }
        catch (UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Warning: Access denied to '{path}', skipping.");
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Warning: Could not read '{path}': {ex.Message}");
        }
    }

    private static NestingDepthInfo ComputeMaxNestingDepth(IndexedLine[] lines)
    {
        int currentDepth = 0;
        int maxDepth = 0;
        var lineDepths = new List<(int LineNumber, int Depth)>(lines.Length);
        bool inBlockComment = false;
        bool inVerbatimString = false;

        foreach (var line in lines)
        {
            int lineMax = currentDepth;
            var text = line.Text;
            bool inRegularString = false;
            bool inChar = false;

            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                char next = i + 1 < text.Length ? text[i + 1] : '\0';

                if (inBlockComment)
                {
                    if (ch == '*' && next == '/')
                    {
                        inBlockComment = false;
                        i++;
                    }
                    continue;
                }

                if (inVerbatimString)
                {
                    if (ch == '"')
                    {
                        if (next == '"')
                            i++;
                        else
                            inVerbatimString = false;
                    }
                    continue;
                }

                if (inRegularString)
                {
                    if (ch == '\\')
                        i++;
                    else if (ch == '"')
                        inRegularString = false;
                    continue;
                }

                if (inChar)
                {
                    if (ch == '\\')
                        i++;
                    else if (ch == '\'')
                        inChar = false;
                    continue;
                }

                if (ch == '/' && next == '/')
                    break;

                if (ch == '/' && next == '*')
                {
                    inBlockComment = true;
                    i++;
                    continue;
                }

                if (ch == '\'')
                {
                    inChar = true;
                    continue;
                }

                if (ch == '"')
                {
                    bool isVerbatim = (i > 0 && text[i - 1] == '@') ||
                                      (i > 0 && text[i - 1] == '$' && i > 1 && text[i - 2] == '@');
                    if (isVerbatim)
                        inVerbatimString = true;
                    else
                        inRegularString = true;
                    continue;
                }

                if (ch == '{')
                {
                    currentDepth++;
                    if (currentDepth > lineMax)
                        lineMax = currentDepth;
                }
                else if (ch == '}')
                {
                    currentDepth--;
                }
            }

            lineDepths.Add((line.LineNumber, lineMax));
            if (lineMax > maxDepth)
                maxDepth = lineMax;
        }

        return new NestingDepthInfo(maxDepth, lineDepths);
    }

    internal static int CountCommentLines(IndexedLine[] lines)
    {
        int commentLines = 0;
        bool inBlockComment = false;

        foreach (var line in lines)
        {
            var trimmed = line.Text.TrimStart();

            if (inBlockComment)
            {
                commentLines++;
                if (trimmed.Contains("*/"))
                    inBlockComment = false;
                continue;
            }

            if (trimmed.StartsWith("//"))
            {
                commentLines++;
            }
            else if (trimmed.StartsWith("/*"))
            {
                commentLines++;
                if (!trimmed.Contains("*/") || trimmed.IndexOf("*/") < trimmed.IndexOf("/*") + 2)
                {
                    // Check if the block comment is closed on the same line
                    var afterOpen = trimmed[(trimmed.IndexOf("/*") + 2)..];
                    if (!afterOpen.Contains("*/"))
                        inBlockComment = true;
                }
            }
        }

        return commentLines;
    }

    [GeneratedRegex(@"(?<!\w)(?:(?:0[xX][0-9a-fA-F]+)|(?:0[bB][01]+)|(?:\d+\.\d+)|(?:\d+))(?:[fFdDmMlLuU]*)(?!\w)", RegexOptions.Compiled)]
    private static partial Regex MagicNumberRegex();

    internal static IReadOnlyList<MagicNumberViolation> FindMagicNumbers(IndexedLine[] lines, HashSet<string>? safeMagicNumbers = null)
    {
        var violations = new List<MagicNumberViolation>();
        bool inBlockComment = false;
        bool inRawString = false;

        foreach (var line in lines)
        {
            var trimmed = line.Text.TrimStart();

            if (inRawString)
            {
                if (trimmed.Contains("\"\"\""))
                    inRawString = false;
                continue;
            }

            if (inBlockComment)
            {
                if (trimmed.Contains("*/"))
                    inBlockComment = false;
                continue;
            }

            if (trimmed.StartsWith("//"))
                continue;

            if (trimmed.StartsWith("/*"))
            {
                var afterOpen = trimmed[(trimmed.IndexOf("/*") + 2)..];
                if (!afterOpen.Contains("*/"))
                    inBlockComment = true;
                continue;
            }

            // Check for raw string literal start (""", $""", $$""", @""", etc.)
            if (line.Text.Contains("\"\"\""))
            {
                var idx = line.Text.IndexOf("\"\"\"");
                var afterOpen = line.Text[(idx + 3)..];
                if (!afterOpen.Contains("\"\"\""))
                    inRawString = true;
                continue;
            }

            var textWithoutStrings = StringLiteralRegex().Replace(line.Text, "\"\"");
            foreach (Match match in MagicNumberRegex().Matches(textWithoutStrings))
            {
                var rawValue = match.Value;
                // Strip type suffixes (e.g. 42L, 3.14f) for safe-number comparison
                var numericValue = rawValue.TrimEnd('f', 'F', 'd', 'D', 'm', 'M', 'l', 'L', 'u', 'U');
                if (safeMagicNumbers is not null && safeMagicNumbers.Contains(numericValue))
                    continue;
                violations.Add(new MagicNumberViolation(line.LineNumber, rawValue, trimmed));
            }
        }

        return violations;
    }


    [GeneratedRegex(@"""([^""\\]*(?:\\.[^""\\]*)*)""", RegexOptions.Compiled)]
    private static partial Regex StringLiteralRegex();

    internal static IReadOnlyList<MagicStringViolation> FindMagicStrings(IndexedLine[] lines)
    {
        var violations = new List<MagicStringViolation>();
        bool inBlockComment = false;

        foreach (var line in lines)
        {
            var trimmed = line.Text.TrimStart();

            if (inBlockComment)
            {
                if (trimmed.Contains("*/"))
                    inBlockComment = false;
                continue;
            }

            if (trimmed.StartsWith("//"))
                continue;

            if (trimmed.StartsWith("/*"))
            {
                var afterOpen = trimmed[(trimmed.IndexOf("/*") + 2)..];
                if (!afterOpen.Contains("*/"))
                    inBlockComment = true;
                continue;
            }

            foreach (Match match in StringLiteralRegex().Matches(line.Text))
            {
                var value = match.Groups[1].Value;
                // Skip empty strings and single characters
                if (value.Length <= 1)
                    continue;

                violations.Add(new MagicStringViolation(line.LineNumber, value, trimmed));
            }
        }

        return violations;
    }
}
