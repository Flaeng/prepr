namespace prepr;

public class LineLimitRule
{
    private readonly List<(string prefix, int limit)> _rules;
    private readonly int? _globalDefault;

    public LineLimitRule(Dictionary<string, int>? configRules, int? cliDefault)
    {
        _rules = [];
        int? configGlobal = null;

        if (configRules is not null)
        {
            foreach (var (key, value) in configRules)
            {
                if (key == "*")
                    configGlobal = value;
                else
                    _rules.Add((NormalizePath(key), value));
            }
        }

        // CLI overrides config "*"
        _globalDefault = cliDefault ?? configGlobal;

        // Sort by prefix length descending for longest-match-first lookup
        _rules.Sort((a, b) => b.prefix.Length.CompareTo(a.prefix.Length));
    }

    public bool HasRules => _rules.Count > 0 || _globalDefault is not null;

    public int? GetLimit(string filePath, string rootPath)
    {
        var relativePath = NormalizePath(Path.GetRelativePath(rootPath, filePath));

        foreach (var (prefix, limit) in _rules)
        {
            if (relativePath.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase) ||
                relativePath.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                return limit;
        }

        return _globalDefault;
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/').TrimEnd('/');
}
