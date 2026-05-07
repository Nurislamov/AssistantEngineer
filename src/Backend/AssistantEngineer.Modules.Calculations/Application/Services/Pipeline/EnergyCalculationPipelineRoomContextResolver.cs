using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Abstractions;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;

internal sealed class EnergyCalculationPipelineRoomContextResolver
{
    private const string AnnualClimateDataSolarSource = "AnnualClimateData";
    private const string ReferenceSolarFallbackSource = "ReferenceByOrientationFallback";

    private readonly ICoolingLoadReferenceData _coolingReferenceData;
    private readonly IGroundTemperatureService? _groundTemperatureService;
    private readonly ISolarRadiationService? _solarRadiationService;
    private readonly Iso52016EnergyNeedOptions _energyNeedOptions;

    public EnergyCalculationPipelineRoomContextResolver(
        ICoolingLoadReferenceData coolingReferenceData,
        IGroundTemperatureService? groundTemperatureService,
        ISolarRadiationService? solarRadiationService,
        Iso52016EnergyNeedOptions energyNeedOptions)
    {
        _coolingReferenceData = coolingReferenceData;
        _groundTemperatureService = groundTemperatureService;
        _solarRadiationService = solarRadiationService;
        _energyNeedOptions = energyNeedOptions;
    }

    public RoomGroundContext ResolveGroundContext(
        Room room,
        PipelineClimateContext climateContext)
    {
        if (!room.Walls.Any(wall => wall.BoundaryType == WallBoundaryType.Ground))
            return RoomGroundContext.Empty;

        var diagnostics = new List<CalculationDiagnostic>();
        var assumptions = new List<string>();
        var context = $"Room {room.Id} application ground boundary";

        if (room.GroundContactMetadata is null)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "GroundContact.MetadataMissing",
                "Ground boundary exists, but ground contact metadata is missing.",
                context));
        }

        if (_groundTemperatureService is not null &&
            climateContext.AnnualClimateData is not null &&
            climateContext.AnnualClimateData.HourlyData.Count > 0)
        {
            var hourly = climateContext.AnnualClimateData.HourlyData.ToArray();
            var monthly = Enumerable.Range(1, 12)
                .Select(month => _groundTemperatureService.GetMonthlyAverageTemperature(hourly, month))
                .ToArray();

            var heatingGroundTemperature = monthly.Min();
            var coolingGroundTemperature = monthly.Max();
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "GroundContact.GroundTemperatureProfileUsed",
                "Ground boundary temperature was resolved from the existing ground temperature profile service.",
                context));
            assumptions.Add("Ground boundary uses existing ground temperature profile values for design-point transmission.");
            return new RoomGroundContext(
                heatingGroundTemperature,
                coolingGroundTemperature,
                diagnostics,
                assumptions);
        }

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Warning,
            "GroundContact.DefaultBoundaryTemperatureUsed",
            $"Ground boundary temperature profile was unavailable; default boundary temperature {_energyNeedOptions.DefaultGroundBoundaryTemperatureC} C was used.",
            context));
        assumptions.Add("Ground boundary uses the configured default boundary temperature when profile data is unavailable.");
        return new RoomGroundContext(
            _energyNeedOptions.DefaultGroundBoundaryTemperatureC,
            _energyNeedOptions.DefaultGroundBoundaryTemperatureC,
            diagnostics,
            assumptions);
    }

    public RoomSolarContext ResolveSolarContext(
        Room room,
        PipelineClimateContext climateContext)
    {
        if (room.Windows.Count == 0)
            return RoomSolarContext.Empty;

        var diagnostics = new List<CalculationDiagnostic>();
        var assumptions = new List<string>();
        var irradianceByWindowId = new Dictionary<int, double>();
        var context = $"Room {room.Id} application solar gains";

        if (_solarRadiationService is not null &&
            climateContext.AnnualClimateData is not null &&
            climateContext.AnnualClimateData.HourlyData.Count > 0)
        {
            foreach (var window in room.Windows)
            {
                var irradiance = climateContext.AnnualClimateData.HourlyData
                    .Select(hour =>
                    {
                        var timestamp = new DateTime(
                                climateContext.AnnualClimateData.Year,
                                1,
                                1,
                                0,
                                0,
                                0,
                                DateTimeKind.Utc)
                            .AddHours(hour.HourOfYear);
                        return _solarRadiationService.CalculateVerticalSurfaceRadiation(
                            hour,
                            window.Orientation,
                            _energyNeedOptions.LatitudeDegrees,
                            timestamp.DayOfYear,
                            timestamp.Hour);
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                irradianceByWindowId[window.Id] = irradiance;
            }

            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "SolarGains.IrradianceSource",
                $"Solar irradiance source: {AnnualClimateDataSolarSource}.",
                context));
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "SolarWeather.AnnualClimateSolarDataUsed",
                "Annual climate direct and diffuse solar data were used to resolve design-point window irradiance.",
                context));
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "SolarWeather.SurfaceIrradianceCalculated",
                "Window irradiance was calculated through the centralized solar position and surface irradiance path.",
                context));
            assumptions.Add("Window solar gains use available annual climate solar data for design-point irradiance.");
            return new RoomSolarContext(irradianceByWindowId, diagnostics, assumptions);
        }

        foreach (var window in room.Windows)
        {
            irradianceByWindowId[window.Id] =
                _coolingReferenceData.GetWindowSolarLoadWPerM2(window.Orientation);
        }

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Warning,
            "SolarGains.ReferenceByOrientationFallback",
            "Window solar gain uses orientation reference irradiance because hourly weather/solar context was not available.",
            context));
        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Warning,
            "SolarWeather.ReferenceByOrientationFallbackUsed",
            "Reference irradiance by window orientation was used because hourly weather/solar context was not available.",
            context));
        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Warning,
            "SolarWeather.MissingDirectDiffuseSolarData",
            "Hourly direct and diffuse solar data were unavailable for this application path.",
            context));
        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Info,
            "SolarGains.IrradianceSource",
            $"Solar irradiance source: {ReferenceSolarFallbackSource}.",
            context));
        assumptions.Add("Window solar gains use orientation reference irradiance fallback when annual weather/solar data is unavailable.");
        return new RoomSolarContext(irradianceByWindowId, diagnostics, assumptions);
    }

    public static void AddInternalGainScheduleDiagnostics(
        Room room,
        List<CalculationDiagnostic> diagnostics,
        List<string> assumptions)
    {
        var context = $"Room {room.Id} application internal gains";
        var hasSchedules =
            room.OccupancySchedule is not null ||
            room.EquipmentSchedule is not null ||
            room.LightingSchedule is not null;

        diagnostics.Add(new CalculationDiagnostic(
            hasSchedules ? CalculationDiagnosticSeverity.Warning : CalculationDiagnosticSeverity.Info,
            hasSchedules
                ? "InternalGains.DesignPointFullScheduleFactorWithSchedules"
                : "InternalGains.DesignPointFullScheduleFactor",
            hasSchedules
                ? "Design-point internal gains use full schedule factor 1.0; room schedules are reserved for hourly analysis paths."
                : "Design-point internal gains use full schedule factor 1.0.",
            context));
        assumptions.Add("Design-point internal gains use full schedule factor 1.0.");
    }
}
