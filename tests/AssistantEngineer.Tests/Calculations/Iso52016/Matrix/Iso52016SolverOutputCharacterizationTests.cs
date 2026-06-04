using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public sealed class Iso52016SolverOutputCharacterizationTests
{
    private const double Tolerance = 0.000001;
    private readonly Iso52016MatrixHourlySolver _solver = new();

    [Fact]
    public void Solve_WinterBaselineFixture_RemainsStableAndDeterministic()
    {
        var fixturePath = Path.Combine(
            TestPaths.RepoRoot,
            "tests",
            "AssistantEngineer.Tests",
            "Calculations",
            "Iso52016",
            "Matrix",
            "Baselines",
            "winter-heating-24h.json");

        var fixture = LoadFixture(fixturePath);
        var request = CreateRequest(fixture);

        var first = _solver.Solve(request);
        var second = _solver.Solve(request);

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(second.IsSuccess, second.Error);

        AssertClose(fixture.Expected.AnnualHeatingEnergyKWh, first.Value.AnnualHeatingEnergyKWh);
        AssertClose(fixture.Expected.AnnualCoolingEnergyKWh, first.Value.AnnualCoolingEnergyKWh);
        AssertClose(fixture.Expected.PeakHeatingLoadW, first.Value.PeakHeatingLoadW);
        AssertClose(fixture.Expected.PeakCoolingLoadW, first.Value.PeakCoolingLoadW);

        AssertClose(first.Value.AnnualHeatingEnergyKWh, second.Value.AnnualHeatingEnergyKWh);
        AssertClose(first.Value.AnnualCoolingEnergyKWh, second.Value.AnnualCoolingEnergyKWh);

        Assert.All(first.Value.Hours, hour =>
        {
            Assert.False(double.IsNaN(hour.HeatingLoadW));
            Assert.False(double.IsInfinity(hour.HeatingLoadW));
            Assert.False(double.IsNaN(hour.CoolingLoadW));
            Assert.False(double.IsInfinity(hour.CoolingLoadW));
            Assert.False(double.IsNaN(hour.AirTemperatureAfterHvacC));
            Assert.False(double.IsInfinity(hour.AirTemperatureAfterHvacC));
        });
    }

    private static void AssertClose(double expected, double actual)
    {
        Assert.InRange(actual, expected - Tolerance, expected + Tolerance);
    }

    private static MatrixBaselineFixture LoadFixture(string fixturePath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(fixturePath));
        var root = document.RootElement;
        var expected = root.GetProperty("expected");

        return new MatrixBaselineFixture(
            ScenarioName: root.GetProperty("scenarioName").GetString() ?? "winter-heating-24h",
            HourCount: root.GetProperty("hourCount").GetInt32(),
            OutdoorTemperatureC: root.GetProperty("outdoorTemperatureC").GetDouble(),
            AirNodeHeatGainW: root.GetProperty("airNodeHeatGainW").GetDouble(),
            MassNodeHeatGainW: root.GetProperty("massNodeHeatGainW").GetDouble(),
            InitialAirTemperatureC: root.GetProperty("initialAirTemperatureC").GetDouble(),
            InitialMassTemperatureC: root.GetProperty("initialMassTemperatureC").GetDouble(),
            HeatingSetpointC: root.GetProperty("heatingSetpointC").GetDouble(),
            CoolingSetpointC: root.GetProperty("coolingSetpointC").GetDouble(),
            Expected: new MatrixBaselineExpected(
                AnnualHeatingEnergyKWh: expected.GetProperty("annualHeatingEnergyKWh").GetDouble(),
                AnnualCoolingEnergyKWh: expected.GetProperty("annualCoolingEnergyKWh").GetDouble(),
                PeakHeatingLoadW: expected.GetProperty("peakHeatingLoadW").GetDouble(),
                PeakCoolingLoadW: expected.GetProperty("peakCoolingLoadW").GetDouble()));
    }

    private static Iso52016MatrixHourlySolverRequest CreateRequest(MatrixBaselineFixture fixture)
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
                new Iso52016MatrixNodeDefinition("air", 1_200_000.0, fixture.InitialAirTemperatureC, IsAirNode: true),
                new Iso52016MatrixNodeDefinition("mass", 8_000_000.0, fixture.InitialMassTemperatureC)
            ],
            InternalConductances:
            [
                new Iso52016MatrixConductanceLink("air", "mass", 40.0)
            ],
            BoundaryConductances:
            [
                new Iso52016MatrixBoundaryConductance("air", "outdoor", 90.0),
                new Iso52016MatrixBoundaryConductance("mass", "outdoor", 15.0)
            ],
            Hours: hours,
            Options: new Iso52016MatrixHourlySolverOptions(
                TimeStepSeconds: 3600.0,
                AirNodeId: "air",
                DefaultHeatingSetpointC: fixture.HeatingSetpointC,
                DefaultCoolingSetpointC: fixture.CoolingSetpointC));
    }

    private sealed record MatrixBaselineFixture(
        string ScenarioName,
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
        double PeakCoolingLoadW);
}
