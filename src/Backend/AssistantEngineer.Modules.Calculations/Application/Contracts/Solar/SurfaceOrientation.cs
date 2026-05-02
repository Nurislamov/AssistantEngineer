namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;

public sealed record SurfaceOrientation(
    double TiltDegrees,
    double AzimuthDegrees)
{
    public static SurfaceOrientation Horizontal { get; } =
        new(
            TiltDegrees: 0,
            AzimuthDegrees: 180);

    public static SurfaceOrientation NorthVertical { get; } =
        new(
            TiltDegrees: 90,
            AzimuthDegrees: 0);

    public static SurfaceOrientation EastVertical { get; } =
        new(
            TiltDegrees: 90,
            AzimuthDegrees: 90);

    public static SurfaceOrientation SouthVertical { get; } =
        new(
            TiltDegrees: 90,
            AzimuthDegrees: 180);

    public static SurfaceOrientation WestVertical { get; } =
        new(
            TiltDegrees: 90,
            AzimuthDegrees: 270);
}