using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixExternalValidationFixtureTests
{
    private const double Tolerance = 0.000001;

    private readonly Iso52016MatrixHourlySolver _solver = new();

    [Theory]
    [MemberData(nameof(ExternalValidationFixtureFiles))]
    public void MatrixSolver_MatchesIndependentManualSteadyStateFormula(
        string fixturePath)
    {
        var fixture = LoadFixture(fixturePath);

        var manual = CalculateManualExpectation(fixture);

        AssertClose(fixture.Expected.HeatingLoadW, manual.HeatingLoadW);
        AssertClose(fixture.Expected.CoolingLoadW, manual.CoolingLoadW);
        AssertClose(fixture.Expected.HeatingEnergyKWh, manual.HeatingEnergyKWh);
        AssertClose(fixture.Expected.CoolingEnergyKWh, manual.CoolingEnergyKWh);

        var result = _solver.Solve(
            CreateRequest(fixture));

        Assert.True(result.IsSuccess, result.Error);

        var profile = result.Value;
        var hour = Assert.Single(profile.Hours);

        Assert.Equal(fixture.ScenarioName, profile.ZoneCode);
        Assert.Equal(1, profile.HourCount);

        AssertClose(manual.HeatingLoadW, hour.HeatingLoadW);
        AssertClose(manual.CoolingLoadW, hour.CoolingLoadW);
        AssertClose(manual.HeatingEnergyKWh, hour.HeatingEnergyKWh);
        AssertClose(manual.CoolingEnergyKWh, hour.CoolingEnergyKWh);

        AssertClose(manual.HeatingEnergyKWh, profile.AnnualHeatingEnergyKWh);
        AssertClose(manual.CoolingEnergyKWh, profile.AnnualCoolingEnergyKWh);
        AssertClose(manual.HeatingLoadW, profile.PeakHeatingLoadW);
        AssertClose(manual.CoolingLoadW, profile.PeakCoolingLoadW);
        AssertClose(fixture.Expected.AirTemperatureAfterHvacC, hour.AirTemperatureAfterHvacC);
    }

    [Fact]
    public void ExternalValidationFixtureSet_CoversHeatingCoolingAndInternalGainCases()
    {
        var fixtureNames = ExternalValidationFixtureFiles()
            .Select(data => Path.GetFileNameWithoutExtension((string)data[0]))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Contains("manual-steady-state-heating", fixtureNames);
        Assert.Contains("manual-steady-state-heating-with-gains", fixtureNames);
        Assert.Contains("manual-steady-state-cooling", fixtureNames);
        Assert.Contains("manual-steady-state-cooling-with-gains", fixtureNames);
    }

    public static IEnumerable<object[]> ExternalValidationFixtureFiles()
    {
        var baselineDirectory = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "AssistantEngineer.Tests",
            "Calculations",
            "Iso52016",
            "Matrix",
            "ExternalValidation");

        Assert.True(
            Directory.Exists(baselineDirectory),
            $"ISO52016 Matrix external validation directory was not found: {baselineDirectory}");

        return Directory
            .GetFiles(baselineDirectory, "*.json")
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(file => new object[] { file });
    }

    private static Iso52016MatrixHourlySolverRequest CreateRequest(
        ExternalValidationFixture fixture) =>
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
        ExternalValidationFixture fixture)
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
            HeatingLoadW: heatingLoadW,
            CoolingLoadW: coolingLoadW,
            HeatingEnergyKWh: heatingLoadW * secondsToHours / 1000.0,
            CoolingEnergyKWh: coolingLoadW * secondsToHours / 1000.0);
    }

    private static ExternalValidationFixture LoadFixture(
        string fixturePath)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var fixture = JsonSerializer.Deserialize<ExternalValidationFixture>(
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

    private sealed record ExternalValidationFixture(
        string ScenarioName,
        string Description,
        string Mode,
        double TimeStepSeconds,
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
        double AirTemperatureAfterHvacC);

    private sealed record ManualExpectation(
        double HeatingLoadW,
        double CoolingLoadW,
        double HeatingEnergyKWh,
        double CoolingEnergyKWh);
}