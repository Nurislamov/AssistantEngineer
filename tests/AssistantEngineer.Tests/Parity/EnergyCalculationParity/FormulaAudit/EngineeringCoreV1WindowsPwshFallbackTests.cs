namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.FormulaAudit;

public class EngineeringCoreV1WindowsPwshFallbackTests
{
    [Theory]
    [InlineData("tools/AssistantEngineer.Tools.EngineeringCoreRelease/Program.cs")]
    [InlineData("tools/AssistantEngineer.Tools.EngineeringCoreVerification/Program.cs")]
    public void EngineeringCoreTools_ResolvePwshToWindowsPowerShellFallback(
        string relativePath)
    {
        var repoRoot = FindRepositoryRoot();
        var path = Path.Combine(
            relativePath.Split('/').Prepend(repoRoot).ToArray());

        Assert.True(File.Exists(path), $"Tool source was not found: {path}");

        var source = File.ReadAllText(path);

        Assert.Contains("ResolveProcessFileName(fileName)", source);
        Assert.Contains("string.Equals(normalized, \"pwsh\", StringComparison.OrdinalIgnoreCase)", source);
        Assert.Contains("FindExecutableOnPath(\"pwsh\"", source);
        Assert.Contains("FindExecutableOnPath(\"powershell\"", source);
        Assert.Contains("return \"powershell.exe\";", source);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var src = Path.Combine(
                directory.FullName,
                "src",
                "Backend",
                "AssistantEngineer.Modules.Calculations");

            var tests = Path.Combine(
                directory.FullName,
                "tests",
                "AssistantEngineer.Tests");

            if (Directory.Exists(src) && Directory.Exists(tests))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate AssistantEngineer repository root from test base directory.");
    }
}