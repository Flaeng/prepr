namespace PrepR;

public static class ConfigInitializer
{
    public static bool TryInit(string directoryPath)
    {
        var configPath = Path.Combine(directoryPath, ConfigLoader.ConfigFileName);
        if (File.Exists(configPath))
        {
            Console.Error.WriteLine($"Config file already exists: {configPath}");
            Environment.ExitCode = 1;
            return true;
        }
        Directory.CreateDirectory(directoryPath);
        File.WriteAllText(configPath, PrepRConfig.DefaultConfigJson);
        Console.WriteLine($"Config file created: {configPath}");
        return true;
    }
}
