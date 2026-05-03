using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SolarGains;

internal static class WindowSolarGainInputFactory
{
    public static WindowSolarGainInput CreateForWindow(
        Window window,
        double incidentIrradianceWPerM2,
        double frameFactor = 1.0,
        double internalShadingFactor = 1.0,
        double externalShadingFactor = 1.0,
        double fixedShadingFactor = 1.0,
        int? hourIndex = null,
        bool isNight = false,
        string? diagnosticsContext = null)
    {
        var surface = WeatherSolarSurface.FromCardinalDirection(window.Orientation);

        return new WindowSolarGainInput(
            WindowId: window.Id,
            RoomId: window.RoomId,
            AreaM2: window.Area.SquareMeters,
            OrientationAzimuthDeg: surface.Orientation.AzimuthDegrees,
            TiltDeg: surface.Orientation.TiltDegrees,
            Shgc: window.Shgc?.Value,
            FrameFactor: frameFactor,
            InternalShadingFactor: internalShadingFactor,
            ExternalShadingFactor: externalShadingFactor,
            FixedShadingFactor: fixedShadingFactor,
            IncidentIrradianceWPerM2: incidentIrradianceWPerM2,
            HourIndex: hourIndex,
            IsNight: isNight,
            DiagnosticsContext: diagnosticsContext ?? $"Room {window.RoomId} window {window.Id}");
    }

    public static WindowSolarGainInput CreateForWindow(
        Window window,
        Iso52016SurfaceWeatherSolarRecord surface,
        double frameFactor = 1.0,
        double internalShadingFactor = 1.0,
        double externalShadingFactor = 1.0,
        double fixedShadingFactor = 1.0,
        int? hourIndex = null,
        bool isNight = false,
        string? diagnosticsContext = null)
    {
        return new WindowSolarGainInput(
            WindowId: window.Id,
            RoomId: window.RoomId,
            AreaM2: window.Area.SquareMeters,
            OrientationAzimuthDeg: surface.Orientation.AzimuthDegrees,
            TiltDeg: surface.Orientation.TiltDegrees,
            Shgc: window.Shgc?.Value,
            FrameFactor: frameFactor,
            InternalShadingFactor: internalShadingFactor,
            ExternalShadingFactor: externalShadingFactor,
            FixedShadingFactor: fixedShadingFactor,
            IncidentIrradianceWPerM2: surface.TotalIrradianceWm2,
            DirectIrradianceWPerM2: surface.BeamIrradianceWm2,
            DiffuseIrradianceWPerM2: surface.DiffuseSkyIrradianceWm2,
            GroundReflectedIrradianceWPerM2: surface.GroundReflectedIrradianceWm2,
            HourIndex: hourIndex,
            IsNight: isNight,
            DiagnosticsContext: diagnosticsContext ?? $"Room {window.RoomId} window {window.Id} ISO 52016 surface {surface.SurfaceCode}");
    }
}
