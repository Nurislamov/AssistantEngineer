using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixBaselineFixtureTests
{
    private const double Tolerance = 0.000001;

    private readonly Iso52016MatrixHourlySolver _solver = new();

    [Theory]
    [MemberData(nameof(BaselineFixtureFiles))]
    public void MatrixSolver_MatchesDeterministicBaselineFixture(
        string fixturePath)
    {
        var fixture = LoadFixture(fixturePath);

        var result = _solver.Solve(
            CreateRequest(fixture));

        Assert.True(result.IsSuccess, result.Error);

        var profile = result.Value;

        Assert.Equal(fixture.ScenarioName, profile.ZoneCode);
        Assert.Equal(fixture.HourCount, profile.HourCount);

        AssertClose(
            fixture.Expected.AnnualHeatingEnergyKWh,
            profile.AnnualHeatingEnergyKWh);

        AssertClose(
            fixture.Expected.AnnualCoolingEnergyKWh,
            profile.AnnualCoolingEnergyKWh);

        AssertClose(
            fixture.Expected.PeakHeatingLoadW,
            profile.PeakHeatingLoadW);

        AssertClose(
            fixture.Expected.PeakCoolingLoadW,
            profile.PeakCoolingLoadW);

        AssertClose(
            fixture.Expected.AnnualTotalNodeHeatGainsKWh,
            profile.AnnualTotalNodeHeatGainsKWh);

        foreach (var expectedHour in fixture.Expected.RepresentativeHours)
        {
            var actualHour = profile.Hours.Single(hour =>
                hour.HourOfYear == expectedHour.HourOfYear);

            AssertClose(
                expectedHour.AirTemperatureBeforeHvacC,
                actualHour.AirTemperatureBeforeHvacC);

            AssertClose(
                expectedHour.AirTemperatureAfterHvacC,
                actualHour.AirTemperatureAfterHvacC);

            AssertClose(
                expectedHour.MassTemperatureAfterHvacC,
                actualHour.GetNodeTemperatureAfterHvacC("mass"));

            AssertClose(
                expectedHour.HeatingLoadW,
                actualHour.HeatingLoadW);

            AssertClose(
                expectedHour.CoolingLoadW,
                actualHour.CoolingLoadW);
        }
    }

    [Fact]
    public void BaselineFixtureSet_CoversNeutralHeatingCoolingAndMassLagScenarios()
    {
        var fixtureNames = BaselineFixtureFiles()
            .Select(data => Path.GetFileNameWithoutExtension((string)data[0]))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Contains("neutral-no-hvac", fixtureNames);
        Assert.Contains("winter-heating-24h", fixtureNames);
        Assert.Contains("summer-cooling-24h", fixtureNames);
        Assert.Contains("mass-lag-heating-1h", fixtureNames);
    }

    public static IEnumerable<object[]> BaselineFixtureFiles()
    {
        var baselineDirectory = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "AssistantEngineer.Tests",
            "Calculations",
            "Iso52016",
            "Matrix",
            "Baselines");

        Assert.True(
            Directory.Exists(baselineDirectory),
            $"ISO52016 Matrix baseline directory was not found: {baselineDirectory}");

        return Directory
            .GetFiles(baselineDirectory, "*.json")
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(file => new object[] { file });
    }

    private static Iso52016MatrixHourlySolverRequest CreateRequest(
        MatrixBaselineFixture fixture)
    {
        var hours = Enumerable
            .Range(0, fixture.HourCount)
            .Select(hour => new Iso52016MatrixHourlyInputRecord(
                HourOfYear: hour,
                Month: 1,
                Day: 1,
                Hour: hour % 24,
                BoundaryTemperaturesC: new Dictionary<string, double>
                {
                    ["outdoor"] = fixture.OutdoorTemperatureC
                },
                NodeHeatGainsW: new Dictionary<string, double>
                {
                    ["air"] = fixture.AirNodeHeatGainW,
                    ["mass"] = fixture.MassNodeHeatGainW
                },
                HeatingSetpointC: fixture.HeatingSetpointC,
                CoolingSetpointC: fixture.CoolingSetpointC))
            .ToArray();

        return new Iso52016MatrixHourlySolverRequest(
            ZoneCode: fixture.ScenarioName,
            Nodes:
            [
                new Iso52016MatrixNodeDefinition(
                    NodeId: "air",
                    HeatCapacityJPerK: 1_200_000.0,
                    InitialTemperatureC: fixture.InitialAirTemperatureC,
                    IsAirNode: true),
                new Iso52016MatrixNodeDefinition(
                    NodeId: "mass",
                    HeatCapacityJPerK: 8_000_000.0,
                    InitialTemperatureC: fixture.InitialMassTemperatureC)
            ],
            InternalConductances:
            [
                new Iso52016MatrixConductanceLink(
                    FromNodeId: "air",
                    ToNodeId: "mass",
                    ConductanceWPerK: 40.0)
            ],
            BoundaryConductances:
            [
                new Iso52016MatrixBoundaryConductance(
                    NodeId: "air",
                    BoundaryId: "outdoor",
                    ConductanceWPerK: 90.0),
                new Iso52016MatrixBoundaryConductance(
                    NodeId: "mass",
                    BoundaryId: "outdoor",
                    ConductanceWPerK: 15.0)
            ],
            Hours: hours,
            Options: new Iso52016MatrixHourlySolverOptions(
                TimeStepSeconds: 3600.0,
                AirNodeId: "air",
                DefaultHeatingSetpointC: fixture.HeatingSetpointC,
                DefaultCoolingSetpointC: fixture.CoolingSetpointC));
    }

    private static MatrixBaselineFixture LoadFixture(
        string fixturePath)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var fixture = JsonSerializer.Deserialize<MatrixBaselineFixture>(
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

    private sealed record MatrixBaselineFixture(
        string ScenarioName,
        string Description,
        int HourCount,
        double OutdoorTemperatureC,
        double AirNodeHeatGainW,
        double MassNodeHeatGainW,
        double InitialAirTemperatureC,
        double InitialMassTemperatureC,
        double HeatingSetpointC,
        double CoolingSetpointC,
        MatrixBaselineExpected Expected);

    private sealed record MatrixBaselineExpected(
        double AnnualHeatingEnergyKWh,
        double AnnualCoolingEnergyKWh,
        double PeakHeatingLoadW,
        double PeakCoolingLoadW,
        double AnnualTotalNodeHeatGainsKWh,
        IReadOnlyList<MatrixBaselineExpectedHour> RepresentativeHours);

    private sealed record MatrixBaselineExpectedHour(
        int HourOfYear,
        double AirTemperatureBeforeHvacC,
        double AirTemperatureAfterHvacC,
        double MassTemperatureAfterHvacC,
        double HeatingLoadW,
        double CoolingLoadW);
}