using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Transmission;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.SolarGains;
using AssistantEngineer.Modules.Calculations.Application.Services.Transmission;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.Tests.Calculations.Iso52016;

namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation;

public class EnergyCalculationDeterministicFixtureTests
{
    [Theory]
    [InlineData("single-zone-no-solar.json", "single-zone-no-solar")]
    [InlineData("single-zone-solar-south-window.json", "single-zone-solar-south-window")]
    [InlineData("single-zone-annual-8760.json", "single-zone-annual-8760")]
    public void FixtureLoaderLoadsDeterministicReferenceFixture(
        string fileName,
        string fixtureName)
    {
        var fixture = ExternalReferenceValidationFixtureLoader.Load(fileName);

        Assert.Equal(fixtureName, fixture.FixtureName);
        Assert.NotEmpty(fixture.Description);
        Assert.Contains("deterministic reference fixture", fixture.SourceReference, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(fixture.Assumptions);
        Assert.NotNull(fixture.Input);
        Assert.NotNull(fixture.ExpectedHourlyResults);
        Assert.NotEmpty(fixture.ExpectedMonthlyResults);
        Assert.NotNull(fixture.ExpectedAnnualResults);
    }

    [Theory]
    [InlineData("transmission-single-external-wall-winter.json")]
    [InlineData("transmission-single-window-winter.json")]
    [InlineData("transmission-adiabatic-internal-wall.json")]
    [InlineData("transmission-adjacent-conditioned-same-temperature.json")]
    [InlineData("transmission-outdoor-cooling-gain.json")]
    public void TransmissionFixturesVerifyEnvelopeHeatTransfer(
        string fileName)
    {
        var fixture = ExternalReferenceValidationFixtureLoader.Load(fileName);
        var transmission = AssertRequired(fixture.Input.Transmission);
        var engine = new TransmissionHeatTransferEngine();

        Assert.Equal("InternalDeterministic", fixture.ReferenceType);
        Assert.Equal("Energy Calculation equivalence / Transmission Heat Transfer", fixture.Method);
        Assert.Equal("transmission-heat-transfer", fixture.Input.CalculationBasis);

        var request = new TransmissionHeatTransferRequest(
            transmission.Elements
                .Select(CreateTransmissionElementInput)
                .ToArray());

        var result = engine.Calculate(request);

        Assert.True(result.IsSuccess, result.Error);
        AssertClose(fixture.Expected.TotalHeatFlowW, result.Value.TotalHeatFlowW, fixture.Tolerances.HourlyLoadW);
        AssertClose(fixture.Expected.TotalHeatLossW, result.Value.TotalHeatLossW, fixture.Tolerances.HourlyLoadW);
        AssertClose(fixture.Expected.TotalHeatGainW, result.Value.TotalHeatGainW, fixture.Tolerances.HourlyLoadW);
        Assert.Equal(fixture.Expected.Elements.Count, result.Value.Elements.Count);

        foreach (var expected in fixture.Expected.Elements)
        {
            var actual = result.Value.Elements.Single(element => element.ElementId == expected.ElementId);

            Assert.Equal(Enum.Parse<TransmissionElementType>(expected.ElementType), actual.ElementType);
            Assert.Equal(Enum.Parse<TransmissionBoundaryType>(expected.BoundaryType), actual.BoundaryType);
            AssertClose(expected.AreaM2, actual.AreaM2, fixture.Tolerances.HeatTransferCoefficientWPerK);
            AssertClose(expected.UValueWPerM2K, actual.UValueWPerM2K, fixture.Tolerances.HeatTransferCoefficientWPerK);
            AssertClose(expected.DeltaTC, actual.DeltaTC, fixture.Tolerances.HourlyTemperatureC);
            AssertClose(expected.HeatFlowW, actual.HeatFlowW, fixture.Tolerances.HourlyLoadW);
            Assert.Equal(expected.IsIncludedInLoad, actual.IsIncludedInLoad);

            foreach (var diagnosticCode in expected.ExpectedDiagnosticCodes)
                Assert.Contains(actual.Diagnostics, diagnostic => diagnostic.Code == diagnosticCode);
        }
    }

    [Theory]
    [InlineData("window-solar-single-window-no-shading.json")]
    [InlineData("window-solar-single-window-with-shading.json")]
    [InlineData("window-solar-night-is-zero.json")]
    [InlineData("window-solar-invalid-shgc-diagnostics.json")]
    [InlineData("window-solar-room-aggregation.json")]
    public void WindowSolarGainFixturesVerifySolarGains(
        string fileName)
    {
        var fixture = ExternalReferenceValidationFixtureLoader.Load(fileName);
        var input = AssertRequired(fixture.Input.WindowSolarGains);
        var engine = new WindowSolarGainEngine();

        Assert.Equal("InternalDeterministic", fixture.ReferenceType);
        Assert.Equal("Energy Calculation equivalence / Window Solar Gains", fixture.Method);
        Assert.Equal("window-solar-gains", fixture.Input.CalculationBasis);

        var result = engine.CalculateRoom(
            new RoomWindowSolarGainRequest(
                input.RoomId,
                input.Windows
                    .Select(CreateWindowSolarGainInput)
                    .ToArray()));

        Assert.True(result.IsSuccess, result.Error);
        AssertClose(fixture.Expected.TotalRoomSolarGainW, result.Value.TotalSolarGainW, fixture.Tolerances.HourlyLoadW);
        Assert.Equal(fixture.Expected.WindowSolarGains.Count, result.Value.WindowBreakdown.Count);

        foreach (var expected in fixture.Expected.WindowSolarGains)
        {
            var actual = result.Value.WindowBreakdown.Single(window => window.WindowId == expected.WindowId);

            AssertClose(expected.EffectiveSolarFactor, actual.EffectiveSolarFactor, fixture.Tolerances.HeatTransferCoefficientWPerK);
            AssertClose(expected.SolarGainW, actual.SolarGainW, fixture.Tolerances.HourlyLoadW);
            Assert.Equal(expected.IsIncludedInLoad, actual.IsIncludedInLoad);

            foreach (var diagnosticCode in expected.ExpectedDiagnosticCodes)
                Assert.Contains(actual.Diagnostics, diagnostic => diagnostic.Code == diagnosticCode);
        }
    }

    [Theory]
    [InlineData("ventilation-mechanical-heating-load.json")]
    [InlineData("ventilation-mechanical-cooling-load.json")]
    [InlineData("ventilation-with-heat-recovery.json")]
    [InlineData("ventilation-infiltration-by-ach.json")]
    [InlineData("ventilation-zero-airflow.json")]
    [InlineData("ventilation-invalid-heat-recovery-efficiency.json")]
    public void VentilationInfiltrationFixturesVerifyOutdoorAirLoads(
        string fileName)
    {
        var fixture = ExternalReferenceValidationFixtureLoader.Load(fileName);
        var input = AssertRequired(fixture.Input.VentilationInfiltration);
        var expected = AssertRequired(fixture.Expected.VentilationInfiltration);
        var engine = new VentilationAndInfiltrationLoadEngine();

        Assert.Equal("InternalDeterministic", fixture.ReferenceType);
        Assert.Equal("Energy Calculation equivalence / Ventilation and Infiltration Loads", fixture.Method);
        Assert.Equal("ventilation-infiltration-loads", fixture.Input.CalculationBasis);

        var result = engine.Calculate(CreateVentilationInfiltrationInput(input));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(expected.HasErrors, result.Value.HasErrors);
        AssertClose(expected.DeltaTC, result.Value.DeltaTC, fixture.Tolerances.HourlyTemperatureC);
        AssertClose(expected.MechanicalAirflowM3PerHour, result.Value.MechanicalVentilation.AirflowM3PerHour, fixture.Tolerances.HeatTransferCoefficientWPerK);
        AssertClose(expected.MechanicalAirflowM3PerSecond, result.Value.MechanicalVentilation.AirflowM3PerSecond, fixture.Tolerances.HeatTransferCoefficientWPerK);
        AssertClose(expected.RawMechanicalHeatingLoadW, result.Value.MechanicalVentilation.RawHeatingLoadW, fixture.Tolerances.HourlyLoadW);
        AssertClose(expected.RawMechanicalCoolingLoadW, result.Value.MechanicalVentilation.RawCoolingLoadW, fixture.Tolerances.HourlyLoadW);
        AssertClose(expected.EffectiveMechanicalHeatingLoadW, result.Value.MechanicalVentilation.EffectiveHeatingLoadW, fixture.Tolerances.HourlyLoadW);
        AssertClose(expected.EffectiveMechanicalCoolingLoadW, result.Value.MechanicalVentilation.EffectiveCoolingLoadW, fixture.Tolerances.HourlyLoadW);
        AssertClose(expected.InfiltrationAirChangesPerHour, result.Value.Infiltration.InfiltrationAirChangesPerHour, fixture.Tolerances.HeatTransferCoefficientWPerK);
        AssertClose(expected.InfiltrationAirflowM3PerHour, result.Value.Infiltration.InfiltrationAirflowM3PerHour, fixture.Tolerances.HeatTransferCoefficientWPerK);
        AssertClose(expected.InfiltrationAirflowM3PerSecond, result.Value.Infiltration.InfiltrationAirflowM3PerSecond, fixture.Tolerances.HeatTransferCoefficientWPerK);
        AssertClose(expected.InfiltrationHeatingLoadW, result.Value.Infiltration.HeatingLoadW, fixture.Tolerances.HourlyLoadW);
        AssertClose(expected.InfiltrationCoolingLoadW, result.Value.Infiltration.CoolingLoadW, fixture.Tolerances.HourlyLoadW);
        AssertClose(expected.TotalHeatingLoadW, result.Value.TotalHeatingLoadW, fixture.Tolerances.HourlyLoadW);
        AssertClose(expected.TotalCoolingLoadW, result.Value.TotalCoolingLoadW, fixture.Tolerances.HourlyLoadW);

        foreach (var diagnosticCode in expected.ExpectedDiagnosticCodes)
            Assert.Contains(result.Value.Diagnostics, diagnostic => diagnostic.Code == diagnosticCode);
    }

    [Fact]
    public void SingleZoneNoSolarFixtureVerifiesHeatingCoolingTransmissionVentilationAndInternalGains()
    {
        var fixture = ExternalReferenceValidationFixtureLoader.Load("single-zone-no-solar.json");
        var input = fixture.Input;
        var envelope = AssertRequired(input.Envelope);
        var internalGains = AssertRequired(input.InternalGains);
        var solar = AssertRequired(input.Solar);
        var simulation = AssertRequired(input.Simulation);
        var profile = CreateHourlyInputProfile(
            fixture.FixtureName,
            simulation,
            envelope,
            solar.ConstantSolarGainsW,
            internalGains.ConstantInternalGainsW);

        var result = Iso52016MatrixTestSolver.Solve(
            profile,
            new Iso52016RoomHeatBalanceOptions(
                InitialIndoorTemperatureC: simulation.InitialIndoorTemperatureC,
                TimeStepSeconds: simulation.TimeStepSeconds));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(fixture.ExpectedHourlyResults.HourCount, result.Value.Hours.Count);
        AssertClose(
            fixture.ExpectedAnnualResults.TransmissionHeatTransferCoefficientWPerK!.Value,
            profile.TransmissionHeatTransferCoefficientWPerK,
            fixture.Tolerances.HeatTransferCoefficientWPerK);
        AssertClose(
            fixture.ExpectedAnnualResults.VentilationHeatTransferCoefficientWPerK!.Value,
            profile.VentilationHeatTransferCoefficientWPerK,
            fixture.Tolerances.HeatTransferCoefficientWPerK);
        AssertClose(
            fixture.ExpectedAnnualResults.HeatingDemandKWh,
            result.Value.AnnualHeatingEnergyKWh,
            fixture.Tolerances.AnnualDemandKWh);
        AssertClose(
            fixture.ExpectedAnnualResults.CoolingDemandKWh,
            result.Value.AnnualCoolingEnergyKWh,
            fixture.Tolerances.AnnualDemandKWh);
        AssertClose(
            fixture.ExpectedAnnualResults.InternalGainsKWh,
            result.Value.AnnualInternalGainsKWh,
            fixture.Tolerances.AnnualDemandKWh);

        var representative = Assert.Single(fixture.ExpectedHourlyResults.RepresentativeHours);
        var actualHour = result.Value.GetHour(representative.HourOfYear);

        AssertClose(representative.HeatingLoadW!.Value, actualHour.HeatingLoadW, fixture.Tolerances.HourlyLoadW);
        AssertClose(representative.CoolingLoadW!.Value, actualHour.CoolingLoadW, fixture.Tolerances.HourlyLoadW);
        AssertClose(representative.InternalGainsW!.Value, actualHour.InternalGainsW, fixture.Tolerances.HourlyLoadW);
        AssertClose(
            representative.IndoorTemperatureAfterHvacC!.Value,
            actualHour.IndoorTemperatureAfterHvacC,
            fixture.Tolerances.HourlyTemperatureC);
    }

    [Fact]
    public void SouthWindowSolarFixtureVerifiesSolarGains()
    {
        var fixture = ExternalReferenceValidationFixtureLoader.Load("single-zone-solar-south-window.json");
        var solar = AssertRequired(fixture.Input.Solar);
        var window = AssertRequired(solar.Window);
        var representative = Assert.Single(fixture.ExpectedHourlyResults.RepresentativeHours);
        var sourceHour = solar.HourlySouthSurfaceIrradiance.Single(hour => hour.HourOfYear == representative.HourOfYear);
        var orientation = Enum.Parse<CardinalDirection>(window.Orientation, ignoreCase: true);
        var calculator = new Iso52016WindowSolarGainCalculator();

        var result = calculator.Calculate(
            new Iso52016WindowSolarGainRequest(
                CreateSolarHour(sourceHour),
                orientation,
                window.AreaM2,
                window.SolarHeatGainCoefficient,
                window.FrameFraction,
                window.ShadingFactor));

        Assert.True(result.IsSuccess, result.Error);
        AssertClose(representative.SolarGainsW!.Value, result.Value.TotalSolarGainW, fixture.Tolerances.HourlyLoadW);
        AssertClose(representative.BeamSolarGainW!.Value, result.Value.BeamSolarGainW, fixture.Tolerances.HourlyLoadW);
        AssertClose(representative.DiffuseSkySolarGainW!.Value, result.Value.DiffuseSkySolarGainW, fixture.Tolerances.HourlyLoadW);
        AssertClose(representative.GroundReflectedSolarGainW!.Value, result.Value.GroundReflectedSolarGainW, fixture.Tolerances.HourlyLoadW);

        var transmissionFactor =
            window.AreaM2 *
            (1.0 - window.FrameFraction) *
            window.SolarHeatGainCoefficient *
            window.ShadingFactor;
        var dailySolarGainsKWh = solar.HourlySouthSurfaceIrradiance
            .Sum(hour => hour.TotalIrradianceWm2 * transmissionFactor) / 1000.0;

        AssertClose(
            fixture.ExpectedAnnualResults.SolarGainsKWh,
            dailySolarGainsKWh,
            fixture.Tolerances.AnnualDemandKWh);
        AssertClose(
            fixture.ExpectedMonthlyResults.Single().SolarGainsKWh,
            dailySolarGainsKWh,
            fixture.Tolerances.MonthlyDemandKWh);
    }

    [Fact]
    public void Annual8760FixtureVerifiesCompactAnnualAggregation()
    {
        var fixture = ExternalReferenceValidationFixtureLoader.Load("single-zone-annual-8760.json");
        var aggregation = AssertRequired(fixture.Input.AnnualAggregation);

        Assert.Equal("monthly-constant", aggregation.HourlyPattern);
        Assert.Equal(8760, aggregation.Months.Sum(month => month.Hours));
        Assert.Equal(8760, fixture.ExpectedHourlyResults.HourCount);
        Assert.Equal(8760, fixture.ExpectedAnnualResults.HourCount);

        foreach (var month in aggregation.Months)
        {
            var expectedMonth = fixture.ExpectedMonthlyResults.Single(expected => expected.Month == month.Month);

            Assert.Equal(month.Hours, expectedMonth.Hours);
            AssertClose(expectedMonth.HeatingDemandKWh, month.HeatingLoadW * month.Hours / 1000.0, fixture.Tolerances.MonthlyDemandKWh);
            AssertClose(expectedMonth.CoolingDemandKWh, month.CoolingLoadW * month.Hours / 1000.0, fixture.Tolerances.MonthlyDemandKWh);
            AssertClose(expectedMonth.InternalGainsKWh, month.InternalGainsW * month.Hours / 1000.0, fixture.Tolerances.MonthlyDemandKWh);
            AssertClose(expectedMonth.SolarGainsKWh, month.SolarGainsW * month.Hours / 1000.0, fixture.Tolerances.MonthlyDemandKWh);
        }

        AssertClose(
            fixture.ExpectedAnnualResults.HeatingDemandKWh,
            fixture.ExpectedMonthlyResults.Sum(month => month.HeatingDemandKWh),
            fixture.Tolerances.AnnualDemandKWh);
        AssertClose(
            fixture.ExpectedAnnualResults.CoolingDemandKWh,
            fixture.ExpectedMonthlyResults.Sum(month => month.CoolingDemandKWh),
            fixture.Tolerances.AnnualDemandKWh);
        AssertClose(
            fixture.ExpectedAnnualResults.InternalGainsKWh,
            fixture.ExpectedMonthlyResults.Sum(month => month.InternalGainsKWh),
            fixture.Tolerances.AnnualDemandKWh);
        AssertClose(
            fixture.ExpectedAnnualResults.SolarGainsKWh,
            fixture.ExpectedMonthlyResults.Sum(month => month.SolarGainsKWh),
            fixture.Tolerances.AnnualDemandKWh);
        Assert.Equal(fixture.ExpectedAnnualResults.PeakHeatingLoadW, fixture.ExpectedHourlyResults.CompactSummary!.PeakHeatingLoadW);
        Assert.Equal(fixture.ExpectedAnnualResults.PeakCoolingLoadW, fixture.ExpectedHourlyResults.CompactSummary!.PeakCoolingLoadW);
    }

    private static Iso52016RoomHourlyInputProfile CreateHourlyInputProfile(
        string roomCode,
        EnergyCalculationSimulationInput simulation,
        EnergyCalculationEnvelopeInput envelope,
        double solarGainsW,
        double internalGainsW)
    {
        var hours = Enumerable
            .Range(0, simulation.HourCount)
            .Select(hour => new Iso52016RoomHourlyInputRecord(
                HourOfYear: hour,
                Month: 1,
                Day: 1,
                Hour: hour % 24,
                OutdoorTemperatureC: simulation.OutdoorTemperatureC,
                GroundBoundaryTemperatureC: simulation.OutdoorTemperatureC,
                HeatingSetpointC: simulation.HeatingSetpointC,
                CoolingSetpointC: simulation.CoolingSetpointC,
                TransmissionHeatTransferCoefficientWPerK: envelope.TransmissionHeatTransferCoefficientWPerK,
                VentilationHeatTransferCoefficientWPerK: envelope.VentilationHeatTransferCoefficientWPerK,
                TotalHeatTransferCoefficientWPerK: envelope.TransmissionHeatTransferCoefficientWPerK + envelope.VentilationHeatTransferCoefficientWPerK,
                ThermalCapacityJPerK: envelope.ThermalCapacityJPerK,
                SolarGainsW: solarGainsW,
                InternalGainsW: internalGainsW,
                TotalGainsW: solarGainsW + internalGainsW))
            .ToArray();

        return new Iso52016RoomHourlyInputProfile(
            RoomCode: roomCode,
            TransmissionHeatTransferCoefficientWPerK: envelope.TransmissionHeatTransferCoefficientWPerK,
            VentilationHeatTransferCoefficientWPerK: envelope.VentilationHeatTransferCoefficientWPerK,
            ThermalCapacityJPerK: envelope.ThermalCapacityJPerK,
            HeatingSetpointC: simulation.HeatingSetpointC,
            CoolingSetpointC: simulation.CoolingSetpointC,
            Hours: hours);
    }

    private static Iso52016HourlyWeatherSolarRecord CreateSolarHour(
        EnergyCalculationSolarHourInput hour) =>
        new(
            HourOfYear: hour.HourOfYear,
            Month: 1,
            Day: 1,
            Hour: hour.HourOfYear % 24,
            OutdoorTemperatureC: 20,
            GroundBoundaryTemperatureC: 20,
            SolarAltitudeDegrees: 35,
            SolarAzimuthDegrees: 180,
            DirectNormalIrradianceWm2: hour.BeamIrradianceWm2,
            DiffuseHorizontalIrradianceWm2: hour.DiffuseSkyIrradianceWm2,
            GlobalHorizontalIrradianceWm2: hour.TotalIrradianceWm2,
            SurfaceIrradiance:
            [
                new Iso52016SurfaceWeatherSolarRecord(
                    SurfaceCode: WeatherSolarSurfaceCodes.South,
                    Orientation: SurfaceOrientation.SouthVertical,
                    IncidenceAngleDegrees: 0,
                    BeamIrradianceWm2: hour.BeamIrradianceWm2,
                    DiffuseSkyIrradianceWm2: hour.DiffuseSkyIrradianceWm2,
                    GroundReflectedIrradianceWm2: hour.GroundReflectedIrradianceWm2,
                    TotalIrradianceWm2: hour.TotalIrradianceWm2)
            ]);

    private static TransmissionElementInput CreateTransmissionElementInput(
        TransmissionFixtureElementInput element) =>
        new(
            ElementId: element.ElementId,
            ElementType: Enum.Parse<TransmissionElementType>(element.ElementType),
            RoomId: element.RoomId,
            AreaM2: element.AreaM2,
            UValueWPerM2K: element.UValueWPerM2K,
            IndoorTemperatureC: element.IndoorTemperatureC,
            BoundaryType: Enum.Parse<TransmissionBoundaryType>(element.BoundaryType),
            OutdoorTemperatureC: element.OutdoorTemperatureC,
            BoundaryTemperatureC: element.BoundaryTemperatureC,
            AdjacentTemperatureC: element.AdjacentTemperatureC,
            GroundTemperatureC: element.GroundTemperatureC,
            CorrectionFactor: element.CorrectionFactor,
            DiagnosticsContext: element.DiagnosticsContext);

    private static WindowSolarGainInput CreateWindowSolarGainInput(
        WindowSolarGainFixtureWindowInput window) =>
        new(
            WindowId: window.WindowId,
            RoomId: window.RoomId,
            AreaM2: window.AreaM2,
            OrientationAzimuthDeg: window.OrientationAzimuthDeg,
            TiltDeg: window.TiltDeg,
            Shgc: window.Shgc,
            FrameFactor: window.FrameFactor,
            InternalShadingFactor: window.InternalShadingFactor,
            ExternalShadingFactor: window.ExternalShadingFactor,
            FixedShadingFactor: window.FixedShadingFactor,
            IncidentIrradianceWPerM2: window.IncidentIrradianceWPerM2,
            DirectIrradianceWPerM2: window.DirectIrradianceWPerM2,
            DiffuseIrradianceWPerM2: window.DiffuseIrradianceWPerM2,
            GroundReflectedIrradianceWPerM2: window.GroundReflectedIrradianceWPerM2,
            HourIndex: window.HourIndex,
            IsNight: window.IsNight,
            DiagnosticsContext: window.DiagnosticsContext);

    private static VentilationAndInfiltrationLoadInput CreateVentilationInfiltrationInput(
        VentilationInfiltrationFixtureInput input) =>
        new(
            RoomId: input.RoomId,
            AreaM2: input.AreaM2,
            VolumeM3: input.VolumeM3,
            OccupancyPeople: input.OccupancyPeople,
            IndoorTemperatureC: input.IndoorTemperatureC,
            OutdoorTemperatureC: input.OutdoorTemperatureC,
            MechanicalAirflowM3PerHour: input.MechanicalAirflowM3PerHour,
            AirflowLitersPerSecond: input.AirflowLitersPerSecond,
            AirflowPerPersonLps: input.AirflowPerPersonLps,
            AirflowPerAreaLpsM2: input.AirflowPerAreaLpsM2,
            AirChangesPerHour: input.AirChangesPerHour,
            InfiltrationAirChangesPerHour: input.InfiltrationAirChangesPerHour,
            InfiltrationAirflowM3PerHour: input.InfiltrationAirflowM3PerHour,
            NaturalVentilationAirflowM3PerHour: input.NaturalVentilationAirflowM3PerHour,
            HeatRecoveryEfficiency: input.HeatRecoveryEfficiency,
            ScheduleFactor: input.ScheduleFactor,
            AirDensityKgPerM3: input.AirDensityKgPerM3,
            AirSpecificHeatJPerKgK: input.AirSpecificHeatJPerKgK,
            DiagnosticsContext: input.DiagnosticsContext);

    private static T AssertRequired<T>(T? value)
        where T : class
    {
        Assert.NotNull(value);
        return value;
    }

    private static void AssertClose(
        double expected,
        double actual,
        double tolerance) =>
        Assert.InRange(actual, expected - tolerance, expected + tolerance);
}
