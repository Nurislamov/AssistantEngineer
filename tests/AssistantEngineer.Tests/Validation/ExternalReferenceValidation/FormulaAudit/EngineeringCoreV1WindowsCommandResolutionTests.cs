namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation.FormulaAudit;

public class EngineeringCoreV1WindowsCommandResolutionTests
{
    [Theory]
    [InlineData("tools/AssistantEngineer.Tools.EngineeringCoreRelease/Program.cs")]
    [InlineData("tools/AssistantEngineer.Tools.EngineeringCoreVerification/Program.cs")]
    public void EngineeringCoreTools_ResolveNpmToWindowsCommandShim(
        string relativePath)
    {
        var repoRoot = FindRepositoryRoot();
        var path = Path.Combine(
            relativePath.Split('/').Prepend(repoRoot).ToArray());

        Assert.True(File.Exists(path), $"Tool source was not found: {path}");

        var source = File.ReadAllText(path);

        Assert.Contains("ResolveProcessFileName(fileName)", source);
        Assert.Contains("private static string ResolveProcessFileName(string fileName)", source);
        Assert.Contains("normalized + \".cmd\"", source);
        Assert.Contains("Path.PathSeparator", source);
        Assert.Contains("FindExecutableOnPath(normalized, \".cmd\", \".exe\", \".bat\")", source);
        Assert.Contains("StringComparison.OrdinalIgnoreCase", source);
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