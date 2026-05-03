using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016MatrixOnlyGuardTests
{
    [Fact]
    public void SimulationEngine_ExposesOnlyMatrix()
    {
        var names = Enum.GetNames<Iso52016SimulationEngine>();

        var name = Assert.Single(names);
        Assert.Equal("Matrix", name);
    }

    [Fact]
    public void SourceTree_DoesNotReferenceRemovedSimplifiedHeatBalanceSolver()
    {
        var repoRoot = FindRepositoryRoot();

        var sourceText = string.Join(
            Environment.NewLine,
            Directory
                .GetFiles(Path.Combine(repoRoot, "src", "Backend"), "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("IIso52016RoomHeatBalanceSolver", sourceText);
        Assert.DoesNotContain("Iso52016RoomHeatBalanceSolver", sourceText);
        Assert.DoesNotContain("Iso52016RoomHeatBalanceRequest", sourceText);
        Assert.DoesNotContain("Iso52016SimulationEngine.Legacy", sourceText);
        Assert.DoesNotContain("Iso52016SimulationEngine.V2Matrix", sourceText);
    }

    [Fact]
    public void Documentation_RecordsMatrixOnlyDecision()
    {
        var repoRoot = FindRepositoryRoot();

        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016MatrixOnly.md");

        Assert.True(File.Exists(docPath), $"Documentation was not found: {docPath}");

        var text = File.ReadAllText(docPath);

        Assert.Contains("Matrix-only", text);
        Assert.Contains("Iso52016SimulationEngine.Matrix", text);
        Assert.Contains("old simplified RC heat-balance path has been removed", text);
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