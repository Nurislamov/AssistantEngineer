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
    public void MatrixSolver_MatchesIndependentManualAnchorFormula(
        string fixturePath)
    {
        var fixture = LoadFixture(fixturePath);

        Assert.Equal("ManualEngineeringValidationAnchor", fixture.SourceType);
        Assert.Equal("IndependentManualEngineeringFormula", fixture.AuthoritativeReference);
        Assert.Equal("ValidationAnchorsOnly", fixture.Scope);

        var manual = CalculateManualExpectation(fixture);

        AssertClose(manual.HeatingLoadW, fixture.Expected.HeatingLoadW);
        AssertClose(manual.CoolingLoadW, fixture.Expected.CoolingLoadW);
        AssertClose(manual.HeatingEnergyKWh, fixture.Expected.HeatingEnergyKWh);
        AssertClose(manual.CoolingEnergyKWh, fixture.Expected.CoolingEnergyKWh);
        AssertClose(manual.AnnualHeatingEnergyKWh, fixture.Expected.AnnualHeatingEnergyKWh);
        AssertClose(manual.AnnualCoolingEnergyKWh, fixture.Expected.AnnualCoolingEnergyKWh);
        AssertClose(manual.AirTemperatureBeforeHvacC, fixture.Expected.AirTemperatureBeforeHvacC);
        AssertClose(manual.AirTemperatureAfterHvacC, fixture.Expected.AirTemperatureAfterHvacC);

        var result = _solver.Solve(CreateRequest(fixture));

        Assert.True(result.IsSuccess, result.Error);

        var profile = result.Value;

        Assert.Equal(fixture.ScenarioName, profile.ZoneCode);
        Assert.Equal(fixture.HourCount, profile.HourCount);

        var firstHour = profile.Hours[0];
        var lastHour = profile.Hours[^1];

        AssertHourMatchesManual(firstHour, manual);
        AssertHourMatchesManual(lastHour, manual);

        AssertClose(manual.AnnualHeatingEnergyKWh, profile.AnnualHeatingEnergyKWh);
        AssertClose(manual.AnnualCoolingEnergyKWh, profile.AnnualCoolingEnergyKWh);
        AssertClose(manual.HeatingLoadW, profile.PeakHeatingLoadW);
        AssertClose(manual.CoolingLoadW, profile.PeakCoolingLoadW);

        if (fixture.HourCount == 8760)
        {
            Assert.Equal("MANUAL-ISO52016-ANNUAL-8760-001", fixture.AnchorId);
            Assert.Equal(12, profile.MonthlySummaries.Count);
        }
    }

    [Fact]
    public void ExternalValidationAnchorFixtureSet_CoversRequiredManualAnchors()
    {
        var fixtures = ExternalValidationAnchorFixtureFiles()
            .Select(data => LoadFixture((string)data[0]))
            .ToArray();

        var anchorIds = fixtures
            .Select(fixture => fixture.AnchorId)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Contains("MANUAL-ISO52016-ANCHOR-001", anchorIds);
        Assert.Contains("MANUAL-ISO52016-ANCHOR-002", anchorIds);
        Assert.Contains("MANUAL-ISO52016-ANCHOR-003", anchorIds);
        Assert.Contains("MANUAL-ISO52016-ANCHOR-004", anchorIds);
        Assert.Contains("MANUAL-ISO52016-ANNUAL-8760-001", anchorIds);

        Assert.Equal(anchorIds.Length, anchorIds.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    public static IEnumerable<object[]> ExternalValidationAnchorFixtureFiles()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixExternalValidationAnchorsManifest.json");

        Assert.True(
            File.Exists(manifestPath),
            $"ISO52016 Matrix external validation anchors manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));

        var fixtureFiles = document.RootElement
            .GetProperty("fixtures")
            .EnumerateArray()
            .Select(item => item.GetString())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(relativePath => Path.Combine(relativePath!.Split('/').Prepend(repoRoot).ToArray()))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return fixtureFiles
            .Select(file => new object[] { file })
            .ToArray();
    }

    private static Iso52016MatrixHourlySolverRequest CreateRequest(
        ExternalValidationAnchorFixture fixture)
    {
        var start = new DateTime(2025, 1, 1, 0, 0, 0);

        var hours = Enumerable
            .Range(0, fixture.HourCount)
            .Select(hourIndex =>
            {
                var timestamp = start.AddHours(hourIndex);

                return new Iso52016MatrixHourlyInputRecord(
                    HourOfYear: hourIndex,
                    Month: timestamp.Month,
                    Day: timestamp.Day,
                    Hour: timestamp.Hour,
                    BoundaryTemperaturesC: new Dictionary<string, double>
                    {
                        ["outdoor"] = fixture.OutdoorTemperatureC
                    },
                    NodeHeatGainsW: new Dictionary<string, double>
                    {
                        ["air"] = fixture.InternalHeatGainW
                    },
                    HeatingSetpointC: fixture.HeatingSetpointC,
                    CoolingSetpointC: fixture.CoolingSetpointC);
            })
            .ToArray();

        return new Iso52016MatrixHourlySolverRequest(
            ZoneCode: fixture.ScenarioName,
            Nodes: new[]
            {
                new Iso52016MatrixNodeDefinition(
                    NodeId: "air",
                    HeatCapacityJPerK: fixture.HeatCapacityJPerK,
                    InitialTemperatureC: fixture.InitialAirTemperatureC,
                    IsAirNode: true)
            },
            InternalConductances: Array.Empty<Iso52016MatrixConductanceLink>(),
            BoundaryConductances: new[]
            {
                new Iso52016MatrixBoundaryConductance(
                    NodeId: "air",
                    BoundaryId: "outdoor",
                    ConductanceWPerK: fixture.HeatTransferCoefficientWPerK)
            },
            Hours: hours,
            Options: new Iso52016MatrixHourlySolverOptions(
                TimeStepSeconds: fixture.TimeStepSeconds,
                AirNodeId: "air",
                DefaultHeatingSetpointC: fixture.HeatingSetpointC,
                DefaultCoolingSetpointC: fixture.CoolingSetpointC));
    }

    private static ManualExpectation CalculateManualExpectation(
        ExternalValidationAnchorFixture fixture)
    {
        var hoursPerStep = fixture.TimeStepSeconds / 3600.0;
        var coefficientWPerK = fixture.HeatCapacityJPerK / fixture.TimeStepSeconds;
        var denominator = coefficientWPerK + fixture.HeatTransferCoefficientWPerK;

        var freeFloatingAirTemperatureC =
            (coefficientWPerK * fixture.InitialAirTemperatureC +
             fixture.HeatTransferCoefficientWPerK * fixture.OutdoorTemperatureC +
             fixture.InternalHeatGainW) /
            denominator;

        var heatingLoadW = 0.0;
        var coolingLoadW = 0.0;
        var airTemperatureAfterHvacC = freeFloatingAirTemperatureC;

        if (fixture.Mode.Equals("SteadyHeating", StringComparison.OrdinalIgnoreCase) ||
            fixture.Mode.Equals("AnnualConstantHeating", StringComparison.OrdinalIgnoreCase))
        {
            heatingLoadW = Math.Max(
                0.0,
                fixture.HeatTransferCoefficientWPerK *
                (fixture.HeatingSetpointC - fixture.OutdoorTemperatureC) -
                fixture.InternalHeatGainW);

            airTemperatureAfterHvacC = fixture.HeatingSetpointC;
        }
        else if (fixture.Mode.Equals("SteadyCooling", StringComparison.OrdinalIgnoreCase))
        {
            coolingLoadW = Math.Max(
                0.0,
                fixture.HeatTransferCoefficientWPerK *
                (fixture.OutdoorTemperatureC - fixture.CoolingSetpointC) +
                fixture.InternalHeatGainW);

            airTemperatureAfterHvacC = fixture.CoolingSetpointC;
        }
        else if (fixture.Mode.Equals("FreeFloatingNoHvac", StringComparison.OrdinalIgnoreCase))
        {
            Assert.InRange(
                freeFloatingAirTemperatureC,
                fixture.HeatingSetpointC,
                fixture.CoolingSetpointC);
        }
        else
        {
            throw new InvalidOperationException($"Unknown external validation anchor mode: {fixture.Mode}");
        }

        var heatingEnergyKWh = heatingLoadW * hoursPerStep / 1000.0;
        var coolingEnergyKWh = coolingLoadW * hoursPerStep / 1000.0;

        return new ManualExpectation(
            HeatingLoadW: heatingLoadW,
            CoolingLoadW: coolingLoadW,
            HeatingEnergyKWh: heatingEnergyKWh,
            CoolingEnergyKWh: coolingEnergyKWh,
            AnnualHeatingEnergyKWh: heatingEnergyKWh * fixture.HourCount,
            AnnualCoolingEnergyKWh: coolingEnergyKWh * fixture.HourCount,
            AirTemperatureBeforeHvacC: freeFloatingAirTemperatureC,
            AirTemperatureAfterHvacC: airTemperatureAfterHvacC);
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

    private static void AssertHourMatchesManual(
        Iso52016MatrixHourlyResult hour,
        ManualExpectation manual)
    {
        AssertClose(manual.HeatingLoadW, hour.HeatingLoadW);
        AssertClose(manual.CoolingLoadW, hour.CoolingLoadW);
        AssertClose(manual.HeatingEnergyKWh, hour.HeatingEnergyKWh);
        AssertClose(manual.CoolingEnergyKWh, hour.CoolingEnergyKWh);
        AssertClose(manual.AirTemperatureBeforeHvacC, hour.AirTemperatureBeforeHvacC);
        AssertClose(manual.AirTemperatureAfterHvacC, hour.AirTemperatureAfterHvacC);
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
        string DisplayName,
        string Description,
        string SourceType,
        string AuthoritativeReference,
        string Scope,
        string Mode,
        string Formula,
        double TimeStepSeconds,
        int HourCount,
        double HeatCapacityJPerK,
        double InitialAirTemperatureC,
        double OutdoorTemperatureC,
        double HeatTransferCoefficientWPerK,
        double InternalHeatGainW,
        double HeatingSetpointC,
        double CoolingSetpointC,
        ExternalValidationExpected Expected);

    private sealed record ExternalValidationExpected(
        double HeatingLoadW,
        double CoolingLoadW,
        double HeatingEnergyKWh,
        double CoolingEnergyKWh,
        double AnnualHeatingEnergyKWh,
        double AnnualCoolingEnergyKWh,
        double AirTemperatureBeforeHvacC,
        double AirTemperatureAfterHvacC);

    private sealed record ManualExpectation(
        double HeatingLoadW,
        double CoolingLoadW,
        double HeatingEnergyKWh,
        double CoolingEnergyKWh,
        double AnnualHeatingEnergyKWh,
        double AnnualCoolingEnergyKWh,
        double AirTemperatureBeforeHvacC,
        double AirTemperatureAfterHvacC);
}