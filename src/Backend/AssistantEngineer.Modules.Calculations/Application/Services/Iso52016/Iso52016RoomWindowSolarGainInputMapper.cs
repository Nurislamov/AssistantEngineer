using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016RoomWindowSolarGainInputMapper : IIso52016RoomWindowSolarGainInputMapper
{
    public Result<IReadOnlyList<Iso52016WindowSolarGainInput>> Map(
        Room room,
        Iso52016RoomSimulationDefaults defaults)
    {
        if (room is null)
            return Result<IReadOnlyList<Iso52016WindowSolarGainInput>>.Validation("Room is required.");

        if (defaults is null)
            return Result<IReadOnlyList<Iso52016WindowSolarGainInput>>.Validation("Room simulation defaults are required.");

        var inputs = room.Windows
            .Select((window, index) => MapWindow(
                window,
                index,
                defaults))
            .ToArray();

        return Result<IReadOnlyList<Iso52016WindowSolarGainInput>>.Success(inputs);
    }

    private static Iso52016WindowSolarGainInput MapWindow(
        Window window,
        int index,
        Iso52016RoomSimulationDefaults defaults)
    {
        var windowCode =
            window.Id > 0
                ? $"window-{window.Id}"
                : $"window-{index + 1}";

        return new Iso52016WindowSolarGainInput(
            WindowCode: windowCode,
            Orientation: window.Orientation,
            WindowAreaM2: window.Area.SquareMeters,
            SolarHeatGainCoefficient: window.Shgc?.Value ?? defaults.DefaultSolarHeatGainCoefficient,
            FrameFraction: defaults.FrameFraction,
            ShadingFactor: CalculateSimplifiedShadingFactor(window.Shading));
    }

    private static double CalculateSimplifiedShadingFactor(
        WindowShadingParameters shading)
    {
        var hasGeometry =
            shading.OverhangDepthM > 0 ||
            shading.SideFinDepthM > 0 ||
            shading.RevealDepthM > 0;

        if (!hasGeometry)
            return 1.0;

        return Math.Clamp(
            Math.Max(
                shading.MinimumDirectSolarReductionFactor,
                shading.DiffuseSolarShareUnaffected),
            0.0,
            1.0);
    }
}