using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public sealed class Iso52016MatrixEngineeringEdgeCaseTests
{
    private const double Tolerance = 0.000001;

    private readonly Iso52016MatrixHourlySolver _solver = new();

    [Fact]
    public void EngineeringEdgeCaseManifest_ListsEveryFixtureAndKeepsNonClaimsExplicit()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixEngineeringEdgeCasesManifest.json");

        Assert.True(File.Exists(manifestPath), $"Engineering edge-case manifest was not found: {manifestPath}");

        var manifestText = File.ReadAllText(manifestPath);

        Assert.DoesNotContain("\"ExternalParityCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"FullParityCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"pyBuildingEnergyParityCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"EnergyPlusParityCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(manifestText);
        var root = document.RootElement;

        Assert.Equal("ISO52016-MATRIX-ENGINEERING-EDGE-CASES", root.GetProperty("stageId").GetString());
        Assert.Equal("EngineeringHardeningOnly", root.GetProperty("scope").GetString());
        Assert.True(root.GetProperty("multiNodeResponseAnchorsIntegrated").GetBoolean());
        Assert.True(root.GetProperty("adjacentUnconditionedBoundaryAnchorsIntegrated").GetBoolean());
        Assert.True(root.GetProperty("timeStepSensitivityAnchorsIntegrated").GetBoolean());
        Assert.True(root.GetProperty("signConventionAnchorsIntegrated").GetBoolean());
        Assert.True(root.GetProperty("aggregationEdgeCaseAnchorsIntegrated").GetBoolean());

        var fixtures = root
            .GetProperty("fixtures")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Equal(5, fixtures.Length);

        foreach (var fixture in fixtures)
        {
            Assert.False(string.IsNullOrWhiteSpace(fixture));

            var fixturePath = Path.Combine(fixture!.Split('/').Prepend(repoRoot).ToArray());
            Assert.True(File.Exists(fixturePath), $"Engineering edge-case fixture was not found: {fixture}");
        }

        var nonClaims = root
            .GetProperty("explicitNonClaims")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("Engineering edge-case hardening only.", nonClaims);
        Assert.Contains("Validation anchors only, not full parity.", nonClaims);
        Assert.Contains("No pyBuildingEnergy parity claim.", nonClaims);
        Assert.Contains("No EnergyPlus parity claim.", nonClaims);
        Assert.Contains("No ASHRAE 140 validation coverage claim.", nonClaims);
        Assert.Contains("No full ISO 52016 parity claim.", nonClaims);
    }

    [Fact]
    public void TwoNodeFreeFloatingResponse_MatchesIndependentImplicitEulerFormula()
    {
        using var document = LoadFixture("engineering-iso52016-matrix-edge-001-two-node-free-floating.json");
        var fixture = document.RootElement;

        var result = _solver.Solve(CreateRequest(fixture));

        Assert.True(result.IsSuccess, result.Error);

        var profile = result.Value;
        var hour = Assert.Single(profile.Hours);

        var expected = CalculateTwoNodeImplicitEulerExpectation(fixture);

        AssertClose(expected.AirTemperatureC, hour.AirTemperatureBeforeHvacC);
        AssertClose(expected.AirTemperatureC, hour.AirTemperatureAfterHvacC);
        AssertClose(expected.MassTemperatureC, hour.NodeStates.Single(state => state.NodeId == "mass").TemperatureAfterHvacC);
        AssertClose(0.0, hour.HeatingLoadW);
        AssertClose(0.0, hour.CoolingLoadW);
        AssertClose(0.0, profile.AnnualHeatingEnergyKWh);
        AssertClose(0.0, profile.AnnualCoolingEnergyKWh);
    }

    [Fact]
    public void AdjacentUnconditionedBoundaryHeatingLoad_MatchesHDeltaT()
    {
        using var document = LoadFixture("engineering-iso52016-matrix-edge-002-adjacent-unconditioned-boundary.json");
        var fixture = document.RootElement;

        var result = _solver.Solve(CreateRequest(fixture));

        Assert.True(result.IsSuccess, result.Error);

        var hour = Assert.Single(result.Value.Hours);

        var expectedHeatingLoadW = fixture.GetProperty("expectedHeatingLoadW").GetDouble();

        AssertClose(expectedHeatingLoadW, hour.HeatingLoadW);
        AssertClose(0.0, hour.CoolingLoadW);
        AssertClose(20.0, hour.AirTemperatureAfterHvacC);
        Assert.True(hour.AirTemperatureBeforeHvacC < 20.0);
    }

    [Fact]
    public void TimeStepEnergyScaling_KeepsSteadyLoadAndScalesEnergy()
    {
        using var document = LoadFixture("engineering-iso52016-matrix-edge-003-timestep-energy-scaling.json");
        var fixture = document.RootElement;

        var timeSteps = fixture
            .GetProperty("timeStepSecondsCases")
            .EnumerateArray()
            .Select(item => item.GetDouble())
            .ToArray();

        Assert.Equal(2, timeSteps.Length);

        var halfHour = SolveSingleNodeSteadyCase(
            heatTransferCoefficientWPerK: fixture.GetProperty("heatTransferCoefficientWPerK").GetDouble(),
            initialAirTemperatureC: fixture.GetProperty("initialAirTemperatureC").GetDouble(),
            boundaryTemperatureC: fixture.GetProperty("outdoorTemperatureC").GetDouble(),
            internalHeatGainW: fixture.GetProperty("internalHeatGainW").GetDouble(),
            heatingSetpointC: fixture.GetProperty("heatingSetpointC").GetDouble(),
            coolingSetpointC: fixture.GetProperty("coolingSetpointC").GetDouble(),
            timeStepSeconds: timeSteps[0]);

        var fullHour = SolveSingleNodeSteadyCase(
            heatTransferCoefficientWPerK: fixture.GetProperty("heatTransferCoefficientWPerK").GetDouble(),
            initialAirTemperatureC: fixture.GetProperty("initialAirTemperatureC").GetDouble(),
            boundaryTemperatureC: fixture.GetProperty("outdoorTemperatureC").GetDouble(),
            internalHeatGainW: fixture.GetProperty("internalHeatGainW").GetDouble(),
            heatingSetpointC: fixture.GetProperty("heatingSetpointC").GetDouble(),
            coolingSetpointC: fixture.GetProperty("coolingSetpointC").GetDouble(),
            timeStepSeconds: timeSteps[1]);

        var expectedHeatingLoadW = fixture.GetProperty("expectedHeatingLoadW").GetDouble();

        AssertClose(expectedHeatingLoadW, halfHour.HeatingLoadW);
        AssertClose(expectedHeatingLoadW, fullHour.HeatingLoadW);
        AssertClose(2.0 * halfHour.HeatingEnergyKWh, fullHour.HeatingEnergyKWh);
    }

    [Fact]
    public void PositiveInternalGains_ReduceHeatingAndIncreaseCooling()
    {
        using var document = LoadFixture("engineering-iso52016-matrix-edge-004-gain-sign-conventions.json");
        var fixture = document.RootElement;
        var h = fixture.GetProperty("heatTransferCoefficientWPerK").GetDouble();
        var gains = fixture.GetProperty("internalHeatGainW").GetDouble();

        var heating = fixture.GetProperty("heatingScenario");
        var heatingWithoutGains = SolveSingleNodeSteadyCase(
            h,
            heating.GetProperty("initialAirTemperatureC").GetDouble(),
            heating.GetProperty("outdoorTemperatureC").GetDouble(),
            0.0,
            heating.GetProperty("heatingSetpointC").GetDouble(),
            heating.GetProperty("coolingSetpointC").GetDouble(),
            3600.0);

        var heatingWithGains = SolveSingleNodeSteadyCase(
            h,
            heating.GetProperty("initialAirTemperatureC").GetDouble(),
            heating.GetProperty("outdoorTemperatureC").GetDouble(),
            gains,
            heating.GetProperty("heatingSetpointC").GetDouble(),
            heating.GetProperty("coolingSetpointC").GetDouble(),
            3600.0);

        AssertClose(heating.GetProperty("expectedWithoutGainsW").GetDouble(), heatingWithoutGains.HeatingLoadW);
        AssertClose(heating.GetProperty("expectedWithGainsW").GetDouble(), heatingWithGains.HeatingLoadW);
        Assert.True(heatingWithGains.HeatingLoadW < heatingWithoutGains.HeatingLoadW);

        var cooling = fixture.GetProperty("coolingScenario");
        var coolingWithoutGains = SolveSingleNodeSteadyCase(
            h,
            cooling.GetProperty("initialAirTemperatureC").GetDouble(),
            cooling.GetProperty("outdoorTemperatureC").GetDouble(),
            0.0,
            cooling.GetProperty("heatingSetpointC").GetDouble(),
            cooling.GetProperty("coolingSetpointC").GetDouble(),
            3600.0);

        var coolingWithGains = SolveSingleNodeSteadyCase(
            h,
            cooling.GetProperty("initialAirTemperatureC").GetDouble(),
            cooling.GetProperty("outdoorTemperatureC").GetDouble(),
            gains,
            cooling.GetProperty("heatingSetpointC").GetDouble(),
            cooling.GetProperty("coolingSetpointC").GetDouble(),
            3600.0);

        AssertClose(cooling.GetProperty("expectedWithoutGainsW").GetDouble(), coolingWithoutGains.CoolingLoadW);
        AssertClose(cooling.GetProperty("expectedWithGainsW").GetDouble(), coolingWithGains.CoolingLoadW);
        Assert.True(coolingWithGains.CoolingLoadW > coolingWithoutGains.CoolingLoadW);
    }

    [Fact]
    public void MonthlyAggregation_SumsEnergyAndTracksPeaksAcrossMonths()
    {
        using var document = LoadFixture("engineering-iso52016-matrix-edge-005-monthly-aggregation.json");
        var fixture = document.RootElement;

        var result = _solver.Solve(CreateRequest(fixture));

        Assert.True(result.IsSuccess, result.Error);

        var profile = result.Value;

        Assert.Equal(3, profile.HourCount);
        AssertClose(fixture.GetProperty("expectedAnnualHeatingEnergyKWh").GetDouble(), profile.AnnualHeatingEnergyKWh);
        AssertClose(200.0, profile.PeakHeatingLoadW);

        var january = Assert.Single(profile.MonthlySummaries, month => month.Month == 1);
        var february = Assert.Single(profile.MonthlySummaries, month => month.Month == 2);

        AssertClose(fixture.GetProperty("expectedJanuaryHeatingEnergyKWh").GetDouble(), january.HeatingEnergyKWh);
        AssertClose(fixture.GetProperty("expectedFebruaryHeatingEnergyKWh").GetDouble(), february.HeatingEnergyKWh);
        AssertClose(fixture.GetProperty("expectedJanuaryPeakHeatingLoadW").GetDouble(), january.PeakHeatingLoadW);
        AssertClose(fixture.GetProperty("expectedFebruaryPeakHeatingLoadW").GetDouble(), february.PeakHeatingLoadW);
    }

    [Fact]
    public void EngineeringEdgeCaseDocumentation_StatesScopeAndNonClaims()
    {
        var repoRoot = FindRepositoryRoot();
        var docPath = Path.Combine(repoRoot, "docs", "calculations", "Iso52016MatrixEngineeringEdgeCases.md");

        Assert.True(File.Exists(docPath), $"Engineering edge-case documentation was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("Engineering edge-case hardening only.", doc);
        Assert.Contains("Validation anchors only, not full parity.", doc);
        Assert.Contains("No pyBuildingEnergy parity claim.", doc);
        Assert.Contains("No EnergyPlus parity claim.", doc);
        Assert.Contains("No ASHRAE 140 validation coverage claim.", doc);
        Assert.Contains("No full ISO 52016 parity claim.", doc);
        Assert.Contains("adjacent unconditioned boundary", doc, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EngineeringEdgeCaseVerificationScript_IsConnectedToMatrixAllVerification()
    {
        var repoRoot = FindRepositoryRoot();

        var edgeVerificationPath = Path.Combine(repoRoot, "scripts", "iso52016", "verify-iso52016-matrix-engineering-edge-cases.ps1");
        var allVerificationPath = Path.Combine(repoRoot, "scripts", "iso52016", "verify-iso52016-matrix-all.ps1");
        var releaseReadyPath = Path.Combine(repoRoot, "scripts", "iso52016", "assert-iso52016-matrix-release-ready.ps1");

        Assert.True(File.Exists(edgeVerificationPath), $"Engineering edge-case verification script was not found: {edgeVerificationPath}");
        Assert.True(File.Exists(allVerificationPath), $"Matrix all-verification script was not found: {allVerificationPath}");
        Assert.True(File.Exists(releaseReadyPath), $"Matrix release-ready script was not found: {releaseReadyPath}");

        var edgeVerification = File.ReadAllText(edgeVerificationPath);
        var allVerification = File.ReadAllText(allVerificationPath);
        var releaseReady = File.ReadAllText(releaseReadyPath);

        Assert.Contains("ISO52016-MATRIX-ENGINEERING-EDGE-CASES", edgeVerification);
        Assert.Contains("Iso52016MatrixEngineeringEdgeCase", edgeVerification);
        Assert.Contains("verify-iso52016-matrix-engineering-edge-cases.ps1", allVerification);
        Assert.Contains("verify-iso52016-matrix-engineering-edge-cases.ps1", releaseReady);
    }

    private Iso52016MatrixHourlyResult SolveSingleNodeSteadyCase(
        double heatTransferCoefficientWPerK,
        double initialAirTemperatureC,
        double boundaryTemperatureC,
        double internalHeatGainW,
        double heatingSetpointC,
        double coolingSetpointC,
        double timeStepSeconds)
    {
        var result = _solver.Solve(new Iso52016MatrixHourlySolverRequest(
            ZoneCode: "engineering-edge-single-node",
            Nodes:
            [
                new Iso52016MatrixNodeDefinition(
                    NodeId: "air",
                    HeatCapacityJPerK: 50000.0,
                    InitialTemperatureC: initialAirTemperatureC,
                    IsAirNode: true)
            ],
            InternalConductances: [],
            BoundaryConductances:
            [
                new Iso52016MatrixBoundaryConductance(
                    NodeId: "air",
                    BoundaryId: "boundary",
                    ConductanceWPerK: heatTransferCoefficientWPerK)
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
                        ["boundary"] = boundaryTemperatureC
                    },
                    NodeHeatGainsW: new Dictionary<string, double>
                    {
                        ["air"] = internalHeatGainW
                    },
                    HeatingSetpointC: heatingSetpointC,
                    CoolingSetpointC: coolingSetpointC)
            ],
            Options: new Iso52016MatrixHourlySolverOptions(
                TimeStepSeconds: timeStepSeconds,
                AirNodeId: "air",
                DefaultHeatingSetpointC: heatingSetpointC,
                DefaultCoolingSetpointC: coolingSetpointC)));

        Assert.True(result.IsSuccess, result.Error);

        return Assert.Single(result.Value.Hours);
    }

    private static Iso52016MatrixHourlySolverRequest CreateRequest(JsonElement fixture)
    {
        var nodes = fixture
            .GetProperty("nodes")
            .EnumerateArray()
            .Select(node => new Iso52016MatrixNodeDefinition(
                NodeId: node.GetProperty("nodeId").GetString()!,
                HeatCapacityJPerK: node.GetProperty("heatCapacityJPerK").GetDouble(),
                InitialTemperatureC: node.GetProperty("initialTemperatureC").GetDouble(),
                IsAirNode: node.GetProperty("isAirNode").GetBoolean()))
            .ToArray();

        var internalConductances = fixture
            .GetProperty("internalConductances")
            .EnumerateArray()
            .Select(link => new Iso52016MatrixConductanceLink(
                FromNodeId: link.GetProperty("fromNodeId").GetString()!,
                ToNodeId: link.GetProperty("toNodeId").GetString()!,
                ConductanceWPerK: link.GetProperty("conductanceWPerK").GetDouble()))
            .ToArray();

        var boundaryConductances = fixture
            .GetProperty("boundaryConductances")
            .EnumerateArray()
            .Select(link => new Iso52016MatrixBoundaryConductance(
                NodeId: link.GetProperty("nodeId").GetString()!,
                BoundaryId: link.GetProperty("boundaryId").GetString()!,
                ConductanceWPerK: link.GetProperty("conductanceWPerK").GetDouble()))
            .ToArray();

        var hours = fixture
            .GetProperty("hours")
            .EnumerateArray()
            .Select(hour => new Iso52016MatrixHourlyInputRecord(
                HourOfYear: hour.GetProperty("hourOfYear").GetInt32(),
                Month: hour.GetProperty("month").GetInt32(),
                Day: hour.GetProperty("day").GetInt32(),
                Hour: hour.GetProperty("hour").GetInt32(),
                BoundaryTemperaturesC: ReadDoubleMap(hour.GetProperty("boundaryTemperaturesC")),
                NodeHeatGainsW: ReadDoubleMap(hour.GetProperty("nodeHeatGainsW")),
                HeatingSetpointC: hour.GetProperty("heatingSetpointC").GetDouble(),
                CoolingSetpointC: hour.GetProperty("coolingSetpointC").GetDouble()))
            .ToArray();

        var firstHour = hours[0];

        return new Iso52016MatrixHourlySolverRequest(
            ZoneCode: fixture.GetProperty("id").GetString()!,
            Nodes: nodes,
            InternalConductances: internalConductances,
            BoundaryConductances: boundaryConductances,
            Hours: hours,
            Options: new Iso52016MatrixHourlySolverOptions(
                TimeStepSeconds: fixture.GetProperty("timeStepSeconds").GetDouble(),
                AirNodeId: fixture.GetProperty("airNodeId").GetString()!,
                DefaultHeatingSetpointC: firstHour.HeatingSetpointC ?? 20.0,
                DefaultCoolingSetpointC: firstHour.CoolingSetpointC ?? 26.0));
    }

    private static Dictionary<string, double> ReadDoubleMap(JsonElement element)
    {
        var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in element.EnumerateObject())
        {
            values[property.Name] = property.Value.GetDouble();
        }

        return values;
    }

    private static TwoNodeExpectation CalculateTwoNodeImplicitEulerExpectation(JsonElement fixture)
    {
        var timeStepSeconds = fixture.GetProperty("timeStepSeconds").GetDouble();
        var nodes = fixture.GetProperty("nodes").EnumerateArray().ToArray();
        var air = nodes.Single(node => node.GetProperty("nodeId").GetString() == "air");
        var mass = nodes.Single(node => node.GetProperty("nodeId").GetString() == "mass");

        var airCapacity = air.GetProperty("heatCapacityJPerK").GetDouble();
        var massCapacity = mass.GetProperty("heatCapacityJPerK").GetDouble();
        var initialAir = air.GetProperty("initialTemperatureC").GetDouble();
        var initialMass = mass.GetProperty("initialTemperatureC").GetDouble();

        var airMassConductance = fixture
            .GetProperty("internalConductances")
            .EnumerateArray()
            .Single()
            .GetProperty("conductanceWPerK")
            .GetDouble();

        var outdoorConductance = fixture
            .GetProperty("boundaryConductances")
            .EnumerateArray()
            .Single()
            .GetProperty("conductanceWPerK")
            .GetDouble();

        var hour = fixture.GetProperty("hours").EnumerateArray().Single();
        var outdoorTemperature = hour.GetProperty("boundaryTemperaturesC").GetProperty("outdoor").GetDouble();
        var airGain = hour.GetProperty("nodeHeatGainsW").GetProperty("air").GetDouble();
        var massGain = hour.GetProperty("nodeHeatGainsW").GetProperty("mass").GetDouble();

        var a00 = airCapacity / timeStepSeconds + outdoorConductance + airMassConductance;
        var a01 = -airMassConductance;
        var a10 = -airMassConductance;
        var a11 = massCapacity / timeStepSeconds + airMassConductance;

        var b0 = airCapacity / timeStepSeconds * initialAir + outdoorConductance * outdoorTemperature + airGain;
        var b1 = massCapacity / timeStepSeconds * initialMass + massGain;

        var determinant = a00 * a11 - a01 * a10;

        return new TwoNodeExpectation(
            AirTemperatureC: (b0 * a11 - a01 * b1) / determinant,
            MassTemperatureC: (a00 * b1 - b0 * a10) / determinant);
    }

    private static JsonDocument LoadFixture(string fileName)
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "AssistantEngineer.Tests",
            "Calculations",
            "Iso52016",
            "Matrix",
            "EngineeringEdgeCases",
            fileName);

        Assert.True(File.Exists(path), $"Engineering edge-case fixture was not found: {path}");

        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static void AssertClose(double expected, double actual) =>
        Assert.InRange(actual, expected - Tolerance, expected + Tolerance);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var src = Path.Combine(directory.FullName, "src", "Backend", "AssistantEngineer.Modules.Calculations");
            var tests = Path.Combine(directory.FullName, "tests", "AssistantEngineer.Tests");

            if (Directory.Exists(src) && Directory.Exists(tests))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate AssistantEngineer repository root from test base directory.");
    }

    private sealed record TwoNodeExpectation(
        double AirTemperatureC,
        double MassTemperatureC);
}