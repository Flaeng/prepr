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
            if (config.MaxFileLines is not null)
            {
                var invalidKeys = config.MaxFileLines.Where(kvp => kvp.Value <= 0).Select(kvp => kvp.Key).ToList();
                if (invalidKeys.Count > 0)
                {
                    Console.Error.WriteLine($"Warning: Invalid maxFileLines values in '{configPath}' for keys: {string.Join(", ", invalidKeys)}. Values must be > 0. Removing invalid entries.");
                    foreach (var key in invalidKeys)
                        config.MaxFileLines.Remove(key);
                    if (config.MaxFileLines.Count == 0)
                        config.MaxFileLines = null;
                }
            }
            Console.Error.WriteLine($"Using config: {configPath}");
            return config;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Warning: Invalid JSON in '{configPath}': {ex.Message}. Using defaults.");
            return new PreprConfig();
        }
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
