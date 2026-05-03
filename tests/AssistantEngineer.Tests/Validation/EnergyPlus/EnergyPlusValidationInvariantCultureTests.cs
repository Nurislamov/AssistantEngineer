namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusValidationInvariantCultureTests
{
    [Fact]
    public void EnergyPlusValidationTool_ForcesInvariantCultureForGeneratedArtifacts()
    {
        var repoRoot = FindRepositoryRoot();

        var toolPath = Path.Combine(
            repoRoot,
            "tools",
            "AssistantEngineer.Tools.EnergyPlusValidation",
            "Program.cs");

        Assert.True(File.Exists(toolPath), $"EnergyPlus validation tool was not found: {toolPath}");

        var source = File.ReadAllText(toolPath);

        Assert.Contains("using System.Globalization;", source);
        Assert.Contains("CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;", source);
        Assert.Contains("CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;", source);
    }

    [Theory]
    [InlineData("docs/reports/validation/EP-SMOKE-001-ComparisonResult.md", "37.8", "37,8")]
    [InlineData("docs/reports/validation/EP-SMOKE-003-ComparisonResult.md", "28.8", "28,8")]
    public void GeneratedValidationMarkdown_UsesDotDecimalSeparator(
        string relativePath,
        string expectedDotDecimal,
        string forbiddenCommaDecimal)
    {
        var repoRoot = FindRepositoryRoot();
        var path = Path.Combine(
            relativePath.Split('/').Prepend(repoRoot).ToArray());

        Assert.True(File.Exists(path), $"Generated validation markdown was not found: {path}");

        var content = File.ReadAllText(path);

        Assert.Contains(expectedDotDecimal, content);
        Assert.DoesNotContain(forbiddenCommaDecimal, content);
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