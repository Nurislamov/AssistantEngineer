using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixExternalValidationAnchorTests
{
    private const double Tolerance = 0.000001;

    private readonly Iso52016MatrixHourlySolver _solver = new();

    [Theory]
    [MemberData(nameof(ManualAnchorFixtureFiles))]
    public void MatrixSolver_MatchesIndependentManualAnchorFormula(
        string fixturePath)
    {
        var fixture = LoadFixture(fixturePath);
        var manual = CalculateManualExpectation(fixture);

        Assert.Equal("ManualEngineeringValidationAnchor", fixture.SourceType);
        Assert.Equal("IndependentManualEngineeringFormula", fixture.AuthoritativeReference);
        Assert.Equal("Validation anchor only; not full parity.", fixture.ValidationClaim);
        Assert.DoesNotContain("authoritative pyBuildingEnergy", fixture.MethodologicalBackground, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("authoritative EnergyPlus", fixture.MethodologicalBackground, StringComparison.OrdinalIgnoreCase);

        AssertClose(fixture.Expected.HeatingLoadW, manual.HeatingLoadW);
        AssertClose(fixture.Expected.CoolingLoadW, manual.CoolingLoadW);
        AssertClose(fixture.Expected.HeatingEnergyKWh, manual.HeatingEnergyKWh);
        AssertClose(fixture.Expected.CoolingEnergyKWh, manual.CoolingEnergyKWh);

        var result = _solver.Solve(CreateRequest(fixture));

        Assert.True(result.IsSuccess, result.Error);

        var profile = result.Value;
        var hour = Assert.Single(profile.Hours);

        Assert.Equal(fixture.ScenarioName, profile.ZoneCode);
        Assert.Equal(1, profile.HourCount);

        AssertClose(manual.HeatingLoadW, hour.HeatingLoadW);
        AssertClose(manual.CoolingLoadW, hour.CoolingLoadW);
        AssertClose(manual.HeatingEnergyKWh, hour.HeatingEnergyKWh);
        AssertClose(manual.CoolingEnergyKWh, hour.CoolingEnergyKWh);
        AssertClose(fixture.Expected.AirTemperatureAfterHvacC, hour.AirTemperatureAfterHvacC);

        AssertClose(manual.HeatingEnergyKWh, profile.AnnualHeatingEnergyKWh);
        AssertClose(manual.CoolingEnergyKWh, profile.AnnualCoolingEnergyKWh);
        AssertClose(manual.HeatingLoadW, profile.PeakHeatingLoadW);
        AssertClose(manual.CoolingLoadW, profile.PeakCoolingLoadW);
    }

    [Fact]
    public void ExternalValidationAnchorFixtureSet_CoversFirstManualAnchorsOnly()
    {
        var anchors = ManualAnchorFixtureFiles()
            .Select(data => LoadFixture((string)data[0]).AnchorId)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Contains("MANUAL-ISO52016-ANCHOR-001", anchors);
        Assert.Contains("MANUAL-ISO52016-ANCHOR-002", anchors);
        Assert.Contains("MANUAL-ISO52016-ANCHOR-003", anchors);
        Assert.DoesNotContain("MANUAL-ISO52016-ANCHOR-004", anchors);
        Assert.DoesNotContain("MANUAL-ISO52016-ANNUAL-8760-001", anchors);
    }

    [Fact]
    public void ExternalValidationAnchors_DocumentExplicitNonClaimsAndIndependentAuthority()
    {
        var repoRoot = FindRepositoryRoot();
        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016MatrixExternalValidationAnchors.md");
        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixExternalValidationAnchorsManifest.json");

        Assert.True(File.Exists(docPath), $"Anchor documentation was not found: {docPath}");
        Assert.True(File.Exists(manifestPath), $"Anchor manifest was not found: {manifestPath}");

        var doc = File.ReadAllText(docPath);
        var manifest = File.ReadAllText(manifestPath);

        Assert.Contains("validation anchors only", doc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not full parity", doc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pyBuildingEnergy` remains methodological background only", doc);
        Assert.Contains("Independent manual engineering formulas only", manifest);
        Assert.Contains("No exact pyBuildingEnergy numerical parity claim.", manifest);
        Assert.Contains("No exact EnergyPlus numerical parity claim.", manifest);
        Assert.Contains("No full ISO 52016 validation coverage claim.", manifest);
    }

    [Fact]
    public void VerificationScripts_IncludeExternalValidationAnchorsInAllAndReleaseReadyGates()
    {
        var repoRoot = FindRepositoryRoot();
        var anchorVerificationScript = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-external-validation-anchors.ps1");
        var allVerificationScript = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-all.ps1");
        var releaseReadyScript = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "assert-iso52016-matrix-release-ready.ps1");

        Assert.True(File.Exists(anchorVerificationScript), $"Anchor verification script was not found: {anchorVerificationScript}");

        var anchorScript = File.ReadAllText(anchorVerificationScript);
        var allScript = File.ReadAllText(allVerificationScript);
        var releaseScript = File.ReadAllText(releaseReadyScript);

        Assert.Contains("validation anchors only", anchorScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Expected at least 3 ISO52016 Matrix external validation anchor fixtures", anchorScript);
        Assert.DoesNotContain("Expected at least 10 ISO52016 Matrix external validation anchor fixtures", anchorScript);
        Assert.DoesNotContain("does not match files on disk", anchorScript);
        Assert.Contains("verify-iso52016-matrix-external-validation-anchors.ps1", allScript);
        Assert.Contains("SkipExternalValidationAnchors", allScript);
        Assert.Contains("Iso52016MatrixExternalValidationAnchor", allScript);
        Assert.Contains("verify-iso52016-matrix-external-validation-anchors.ps1", releaseScript);
        Assert.Contains("Iso52016MatrixExternalValidationAnchorTests.cs", releaseScript);
    }

    public static IEnumerable<object[]> ManualAnchorFixtureFiles() =>
        LoadManifestFixturePaths()
            .Select(file => new object[] { file });

    private static IEnumerable<string> LoadManifestFixturePaths()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixExternalValidationAnchorsManifest.json");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));

        return document.RootElement
            .GetProperty("fixtures")
            .EnumerateArray()
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => Path.Combine(item!.Split('/').Prepend(repoRoot).ToArray()))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Iso52016MatrixHourlySolverRequest CreateRequest(
        ManualAnchorFixture fixture) =>
        new(
            ZoneCode: fixture.ScenarioName,
            Nodes:
            [
                new Iso52016MatrixNodeDefinition(
                    NodeId: "air",
                    HeatCapacityJPerK: fixture.HeatCapacityJPerK,
                    InitialTemperatureC: fixture.InitialAirTemperatureC,
                    IsAirNode: true)
            ],
            InternalConductances: [],
            BoundaryConductances:
            [
                new Iso52016MatrixBoundaryConductance(
                    NodeId: "air",
                    BoundaryId: "outdoor",
                    ConductanceWPerK: fixture.HeatTransferCoefficientWPerK)
            ],
            Hours:
            [
                new Iso52016MatrixHourlyInputRecord(
                    HourOfYear: 0,
                    Month: 1,
                    Day: 1,
                    Hour: 0,
                    BoundaryTemperaturesC: new Dictionary<string, double>
                    {
                        ["outdoor"] = fixture.OutdoorTemperatureC
                    },
                    NodeHeatGainsW: new Dictionary<string, double>
                    {
                        ["air"] = fixture.InternalHeatGainW
                    },
                    HeatingSetpointC: fixture.HeatingSetpointC,
                    CoolingSetpointC: fixture.CoolingSetpointC)
            ],
            Options: new Iso52016MatrixHourlySolverOptions(
                TimeStepSeconds: fixture.TimeStepSeconds,
                AirNodeId: "air",
                DefaultHeatingSetpointC: fixture.HeatingSetpointC,
                DefaultCoolingSetpointC: fixture.CoolingSetpointC));

    private static ManualExpectation CalculateManualExpectation(
        ManualAnchorFixture fixture)
    {
        var hours = fixture.TimeStepSeconds / 3600.0;

        var heatingLoadW = fixture.Mode.Equals(
            "Heating",
            StringComparison.OrdinalIgnoreCase)
            ? Math.Max(
                0.0,
                fixture.HeatTransferCoefficientWPerK *
                (fixture.HeatingSetpointC - fixture.OutdoorTemperatureC) -
                fixture.InternalHeatGainW)
            : 0.0;

        var coolingLoadW = fixture.Mode.Equals(
            "Cooling",
            StringComparison.OrdinalIgnoreCase)
            ? Math.Max(
                0.0,
                fixture.HeatTransferCoefficientWPerK *
                (fixture.OutdoorTemperatureC - fixture.CoolingSetpointC) +
                fixture.InternalHeatGainW)
            : 0.0;

        return new ManualExpectation(
            HeatingLoadW: heatingLoadW,
            CoolingLoadW: coolingLoadW,
            HeatingEnergyKWh: heatingLoadW * hours / 1000.0,
            CoolingEnergyKWh: coolingLoadW * hours / 1000.0);
    }

    private static ManualAnchorFixture LoadFixture(
        string fixturePath)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var fixture = JsonSerializer.Deserialize<ManualAnchorFixture>(
            File.ReadAllText(fixturePath),
            options);

        Assert.NotNull(fixture);
        return fixture;
    }

    private static void AssertClose(
        double expected,
        double actual) =>
        Assert.InRange(
            actual,
            expected - Tolerance,
            expected + Tolerance);

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

    private sealed record ManualAnchorFixture(
        string AnchorId,
        string ScenarioName,
        string Description,
        string Mode,
        string ManualFormula,
        string SourceType,
        string AuthoritativeReference,
        string MethodologicalBackground,
        string ValidationClaim,
        double TimeStepSeconds,
        double HeatCapacityJPerK,
        double InitialAirTemperatureC,
        double OutdoorTemperatureC,
        double HeatTransferCoefficientWPerK,
        double InternalHeatGainW,
        double HeatingSetpointC,
        double CoolingSetpointC,
        ManualAnchorExpected Expected);

    private sealed record ManualAnchorExpected(
        double HeatingLoadW,
        double CoolingLoadW,
        double HeatingEnergyKWh,
        double CoolingEnergyKWh,
        double AirTemperatureAfterHvacC);

    private sealed record ManualExpectation(
        double HeatingLoadW,
        double CoolingLoadW,
        double HeatingEnergyKWh,
        double CoolingEnergyKWh);
}