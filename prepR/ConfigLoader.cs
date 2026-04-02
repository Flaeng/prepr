using System.Text.Json;

namespace Prepr;

public static class ConfigLoader
{
    public const string ConfigFileName = ".preprrc";

    public static PreprConfig LoadConfig(string startDirectory)
    {
        var configPath = FindConfigFile(startDirectory);
        if (configPath is null)
            return new PreprConfig();

        try
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<PreprConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (config is null)
                return new PreprConfig();

            ValidateConfig(config, configPath);
            Console.Error.WriteLine($"Using config: {configPath}");
            return config;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Warning: Invalid JSON in '{configPath}': {ex.Message}. Using defaults.");
            return new PreprConfig();
        }
    }

    private static void ValidateConfig(PreprConfig config, string configPath)
    {
        if (config.Threshold is <= 0)
        {
            Console.Error.WriteLine($"Warning: Invalid threshold ({config.Threshold}) in '{configPath}'. Must be > 0. Using default.");
            config.Threshold = null;
        }

        if (config.Verbosity is not null &&
            !string.Equals(config.Verbosity, "quiet", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(config.Verbosity, "normal", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(config.Verbosity, "detailed", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Warning: Invalid verbosity '{config.Verbosity}' in '{configPath}'. Must be quiet, normal, or detailed. Using default.");
            config.Verbosity = null;
        }

        if (config.HighSeverityThreshold is < 0 or > 100)
        {
            Console.Error.WriteLine($"Warning: Invalid highSeverityThreshold ({config.HighSeverityThreshold}) in '{configPath}'. Must be 0–100. Using default.");
            config.HighSeverityThreshold = null;
        }

        if (config.MediumSeverityThreshold is < 0 or > 100)
        {
            Console.Error.WriteLine($"Warning: Invalid mediumSeverityThreshold ({config.MediumSeverityThreshold}) in '{configPath}'. Must be 0–100. Using default.");
            config.MediumSeverityThreshold = null;
        }

        RemoveInvalidDictionaryEntries(config.MaxFileLines, "maxFileLines", configPath, v => v <= 0);
        if (config.MaxFileLines?.Count == 0)
            config.MaxFileLines = null;

        RemoveInvalidDictionaryEntries(config.MaxIndentation, "maxIndentation", configPath, v => v <= 0);
        if (config.MaxIndentation?.Count == 0)
            config.MaxIndentation = null;
    }

    private static void RemoveInvalidDictionaryEntries(Dictionary<string, int>? dict, string name, string configPath, Func<int, bool> isInvalid)
    {
        if (dict is null)
            return;

        var invalidKeys = dict.Where(kvp => isInvalid(kvp.Value)).Select(kvp => kvp.Key).ToList();
        if (invalidKeys.Count == 0)
            return;

        Console.Error.WriteLine($"Warning: Invalid {name} values in '{configPath}' for keys: {string.Join(", ", invalidKeys)}. Values must be > 0. Removing invalid entries.");
        foreach (var key in invalidKeys)
            dict.Remove(key);
    }

    private static string? FindConfigFile(string startDirectory)
    {
        var dir = new DirectoryInfo(startDirectory);
        while (dir is not null)
        {
            var configPath = Path.Combine(dir.FullName, ConfigFileName);
            if (File.Exists(configPath))
                return configPath;
            dir = dir.Parent;
        }
        return null;
    }
}
