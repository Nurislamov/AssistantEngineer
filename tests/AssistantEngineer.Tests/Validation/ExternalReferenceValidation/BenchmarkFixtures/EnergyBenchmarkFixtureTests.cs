using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;
using AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;
using AssistantEngineer.Modules.Calculations.Application.Services.SolarGains;

namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation.BenchmarkFixtures;

public class EnergyBenchmarkFixtureTests
{
    [Fact]
    public void BenchmarkFixtureLoader_LoadsActiveFixtures()
    {
        var result = EnergyBenchmarkFixtureLoader.LoadFromDefaultDirectory();
        var fixtureNames = result.Fixtures
            .Select(fixture => fixture.FixtureName)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("annual-constant-heating-8760", fixtureNames);
        Assert.Contains("annual-constant-cooling-8760", fixtureNames);
        Assert.Contains("signed-component-balance-winter", fixtureNames);
        Assert.Contains("signed-component-balance-summer", fixtureNames);
        Assert.Contains("signed-component-balance-with-infiltration-winter", fixtureNames);
        Assert.Contains("signed-component-balance-with-ventilation-split-winter", fixtureNames);
        Assert.Contains("solar-night-zero", fixtureNames);
        Assert.Contains("window-solar-gain-basic", fixtureNames);
        Assert.Contains("window-solar-gain-with-shading", fixtureNames);
        Assert.Contains("surface-irradiance-night-zero", fixtureNames);
        Assert.All(result.Fixtures, fixture =>
        {
            Assert.Equal("Active", fixture.Status);
            Assert.NotEmpty(fixture.FixtureName);
            Assert.NotEmpty(fixture.Description);
            Assert.NotEmpty(fixture.Category);
            Assert.NotEmpty(fixture.ReferenceType);
            Assert.NotEmpty(fixture.Method);
            Assert.NotNull(fixture.Input);
            Assert.NotNull(fixture.Tolerances);
            Assert.NotEmpty(fixture.Assumptions);
            Assert.NotEmpty(fixture.Notes);
        });
    }

    [Fact]
    public void BenchmarkFixtureLoader_SkipsPendingAndDisabled()
    {
        var directory = CreateTempDirectory();

        try
        {
            WriteFixture(directory, "active.json", CreateFixtureJson("active-fixture", "Active"));
            WriteFixture(directory, "pending.json", CreateFixtureJson("pending-fixture", "Pending"));
            WriteFixture(directory, "disabled.json", CreateFixtureJson("disabled-fixture", "Disabled"));

            var result = EnergyBenchmarkFixtureLoader.LoadFromDirectory(directory);

            var active = Assert.Single(result.Fixtures);
            Assert.Equal("active-fixture", active.FixtureName);
            Assert.Equal(2, result.SkippedFixtures.Count);
            Assert.Contains(result.SkippedFixtures, skipped =>
                skipped.FixtureName == "pending-fixture" &&
                skipped.Status == "Pending" &&
                skipped.Reason.Contains("skipped by default", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.SkippedFixtures, skipped =>
                skipped.FixtureName == "disabled-fixture" &&
                skipped.Status == "Disabled" &&
                skipped.Reason.Contains("skipped by default", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void BenchmarkFixtureLoader_InvalidFixtureFailsWithHelpfulMessage()
    {
        var directory = CreateTempDirectory();

        try
        {
            WriteFixture(directory, "invalid.json", "{ invalid json");

            var exception = Assert.Throws<InvalidOperationException>(() =>
                EnergyBenchmarkFixtureLoader.LoadFromDirectory(directory));

            Assert.Contains("JSON is invalid", exception.Message);
            Assert.Contains("invalid.json", exception.Message);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void BenchmarkComparison_PassesWithinAbsoluteTolerance()
    {
        var result = EnergyBenchmarkComparison.CompareNumeric(
            "fixture",
            "annualHeatingDemandKWh",
            expected: 0,
            actual: 0.005,
            new EnergyBenchmarkTolerance { Absolute = 0.01 });

        Assert.True(result.Passed, result.Message);
        Assert.Equal(0.005, result.AbsoluteDifference, precision: 6);
    }

    [Fact]
    public void BenchmarkComparison_PassesWithinRelativeTolerance()
    {
        var result = EnergyBenchmarkComparison.CompareNumeric(
            "fixture",
            "annualHeatingDemandKWh",
            expected: 100,
            actual: 100.05,
            new EnergyBenchmarkTolerance
            {
                Absolute = 0.001,
                RelativePercent = 0.1
            });

        Assert.True(result.Passed, result.Message);
        Assert.True(result.AbsoluteDifference > result.AbsoluteTolerance);
        Assert.True(result.RelativeDifferencePercent <= result.RelativeTolerancePercent);
    }

    [Fact]
    public void BenchmarkComparison_FailsOutsideTolerance()
    {
        var result = EnergyBenchmarkComparison.CompareNumeric(
            "fixture",
            "annualHeatingDemandKWh",
            expected: 100,
            actual: 102,
            new EnergyBenchmarkTolerance
            {
                Absolute = 0.1,
                RelativePercent = 1
            });

        Assert.False(result.Passed);
        Assert.Contains("fixture", result.Message);
        Assert.Contains("annualHeatingDemandKWh", result.Message);
    }

    [Fact]
    public void AnnualEnergyBenchmarkFixtures_AllActiveFixturesPass()
    {
        var loadResult = EnergyBenchmarkFixtureLoader.LoadFromDefaultDirectory();
        var engine = new AnnualEnergyBalanceEngine();
        var failures = new List<string>();

        foreach (var fixture in loadResult.Fixtures)
        {
            var actualResult = CreateActualBenchmarkResult(fixture, engine, failures);

            if (actualResult is null)
                continue;

            failures.AddRange(EnergyBenchmarkComparison
                .CompareExpectedNumericFields(fixture, actualResult)
                .Where(comparison => !comparison.Passed)
                .Select(comparison => WithFixtureContext(fixture, comparison.Message)));

            foreach (var expectedBoolean in EnergyBenchmarkComparison.GetExpectedBooleanValues(fixture))
            {
                if (!EnergyBenchmarkComparison.TryGetBooleanValue(
                        actualResult,
                        expectedBoolean.Key,
                        out var actualValue,
                        out var failure))
                {
                    failures.Add(WithFixtureContext(
                        fixture,
                        $"Fixture '{fixture.FixtureName}' field '{expectedBoolean.Key}' could not be read from actual result: {failure}"));
                    continue;
                }

                if (actualValue != expectedBoolean.Value)
                {
                    failures.Add(WithFixtureContext(
                        fixture,
                        $"Fixture '{fixture.FixtureName}' field '{expectedBoolean.Key}' failed: expected {expectedBoolean.Value}, actual {actualValue}."));
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static object? CreateActualBenchmarkResult(
        EnergyBenchmarkFixture fixture,
        AnnualEnergyBalanceEngine annualEnergyEngine,
        ICollection<string> failures) =>
        fixture.Category switch
        {
            "AnnualEnergyBalance" or "SignedComponentBalance" =>
                CreateAnnualEnergyBenchmarkResult(
                    fixture,
                    annualEnergyEngine,
                    failures),
            "SolarGains" or "WindowSolarGains" =>
                CreateWindowSolarGainBenchmarkResult(
                    fixture,
                    failures),
            "SurfaceIrradiance" =>
                CreateSurfaceIrradianceBenchmarkResult(
                    fixture,
                    failures),
            _ => AddUnsupportedFixtureFailure(
                fixture,
                failures)
        };

    private static object? CreateAnnualEnergyBenchmarkResult(
        EnergyBenchmarkFixture fixture,
        AnnualEnergyBalanceEngine engine,
        ICollection<string> failures)
    {
        var input = CreateAnnualEnergyInput(fixture);
        var result = engine.Calculate(input);

        if (result.IsSuccess)
            return result.Value;

        failures.Add(WithFixtureContext(fixture, result.Error));
        return null;
    }

    private static object? CreateWindowSolarGainBenchmarkResult(
        EnergyBenchmarkFixture fixture,
        ICollection<string> failures)
    {
        var input = fixture.Input.WindowSolarGain;
        if (input is null)
        {
            failures.Add(WithFixtureContext(
                fixture,
                $"Fixture '{fixture.FixtureName}' is missing input.windowSolarGain."));
            return null;
        }

        var result = new WindowSolarGainEngine().Calculate(
            new WindowSolarGainInput(
                WindowId: input.WindowId,
                RoomId: input.RoomId,
                AreaM2: input.AreaM2,
                OrientationAzimuthDeg: input.OrientationAzimuthDeg,
                TiltDeg: input.TiltDeg,
                Shgc: input.Shgc,
                FrameFactor: input.FrameFactor,
                InternalShadingFactor: input.InternalShadingFactor,
                ExternalShadingFactor: input.ExternalShadingFactor,
                FixedShadingFactor: input.FixedShadingFactor,
                IncidentIrradianceWPerM2: input.IncidentIrradianceWPerM2,
                DirectIrradianceWPerM2: input.DirectIrradianceWPerM2,
                DiffuseIrradianceWPerM2: input.DiffuseIrradianceWPerM2,
                GroundReflectedIrradianceWPerM2: input.GroundReflectedIrradianceWPerM2,
                IsNight: input.IsNight,
                DiagnosticsContext: fixture.FixtureName));

        if (result.IsSuccess)
            return result.Value;

        failures.Add(WithFixtureContext(fixture, result.Error));
        return null;
    }

    private static object? CreateSurfaceIrradianceBenchmarkResult(
        EnergyBenchmarkFixture fixture,
        ICollection<string> failures)
    {
        var input = fixture.Input.SurfaceIrradiance;
        if (input is null)
        {
            failures.Add(WithFixtureContext(
                fixture,
                $"Fixture '{fixture.FixtureName}' is missing input.surfaceIrradiance."));
            return null;
        }

        var result = new IsotropicSkySurfaceIrradianceCalculator().Calculate(
            new SurfaceIrradianceRequest(
                SolarPosition: new SolarPositionResult(
                    DayOfYear: 1,
                    SolarDeclinationDegrees: 0,
                    EquationOfTimeMinutes: 0,
                    HourAngleDegrees: 0,
                    SolarAltitudeDegrees: input.SolarAltitudeDeg,
                    SolarAzimuthDegrees: input.SolarAzimuthDeg,
                    ZenithAngleDegrees: 90 - input.SolarAltitudeDeg,
                    RelativeAirMass: input.SolarAltitudeDeg > 0 ? 1 : 0),
                Surface: new SurfaceOrientation(
                    TiltDegrees: input.SurfaceTiltDeg,
                    AzimuthDegrees: input.SurfaceAzimuthDeg),
                DirectNormalIrradianceWm2: input.DirectNormalIrradianceWPerM2,
                DiffuseHorizontalIrradianceWm2: input.DiffuseHorizontalIrradianceWPerM2,
                GlobalHorizontalIrradianceWm2: input.GlobalHorizontalIrradianceWPerM2,
                GroundReflectance: input.GroundReflectance,
                DiagnosticsContext: fixture.FixtureName));

        return new SurfaceIrradianceBenchmarkResult(
            DirectOnSurfaceWPerM2: result.BeamIrradianceWm2,
            DiffuseOnSurfaceWPerM2: result.DiffuseSkyIrradianceWm2,
            GroundReflectedWPerM2: result.GroundReflectedIrradianceWm2,
            TotalIncidentIrradianceWPerM2: result.TotalIrradianceWm2,
            SolarAltitudeDeg: input.SolarAltitudeDeg,
            SolarAzimuthDeg: input.SolarAzimuthDeg,
            IncidenceAngleDeg: result.IncidenceAngleDegrees);
    }

    private static object? AddUnsupportedFixtureFailure(
        EnergyBenchmarkFixture fixture,
        ICollection<string> failures)
    {
        failures.Add($"Fixture '{fixture.FixtureName}' has unsupported benchmark category '{fixture.Category}'.");
        return null;
    }

    private static AnnualEnergyBalanceInput CreateAnnualEnergyInput(EnergyBenchmarkFixture fixture)
    {
        var input = fixture.Input;

        if (!string.Equals(input.HourlyPattern, "constant", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Fixture '{fixture.FixtureName}' uses unsupported hourlyPattern '{input.HourlyPattern}'.");
        }

        var values = input.HourlyValues;
        var hours = Enumerable
            .Range(0, input.HourlyRecordCount)
            .Select(hourIndex => new AnnualEnergyBalanceHourInput(
                HourIndex: hourIndex,
                Month: ResolveMonth(hourIndex),
                HeatingLoadW: values.HeatingLoadW,
                CoolingLoadW: values.CoolingLoadW,
                TransmissionW: values.TransmissionW,
                VentilationW: values.VentilationW,
                InfiltrationW: values.InfiltrationW,
                SolarGainsW: values.SolarGainsW,
                InternalGainsW: values.InternalGainsW,
                GroundW: values.GroundW,
                HourDurationH: values.HourDurationH,
                TransmissionBalanceW: values.TransmissionBalanceW,
                VentilationBalanceW: values.VentilationBalanceW,
                InfiltrationBalanceW: values.InfiltrationBalanceW,
                GroundBalanceW: values.GroundBalanceW,
                MechanicalVentilationW: values.MechanicalVentilationW,
                NaturalVentilationW: values.NaturalVentilationW,
                MechanicalVentilationBalanceW: values.MechanicalVentilationBalanceW,
                NaturalVentilationBalanceW: values.NaturalVentilationBalanceW))
            .ToArray();

        return new AnnualEnergyBalanceInput(
            input.BuildingId,
            input.BuildingName,
            input.BuildingAreaM2,
            input.Year,
            hours,
            DiagnosticsContext: fixture.FixtureName,
            EnergyDataSource: input.EnergyDataSource,
            IsTrueHourly8760: input.IsTrueHourly8760,
            ActualMethod: fixture.Method);
    }

    private static int ResolveMonth(int hourIndex)
    {
        var monthHours = new[]
        {
            744,
            672,
            744,
            720,
            744,
            720,
            744,
            744,
            720,
            744,
            720,
            744
        };

        var remaining = hourIndex;
        for (var month = 0; month < monthHours.Length; month++)
        {
            if (remaining < monthHours[month])
                return month + 1;

            remaining -= monthHours[month];
        }

        return 12;
    }

    private static string WithFixtureContext(EnergyBenchmarkFixture fixture, string message) =>
        $"{message} Assumptions: {string.Join(" | ", fixture.Assumptions)}. Notes: {string.Join(" | ", fixture.Notes)}.";

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "assistant-engineer-benchmark-fixtures",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteFixture(string directory, string fileName, string content) =>
        File.WriteAllText(Path.Combine(directory, fileName), content);

    private static string CreateFixtureJson(string fixtureName, string status) =>
        $$"""
        {
          "fixtureName": "{{fixtureName}}",
          "description": "Temporary loader fixture.",
          "category": "AnnualEnergyBalance",
          "referenceType": "BenchmarkReference",
          "method": "Energy Calculation equivalence / Annual 8760 Energy Balance",
          "status": "{{status}}",
          "input": {
            "calculationBasis": "annual-energy-balance",
            "buildingAreaM2": 100,
            "hourlyPattern": "constant",
            "hourlyRecordCount": 1,
            "hourlyValues": {
              "heatingLoadW": 1000
            }
          },
          "expected": {
            "annualHeatingDemandKWh": 1
          },
          "tolerances": {
            "defaultAbsolute": 0.000001,
            "defaultRelativePercent": 0.000001
          },
          "assumptions": [
            "Temporary fixture for loader tests."
          ],
          "notes": [
            "No external equivalence claim."
          ]
        }
        """;

    private sealed record SurfaceIrradianceBenchmarkResult(
        double DirectOnSurfaceWPerM2,
        double DiffuseOnSurfaceWPerM2,
        double GroundReflectedWPerM2,
        double TotalIncidentIrradianceWPerM2,
        double SolarAltitudeDeg,
        double SolarAzimuthDeg,
        double IncidenceAngleDeg);
}
