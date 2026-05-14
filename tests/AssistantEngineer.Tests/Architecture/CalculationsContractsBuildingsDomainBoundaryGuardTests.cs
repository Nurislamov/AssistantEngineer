using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Architecture;

public sealed class CalculationsContractsBuildingsDomainBoundaryGuardTests
{
    private const string ForbiddenDependency = "AssistantEngineer.Modules.Buildings.Domain";
    private const string DebtDocumentPath = "docs/architecture/calculations-buildings-boundary-debt.md";

    private static readonly HashSet<string> BaselineAllowedFiles =
    [
        NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016BuildingDomainSimulationFacadeRequest.cs"),
        NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016BuildingDomainSimulationFacadeResult.cs"),
        NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016BuildingSimulationFacadeRequest.cs"),
        NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016HourlyRoomWindowSolarGainRecord.cs"),
        NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016HourlyWeatherSolarRecord.cs"),
        NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016HourlyWindowSolarGainRecord.cs"),
        NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016RoomEnergySimulationBuildRequest.cs"),
        NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016RoomSimulationFacadeRequest.cs"),
        NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016WeatherSolarContextRequest.cs"),
        NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016WindowSolarGainInput.cs"),
        NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016WindowSolarGainProfile.cs"),
        NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016WindowSolarGainProfileRequest.cs"),
        NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016WindowSolarGainRequest.cs"),
        NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Iso52016/Iso52016WindowSolarGainResult.cs"),
        NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Validation/BuildingInput/BuildingInputValidationRequest.cs"),
        NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/Weather/AnnualWeatherNormalizationRequest.cs"),
        NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/WeatherSolar/WeatherSolarSurface.cs"),
        NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Contracts/WeatherSolar/WeatherSolarSurfaceCodes.cs")
    ];

    [Fact]
    public void CalculationsContracts_DoNotIntroduceNewBuildingsDomainDependenciesOutsideBaseline()
    {
        var contractsRoot = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Contracts");

        var filesWithForbiddenDependency = Directory
            .EnumerateFiles(contractsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains(ForbiddenDependency, StringComparison.Ordinal))
            .Select(Path.GetFullPath)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var unexpectedViolations = filesWithForbiddenDependency
            .Where(path => !BaselineAllowedFiles.Contains(path))
            .ToArray();

        Assert.True(
            unexpectedViolations.Length == 0,
            $"New forbidden dependency from Calculations.Application.Contracts to Buildings.Domain detected. " +
            $"See {DebtDocumentPath}. Violations: {string.Join("; ", unexpectedViolations)}");
    }

    [Fact]
    public void CalculationsContractsBoundaryDebtDocument_Exists_AndMentionsTargetModel()
    {
        var path = Path.Combine(TestPaths.RepoRoot, DebtDocumentPath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"Debt document is missing: {path}");

        var content = File.ReadAllText(path);
        Assert.Contains("Buildings.Application.Contracts", content, StringComparison.Ordinal);
        Assert.Contains("Calculations.Application.Contracts", content, StringComparison.Ordinal);
        Assert.Contains("must not expose `Buildings.Domain` entities", content, StringComparison.Ordinal);
        Assert.Contains("P1 Backlog Item", content, StringComparison.Ordinal);
    }

    private static string NormalizePath(string relativePath) =>
        Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
