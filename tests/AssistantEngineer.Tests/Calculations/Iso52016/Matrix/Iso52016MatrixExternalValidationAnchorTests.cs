using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixExternalValidationAnchorTests
{
    private const double Tolerance = 0.000001;

    private readonly Iso52016MatrixHourlySolver _solver = new();

    [Theory]
    [MemberData(nameof(ExternalValidationAnchorFixtureFiles))]
    public void MatrixSolver_MatchesIndependentExternalValidationAnchor(
        string fixturePath)
    {
        var fixture = LoadFixture(fixturePath);

        Assert.Equal("ValidationAnchorOnly", fixture.ClaimScope);

        Assert.Contains(
            "No exact pyBuildingEnergy numerical parity claim.",
            fixture.ExplicitNonClaims);

        Assert.Contains(
            "No exact EnergyPlus numerical parity claim.",
            fixture.ExplicitNonClaims);

        Assert.Contains(
            "No ASHRAE 140 validation coverage claim.",
            fixture.ExplicitNonClaims);

        if (fixture.Kind.Equals(
            "singleHourSteadyState",
            StringComparison.OrdinalIgnoreCase))
        {
            AssertSingleHourFixture(fixture);
            return;
        }

        if (fixture.Kind.Equals(
            "annual8760ManualReference",
            StringComparison.OrdinalIgnoreCase))
        {
            AssertAnnual8760Fixture(fixture);
            return;
        }

        throw new InvalidOperationException(
            $"Unsupported ISO52016 Matrix external validation anchor kind: {fixture.Kind}");
    }

    [Fact]
    public void ExternalValidationAnchorSet_CoversManualPbeStyleEnergyPlusStyleAndAnnual8760()
    {
        var fixtures = ExternalValidationAnchorFixtureFiles()
            .Select(data => LoadFixture((string)data[0]))
            .ToArray();

        Assert.Contains(fixtures, fixture => fixture.ScenarioName == "manual-independent-steady-heating");
        Assert.Contains(fixtures, fixture => fixture.ScenarioName == "manual-independent-steady-heating-with-gains");
        Assert.Contains(fixtures, fixture => fixture.ScenarioName == "manual-independent-steady-cooling");
        Assert.Contains(fixtures, fixture => fixture.ReferenceStyle == "pyBuildingEnergy-style naming only");
        Assert.Contains(fixtures, fixture => fixture.ReferenceStyle == "EnergyPlus-style naming only");
        Assert.Contains(fixtures, fixture => fixture.Kind == "annual8760ManualReference" && fixture.HourCount == 8760);
    }

    [Fact]
    public void ExternalValidationAnchorDocsAndManifest_DoNotClaimFullParity()
    {
        var repoRoot = FindRepositoryRoot();

        var documentationPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016MatrixExternalValidationAnchors.md");

        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixExternalValidationAnchorsManifest.json");

        Assert.True(File.Exists(documentationPath), $"Documentation was not found: {documentationPath}");
        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        var documentation = File.ReadAllText(documentationPath);
        var manifest = File.ReadAllText(manifestPath);

        Assert.Contains("validation anchors only", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No exact pyBuildingEnergy numerical parity claim", documentation);
        Assert.Contains("No exact EnergyPlus numerical parity claim", documentation);
        Assert.Contains("No ASHRAE 140 validation coverage claim", documentation);

        using var document = JsonDocument.Parse(manifest);
        var root = document.RootElement;

        Assert.Equal(
            "ValidationAnchorOnly",
            root.GetProperty("claimScope").GetString());

        var nonClaims = root
            .GetProperty("explicitNonClaims")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("No full pyBuildingEnergy parity claim.", nonClaims);
        Assert.Contains("No full EnergyPlus parity claim.", nonClaims);
        Assert.Contains("No ASHRAE 140 validation coverage claim.", nonClaims);
    }

    [Fact]
    public void ExternalValidationAnchorVerification_IsPartOfAllAndReleaseReadyScripts()
    {
        var repoRoot = FindRepositoryRoot();

        var allScriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-all.ps1");

        var releaseScriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "assert-iso52016-matrix-release-ready.ps1");

        Assert.True(File.Exists(allScriptPath), $"All verification script was not found: {allScriptPath}");
        Assert.True(File.Exists(releaseScriptPath), $"Release-ready script was not found: {releaseScriptPath}");

        var allScript = File.ReadAllText(allScriptPath);
        var releaseScript = File.ReadAllText(releaseScriptPath);

        Assert.Contains("verify-iso52016-matrix-external-validation-anchors.ps1", allScript);
        Assert.Contains("SkipExternalValidationAnchors", allScript);
        Assert.Contains("Iso52016MatrixExternalValidationAnchor", allScript);

        Assert.Contains("verify-iso52016-matrix-external-validation-anchors.ps1", releaseScript);
        Assert.Contains("Iso52016MatrixExternalValidationAnchorsManifest.json", releaseScript);
        Assert.Contains("Iso52016MatrixExternalValidationAnchorTests.cs", releaseScript);
    }

    public static IEnumerable<object[]> ExternalValidationAnchorFixtureFiles()
    {
        var anchorDirectory = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "AssistantEngineer.Tests",
            "Calculations",
            "Iso52016",
            "Matrix",
            "ExternalValidationAnchors");

        Assert.True(
            Directory.Exists(anchorDirectory),
            $"ISO52016 Matrix external validation anchor directory was not found: {anchorDirectory}");

        return Directory
            .GetFiles(anchorDirectory, "*.json")
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(file => new object[] { file });
    }

    private void AssertSingleHourFixture(
        ExternalValidationAnchorFixture fixture)
    {
        var manual = CalculateSingleHourManualExpectation(fixture);

        AssertClose(fixture.Expected.HeatingLoadW, manual.HeatingLoadW);
        AssertClose(fixture.Expected.CoolingLoadW, manual.CoolingLoadW);
        AssertClose(fixture.Expected.HeatingEnergyKWh, manual.HeatingEnergyKWh);
        AssertClose(fixture.Expected.CoolingEnergyKWh, manual.CoolingEnergyKWh);

        var result = _solver.Solve(
            CreateSingleHourRequest(fixture));

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

    private void AssertAnnual8760Fixture(
        ExternalValidationAnchorFixture fixture)
    {
        var manual = CalculateAnnualManualExpectation(fixture);

        Assert.Equal(fixture.Expected.HourCount, manual.HourCount);
        Assert.Equal(fixture.HourCount.GetValueOrDefault(), manual.HourCount);
        AssertClose(fixture.Expected.AnnualHeatingEnergyKWh, manual.HeatingEnergyKWh);
        AssertClose(fixture.Expected.AnnualCoolingEnergyKWh, manual.CoolingEnergyKWh);
        AssertClose(fixture.Expected.PeakHeatingLoadW, manual.PeakHeatingLoadW);
        AssertClose(fixture.Expected.PeakCoolingLoadW, manual.PeakCoolingLoadW);

        var result = _solver.Solve(
            CreateAnnualRequest(fixture));

        Assert.True(result.IsSuccess, result.Error);

        var profile = result.Value;

        Assert.Equal(fixture.ScenarioName, profile.ZoneCode);
        Assert.Equal(fixture.HourCount.GetValueOrDefault(), profile.HourCount);
        Assert.Equal(12, profile.MonthlySummaries.Count);

        AssertClose(manual.HeatingEnergyKWh, profile.AnnualHeatingEnergyKWh);
        AssertClose(manual.CoolingEnergyKWh, profile.AnnualCoolingEnergyKWh);
        AssertClose(manual.PeakHeatingLoadW, profile.PeakHeatingLoadW);
        AssertClose(manual.PeakCoolingLoadW, profile.PeakCoolingLoadW);
    }

    private static Iso52016MatrixHourlySolverRequest CreateSingleHourRequest(
        ExternalValidationAnchorFixture fixture) =>
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

    private static Iso52016MatrixHourlySolverRequest CreateAnnualRequest(
        ExternalValidationAnchorFixture fixture) =>
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
            Hours: BuildAnnualHours(fixture),
            Options: new Iso52016MatrixHourlySolverOptions(
                TimeStepSeconds: fixture.TimeStepSeconds,
                AirNodeId: "air",
                DefaultHeatingSetpointC: fixture.HeatingSetpointC,
                DefaultCoolingSetpointC: fixture.CoolingSetpointC));

    private static IReadOnlyList<Iso52016MatrixHourlyInputRecord> BuildAnnualHours(
        ExternalValidationAnchorFixture fixture)
    {
        var pattern = fixture.AnnualOutdoorTemperaturePattern ??
            throw new InvalidOperationException("Annual fixture must include an outdoor temperature pattern.");

        var hours = new List<Iso52016MatrixHourlyInputRecord>(
            fixture.HourCount ?? 0);

        var hourOfYear = 0;

        for (var monthIndex = 0; monthIndex < pattern.MonthlyOutdoorTemperatureC.Count; monthIndex++)
        {
            var month = monthIndex + 1;
            var outdoorTemperatureC = pattern.MonthlyOutdoorTemperatureC[monthIndex];

            for (var blockHour = 0; blockHour < pattern.HoursPerMonthBlock; blockHour++)
            {
                hours.Add(
                    new Iso52016MatrixHourlyInputRecord(
                        HourOfYear: hourOfYear,
                        Month: month,
                        Day: blockHour / 24 + 1,
                        Hour: blockHour % 24,
                        BoundaryTemperaturesC: new Dictionary<string, double>
                        {
                            ["outdoor"] = outdoorTemperatureC
                        },
                        NodeHeatGainsW: new Dictionary<string, double>
                        {
                            ["air"] = fixture.InternalHeatGainW
                        },
                        HeatingSetpointC: fixture.HeatingSetpointC,
                        CoolingSetpointC: fixture.CoolingSetpointC));

                hourOfYear++;
            }
        }

        Assert.Equal(fixture.HourCount.GetValueOrDefault(), hours.Count);
        return hours;
    }

    private static ManualExpectation CalculateSingleHourManualExpectation(
        ExternalValidationAnchorFixture fixture)
    {
        var secondsToHours = fixture.TimeStepSeconds / 3600.0;

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
            HourCount: 1,
            HeatingLoadW: heatingLoadW,
            CoolingLoadW: coolingLoadW,
            HeatingEnergyKWh: heatingLoadW * secondsToHours / 1000.0,
            CoolingEnergyKWh: coolingLoadW * secondsToHours / 1000.0,
            PeakHeatingLoadW: heatingLoadW,
            PeakCoolingLoadW: coolingLoadW);
    }

    private static ManualExpectation CalculateAnnualManualExpectation(
        ExternalValidationAnchorFixture fixture)
    {
        var pattern = fixture.AnnualOutdoorTemperaturePattern ??
            throw new InvalidOperationException("Annual fixture must include an outdoor temperature pattern.");

        var capacityTermWPerK = fixture.HeatCapacityJPerK / fixture.TimeStepSeconds;
        var denominator = capacityTermWPerK + fixture.HeatTransferCoefficientWPerK;
        var secondsToHours = fixture.TimeStepSeconds / 3600.0;

        var previousAirTemperatureC = fixture.InitialAirTemperatureC;
        var hourCount = 0;
        var heatingEnergyKWh = 0.0;
        var coolingEnergyKWh = 0.0;
        var peakHeatingLoadW = 0.0;
        var peakCoolingLoadW = 0.0;

        foreach (var outdoorTemperatureC in pattern.MonthlyOutdoorTemperatureC)
        {
            for (var blockHour = 0; blockHour < pattern.HoursPerMonthBlock; blockHour++)
            {
                var freeFloatingAirTemperatureC =
                    (capacityTermWPerK * previousAirTemperatureC +
                     fixture.HeatTransferCoefficientWPerK * outdoorTemperatureC +
                     fixture.InternalHeatGainW) /
                    denominator;

                var heatingLoadW = 0.0;
                var coolingLoadW = 0.0;
                var airTemperatureAfterHvacC = freeFloatingAirTemperatureC;

                if (freeFloatingAirTemperatureC < fixture.HeatingSetpointC)
                {
                    heatingLoadW =
                        (fixture.HeatingSetpointC - freeFloatingAirTemperatureC) *
                        denominator;

                    airTemperatureAfterHvacC = fixture.HeatingSetpointC;
                }
                else if (freeFloatingAirTemperatureC > fixture.CoolingSetpointC)
                {
                    coolingLoadW =
                        (freeFloatingAirTemperatureC - fixture.CoolingSetpointC) *
                        denominator;

                    airTemperatureAfterHvacC = fixture.CoolingSetpointC;
                }

                heatingEnergyKWh += heatingLoadW * secondsToHours / 1000.0;
                coolingEnergyKWh += coolingLoadW * secondsToHours / 1000.0;
                peakHeatingLoadW = Math.Max(peakHeatingLoadW, heatingLoadW);
                peakCoolingLoadW = Math.Max(peakCoolingLoadW, coolingLoadW);

                previousAirTemperatureC = airTemperatureAfterHvacC;
                hourCount++;
            }
        }

        return new ManualExpectation(
            HourCount: hourCount,
            HeatingLoadW: 0.0,
            CoolingLoadW: 0.0,
            HeatingEnergyKWh: heatingEnergyKWh,
            CoolingEnergyKWh: coolingEnergyKWh,
            PeakHeatingLoadW: peakHeatingLoadW,
            PeakCoolingLoadW: peakCoolingLoadW);
    }

    private static ExternalValidationAnchorFixture LoadFixture(
        string fixturePath)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var fixture = JsonSerializer.Deserialize<ExternalValidationAnchorFixture>(
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

    private sealed record ExternalValidationAnchorFixture(
        string AnchorId,
        string ScenarioName,
        string ReferenceFamily,
        string ReferenceStyle,
        string ClaimScope,
        IReadOnlyList<string> ExplicitNonClaims,
        string Description,
        string Kind,
        string Mode,
        double TimeStepSeconds,
        double HeatCapacityJPerK,
        double InitialAirTemperatureC,
        double OutdoorTemperatureC,
        double HeatTransferCoefficientWPerK,
        double InternalHeatGainW,
        double HeatingSetpointC,
        double CoolingSetpointC,
        int? HourCount,
        AnnualOutdoorTemperaturePattern? AnnualOutdoorTemperaturePattern,
        ExternalValidationAnchorExpected Expected);

    private sealed record AnnualOutdoorTemperaturePattern(
        string Type,
        int HoursPerMonthBlock,
        IReadOnlyList<double> MonthlyOutdoorTemperatureC);

    private sealed record ExternalValidationAnchorExpected(
        double HeatingLoadW,
        double CoolingLoadW,
        double HeatingEnergyKWh,
        double CoolingEnergyKWh,
        double AirTemperatureAfterHvacC,
        int HourCount,
        double AnnualHeatingEnergyKWh,
        double AnnualCoolingEnergyKWh,
        double PeakHeatingLoadW,
        double PeakCoolingLoadW);

    private sealed record ManualExpectation(
        int HourCount,
        double HeatingLoadW,
        double CoolingLoadW,
        double HeatingEnergyKWh,
        double CoolingEnergyKWh,
        double PeakHeatingLoadW,
        double PeakCoolingLoadW);
}