using System.Diagnostics;

namespace prepR.Tests;

public class WriteToFileTests
{
    [Fact]
    public void OutputFile_CreatesDirectoryIfNotExists()
    {
        using var tree = new TempFileTree();
        tree.AddFile("src/A.cs", ["line1", "line2", "line3", "line4", "line5", "line6"]);
        tree.AddFile("src/B.cs", ["line1", "line2", "line3", "line4", "line5", "line6"]);

        var outputDir = Path.Combine(tree.RootPath, "reports", "nested");
        var outputBase = Path.Combine(outputDir, "report.prepr");

        Assert.False(Directory.Exists(outputDir));

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList = { "run", "--project", GetProjectPath(), "--", tree.RootPath, "--output", "csv", "--output-file", outputBase },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi)!;
        process.WaitForExit(30_000);

        Assert.True(Directory.Exists(outputDir), "Output directory should have been created");
        Assert.True(File.Exists(outputBase + ".csv"), "CSV report file should exist");
    }

    private static string GetProjectPath()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "prepR.Tests.csproj")))
            dir = Path.GetDirectoryName(dir);
        return Path.Combine(Path.GetDirectoryName(dir!)!, "prepR", "prepR.csproj");
    }
}
