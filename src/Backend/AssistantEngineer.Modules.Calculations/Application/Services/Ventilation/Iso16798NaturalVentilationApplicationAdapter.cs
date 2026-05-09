using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;
using AssistantEngineer.Modules.Calculations.Application.Models.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class Iso16798NaturalVentilationApplicationAdapter
{
    public Iso16798NaturalVentilationInput BuildInput(
        Room room,
        NaturalVentilationOpeningState openingState,
        NaturalVentilationOptions options,
        double indoorTemperatureC,
        double outdoorTemperatureC,
        double windSpeedMPerS,
        double? openingScheduleFraction = null,
        double? occupancyFraction = null,
        double? altitudeMeters = null)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(options);

        var ventilation = room.VentilationParameters;
        var stackCoefficient = ventilation?.StackCoefficient ?? 0.0;
        var windCoefficient = ventilation?.WindCoefficient ?? 0.0;
        var windExposure = ventilation?.WindExposureFactor ?? 1.0;
        var openingArea = Math.Max(openingState.EffectiveOpeningAreaM2, 0.0);

        return new Iso16798NaturalVentilationInput(
            RoomVolumeM3: Math.Max(room.CalculateVolume(), 0.001),
            IndoorTemperatureC: indoorTemperatureC,
            OutdoorTemperatureC: outdoorTemperatureC,
            WindSpeedMPerS: windSpeedMPerS,
            AirDensityKgPerM3: AirPhysicalConstants.AirDensityKgPerM3,
            AirSpecificHeatJPerKgK: AirPhysicalConstants.AirSpecificHeatJPerKgK,
            DischargeCoefficient: options.OpeningDischargeCoefficient,
            MaximumAirChangesPerHour: options.MaximumAirChangesPerHour,
            OpeningHeightM: Math.Max(room.HeightM, 0.0),
            UsefulHeightDifferenceM: Math.Max(room.HeightM * 0.5, 0.0),
            WindPressureCoefficient: 1.0,
            WindExposureFactor: Math.Max(windExposure, 0.0),
            StackCoefficient: Math.Max(stackCoefficient, 0.0),
            WindCoefficient: Math.Max(windCoefficient, 0.0),
            Openings: new[]
            {
                new Iso16798NaturalVentilationOpeningInput(
                    OpeningId: "compatibility-opening",
                    OpeningAreaM2: openingArea,
                    OpeningRatio: openingState.OpeningFactor,
                    IsOpen: openingState.IsOpen)
            },
            NaturalVentilationOpenings: new[]
            {
                new NaturalVentilationOpening(
                    OpeningId: "compatibility-opening",
                    OpeningAreaM2: openingArea,
                    OpeningFraction: openingState.OpeningFactor,
                    IsOpen: openingState.IsOpen,
                    OpeningHeightM: Math.Max(room.HeightM, 0.0),
                    DischargeCoefficient: options.OpeningDischargeCoefficient)
            },
            OpeningSchedule: new NaturalVentilationOpeningSchedule(
                OpeningFraction: Math.Clamp(openingScheduleFraction ?? 1.0, 0.0, 1.0)),
            DrivingForces: new NaturalVentilationDrivingForces(
                IndoorTemperatureC: indoorTemperatureC,
                OutdoorTemperatureC: outdoorTemperatureC,
                WindSpeedMPerS: windSpeedMPerS,
                OpeningHeightM: Math.Max(room.HeightM, 0.0),
                AirDensityKgPerM3: AirPhysicalConstants.AirDensityKgPerM3,
                AirSpecificHeatJPerKgK: AirPhysicalConstants.AirSpecificHeatJPerKgK,
                AltitudeMeters: altitudeMeters),
            OccupancyControl: new NaturalVentilationOccupancyControl(
                Enabled: occupancyFraction.HasValue,
                OccupancyFraction: Math.Clamp(occupancyFraction ?? 1.0, 0.0, 1.0),
                MinimumOccupancyFractionToEnable: 0.01,
                DisableWhenUnoccupied: true),
            CalculationOptions: new NaturalVentilationCalculationOptions(
                BranchSelectionMode: NaturalVentilationBranchSelectionMode.SumWindAndStack,
                UseDensityCorrection: true,
                UseAltitudeDensityCorrection: altitudeMeters.HasValue,
                SingleSidedOpeningCoefficient: 1.0,
                MaximumAirChangesPerHour: options.MaximumAirChangesPerHour));
    }
}
