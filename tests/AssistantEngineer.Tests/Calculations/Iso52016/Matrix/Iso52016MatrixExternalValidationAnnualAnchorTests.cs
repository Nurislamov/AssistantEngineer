using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixExternalValidationAnnualAnchorTests
{
    private const double Tolerance = 0.000001;

    private readonly Iso52016MatrixHourlySolver _solver = new();

    [Fact]
    public void Annual8760Anchor_MatchesIndependentManualOneNodeReference()
    {
        var fixture = LoadAnnualFixture();

        Assert.Equal("ValidationAnchorOnly", fixture.Scope);
        Assert.Equal("IndependentManual", fixture.SourceStyle);
        Assert.Equal(8760, fixture.HourCount);

        var manual = CalculateManualExpectation(fixture);

        Assert.Equal(fixture.Expected.HourCount, manual.HourCount);
        AssertClose(fixture.Expected.AnnualHeatingEnergyKWh, manual.AnnualHeatingEnergyKWh);
        AssertClose(fixture.Expected.AnnualCoolingEnergyKWh, manual.AnnualCoolingEnergyKWh);
        AssertClose(fixture.Expected.AnnualTotalNodeHeatGainsKWh, manual.AnnualTotalNodeHeatGainsKWh);
        AssertClose(fixture.Expected.PeakHeatingLoadW, manual.PeakHeatingLoadW);
        AssertClose(fixture.Expected.PeakCoolingLoadW, manual.PeakCoolingLoadW);

        var result = _solver.Solve(CreateRequest(fixture));

        Assert.True(result.IsSuccess, result.Error);

        var profile = result.Value;

        Assert.Equal(fixture.AnchorId, profile.ZoneCode);
        Assert.Equal(8760, profile.HourCount);
        AssertClose(manual.AnnualHeatingEnergyKWh, profile.AnnualHeatingEnergyKWh);
        AssertClose(manual.AnnualCoolingEnergyKWh, profile.AnnualCoolingEnergyKWh);
        AssertClose(manual.AnnualTotalNodeHeatGainsKWh, profile.AnnualTotalNodeHeatGainsKWh);
        AssertClose(manual.PeakHeatingLoadW, profile.PeakHeatingLoadW);
        AssertClose(manual.PeakCoolingLoadW, profile.PeakCoolingLoadW);

        foreach (var expectedMonth in fixture.Expected.MonthlySummaries)
        {
            var manualMonth = manual.MonthlySummaries[expectedMonth.Month];
            var actualMonth = profile.MonthlySummaries.Single(summary => summary.Month == expectedMonth.Month);

            AssertClose(expectedMonth.HeatingEnergyKWh, manualMonth.HeatingEnergyKWh);
            AssertClose(expectedMonth.CoolingEnergyKWh, manualMonth.CoolingEnergyKWh);
            AssertClose(manualMonth.HeatingEnergyKWh, actualMonth.HeatingEnergyKWh);
            AssertClose(manualMonth.CoolingEnergyKWh, actualMonth.CoolingEnergyKWh);
        }
    }

    [Fact]
    public void Annual8760Anchor_DocsAndManifestKeepParityClaimsHonest()
    {
        var repoRoot = FindRepositoryRoot();

        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016MatrixExternalValidationAnnualAnchors.md");

        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixExternalValidationAnnualAnchorsManifest.json");

        var verifyScriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-external-validation-annual-anchors.ps1");

        Assert.True(File.Exists(docPath), $"Annual anchor doc was not found: {docPath}");
        Assert.True(File.Exists(manifestPath), $"Annual anchor manifest was not found: {manifestPath}");
        Assert.True(File.Exists(verifyScriptPath), $"Annual anchor verification script was not found: {verifyScriptPath}");

        var doc = File.ReadAllText(docPath);
        var verifyScript = File.ReadAllText(verifyScriptPath);

        Assert.Contains("8760", doc);
        Assert.Contains("Validation anchors only, not full equivalence claim.", doc);
        Assert.Contains("No exact StandardReference numerical equivalence claim.", doc);
        Assert.Contains("No exact EnergyPlus numerical equivalence claim.", doc);
        Assert.Contains("verify-iso52016-matrix-external-validation-annual-anchors.ps1", verifyScript);

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("ISO52016-MATRIX-EXTERNAL-VALIDATION-ANNUAL-ANCHORS", root.GetProperty("stageId").GetString());
        Assert.Equal("ValidationAnchorOnly", root.GetProperty("scope").GetString());
        Assert.True(root.GetProperty("annual8760ManualReferenceIntegrated").GetBoolean());
        Assert.Equal(8760, root.GetProperty("annualHourCount").GetInt32());

        var nonClaims = root
            .GetProperty("explicitNonClaims")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("No exact StandardReference numerical equivalence claim.", nonClaims);
        Assert.Contains("No exact EnergyPlus numerical equivalence claim.", nonClaims);
        Assert.Contains("Validation anchors only, not full equivalence claim.", nonClaims);
    }

    [Fact]
    public void Annual8760Anchor_IsChainedThroughExternalAnchorsVerification()
    {
        var repoRoot = FindRepositoryRoot();

        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-external-validation-anchors.ps1");

        Assert.True(File.Exists(scriptPath), $"External validation anchors script was not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("verify-iso52016-matrix-external-validation-annual-anchors.ps1", script);
    }

    private static Iso52016MatrixHourlySolverRequest CreateRequest(
        AnnualAnchorFixture fixture) =>
        new(
            ZoneCode: fixture.AnchorId,
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
            Hours: BuildHours(fixture),
            Options: new Iso52016MatrixHourlySolverOptions(
                TimeStepSeconds: fixture.TimeStepSeconds,
                AirNodeId: "air",
                DefaultHeatingSetpointC: fixture.HeatingSetpointC,
                DefaultCoolingSetpointC: fixture.CoolingSetpointC));

    private static IReadOnlyList<Iso52016MatrixHourlyInputRecord> BuildHours(
        AnnualAnchorFixture fixture)
    {
        var hours = new List<Iso52016MatrixHourlyInputRecord>(fixture.HourCount);

        for (var hourOfYear = 0; hourOfYear < fixture.HourCount; hourOfYear++)
        {
            var segment = FindSegment(fixture, hourOfYear);
            var calendar = ToCalendar(hourOfYear);

            hours.Add(
                new Iso52016MatrixHourlyInputRecord(
                    HourOfYear: hourOfYear,
                    Month: calendar.Month,
                    Day: calendar.Day,
                    Hour: calendar.Hour,
                    BoundaryTemperaturesC: new Dictionary<string, double>
                    {
                        ["outdoor"] = segment.OutdoorTemperatureC
                    },
                    NodeHeatGainsW: new Dictionary<string, double>
                    {
                        ["air"] = segment.InternalHeatGainW ?? fixture.InternalHeatGainW
                    },
                    HeatingSetpointC: fixture.HeatingSetpointC,
                    CoolingSetpointC: fixture.CoolingSetpointC));
        }

        return hours;
    }

    private static AnnualManualExpectation CalculateManualExpectation(
        AnnualAnchorFixture fixture)
    {
        var capacityRateWPerK = fixture.HeatCapacityJPerK / fixture.TimeStepSeconds;
        var denominatorWPerK = capacityRateWPerK + fixture.HeatTransferCoefficientWPerK;
        var previousTemperatureC = fixture.InitialAirTemperatureC;
        var monthly = Enumerable
            .Range(1, 12)
            .ToDictionary(
                month => month,
                _ => new MonthAccumulator());

        var annualHeatingEnergyKWh = 0.0;
        var annualCoolingEnergyKWh = 0.0;
        var annualTotalGainsKWh = 0.0;
        var peakHeatingLoadW = 0.0;
        var peakCoolingLoadW = 0.0;

        for (var hourOfYear = 0; hourOfYear < fixture.HourCount; hourOfYear++)
        {
            var segment = FindSegment(fixture, hourOfYear);
            var calendar = ToCalendar(hourOfYear);
            var gainW = segment.InternalHeatGainW ?? fixture.InternalHeatGainW;

            var freeFloatingTemperatureC =
                (capacityRateWPerK * previousTemperatureC +
                 fixture.HeatTransferCoefficientWPerK * segment.OutdoorTemperatureC +
                 gainW) /
                denominatorWPerK;

            var heatingLoadW = 0.0;
            var coolingLoadW = 0.0;
            var controlledTemperatureC = freeFloatingTemperatureC;

            if (freeFloatingTemperatureC < fixture.HeatingSetpointC)
            {
                heatingLoadW =
                    fixture.HeatingSetpointC * denominatorWPerK -
                    capacityRateWPerK * previousTemperatureC -
                    fixture.HeatTransferCoefficientWPerK * segment.OutdoorTemperatureC -
                    gainW;

                controlledTemperatureC = fixture.HeatingSetpointC;
            }
            else if (freeFloatingTemperatureC > fixture.CoolingSetpointC)
            {
                coolingLoadW =
                    capacityRateWPerK * previousTemperatureC +
                    fixture.HeatTransferCoefficientWPerK * segment.OutdoorTemperatureC +
                    gainW -
                    fixture.CoolingSetpointC * denominatorWPerK;

                controlledTemperatureC = fixture.CoolingSetpointC;
            }

            var heatingEnergyKWh = heatingLoadW * fixture.TimeStepSeconds / 3_600_000.0;
            var coolingEnergyKWh = coolingLoadW * fixture.TimeStepSeconds / 3_600_000.0;
            var gainEnergyKWh = gainW * fixture.TimeStepSeconds / 3_600_000.0;

            annualHeatingEnergyKWh += heatingEnergyKWh;
            annualCoolingEnergyKWh += coolingEnergyKWh;
            annualTotalGainsKWh += gainEnergyKWh;
            peakHeatingLoadW = Math.Max(peakHeatingLoadW, heatingLoadW);
            peakCoolingLoadW = Math.Max(peakCoolingLoadW, coolingLoadW);

            monthly[calendar.Month].HeatingEnergyKWh += heatingEnergyKWh;
            monthly[calendar.Month].CoolingEnergyKWh += coolingEnergyKWh;

            previousTemperatureC = controlledTemperatureC;
        }

        return new AnnualManualExpectation(
            HourCount: fixture.HourCount,
            AnnualHeatingEnergyKWh: annualHeatingEnergyKWh,
            AnnualCoolingEnergyKWh: annualCoolingEnergyKWh,
            AnnualTotalNodeHeatGainsKWh: annualTotalGainsKWh,
            PeakHeatingLoadW: peakHeatingLoadW,
            PeakCoolingLoadW: peakCoolingLoadW,
            MonthlySummaries: monthly.ToDictionary(
                item => item.Key,
                item => new ManualMonthExpectation(
                    item.Value.HeatingEnergyKWh,
                    item.Value.CoolingEnergyKWh)));
    }

    private static AnnualAnchorSegment FindSegment(
        AnnualAnchorFixture fixture,
        int hourOfYear)
    {
        var segment = fixture.AnnualPattern.SingleOrDefault(
            candidate =>
                candidate.StartHourInclusive <= hourOfYear &&
                hourOfYear < candidate.EndHourExclusive);

        Assert.NotNull(segment);
        return segment;
    }

    private static CalendarHour ToCalendar(
        int hourOfYear)
    {
        var monthLengths = new[]
        {
            31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };

        var dayOfYear = hourOfYear / 24;
        var hour = hourOfYear % 24;
        var remainingDays = dayOfYear;

        for (var monthIndex = 0; monthIndex < monthLengths.Length; monthIndex++)
        {
            if (remainingDays < monthLengths[monthIndex])
            {
                return new CalendarHour(
                    Month: monthIndex + 1,
                    Day: remainingDays + 1,
                    Hour: hour);
            }

            remainingDays -= monthLengths[monthIndex];
        }

        throw new ArgumentOutOfRangeException(
            nameof(hourOfYear),
            hourOfYear,
            "Hour of year must fit a non-leap 8760-hour calendar.");
    }

    private static AnnualAnchorFixture LoadAnnualFixture()
    {
        var fixturePath = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "AssistantEngineer.Tests",
            "Calculations",
            "Iso52016",
            "Matrix",
            "ExternalValidationAnnualAnchors",
            "manual-independent-annual-8760-seasonal-loads.json");

        Assert.True(File.Exists(fixturePath), $"Annual anchor fixture was not found: {fixturePath}");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var fixture = JsonSerializer.Deserialize<AnnualAnchorFixture>(
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

    private sealed record AnnualAnchorFixture(
        string AnchorId,
        string SourceStyle,
        string Scope,
        string Description,
        int HourCount,
        double TimeStepSeconds,
        double HeatCapacityJPerK,
        double InitialAirTemperatureC,
        double HeatTransferCoefficientWPerK,
        double InternalHeatGainW,
        double HeatingSetpointC,
        double CoolingSetpointC,
        IReadOnlyList<AnnualAnchorSegment> AnnualPattern,
        AnnualAnchorExpected Expected,
        IReadOnlyList<string> ExplicitNonClaims);

    private sealed record AnnualAnchorSegment(
        string Name,
        int StartHourInclusive,
        int EndHourExclusive,
        double OutdoorTemperatureC,
        double? InternalHeatGainW = null);

    private sealed record AnnualAnchorExpected(
        int HourCount,
        double AnnualHeatingEnergyKWh,
        double AnnualCoolingEnergyKWh,
        double AnnualTotalNodeHeatGainsKWh,
        double PeakHeatingLoadW,
        double PeakCoolingLoadW,
        IReadOnlyList<AnnualMonthExpected> MonthlySummaries);

    private sealed record AnnualMonthExpected(
        int Month,
        double HeatingEnergyKWh,
        double CoolingEnergyKWh);

    private sealed record AnnualManualExpectation(
        int HourCount,
        double AnnualHeatingEnergyKWh,
        double AnnualCoolingEnergyKWh,
        double AnnualTotalNodeHeatGainsKWh,
        double PeakHeatingLoadW,
        double PeakCoolingLoadW,
        IReadOnlyDictionary<int, ManualMonthExpectation> MonthlySummaries);

    private sealed record ManualMonthExpectation(
        double HeatingEnergyKWh,
        double CoolingEnergyKWh);

    private sealed record CalendarHour(
        int Month,
        int Day,
        int Hour);

    private sealed class MonthAccumulator
    {
        public double HeatingEnergyKWh { get; set; }

        public double CoolingEnergyKWh { get; set; }
    }
}
