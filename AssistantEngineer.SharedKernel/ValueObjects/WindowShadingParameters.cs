using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.SharedKernel.ValueObjects;

public record WindowShadingParameters
{
    public double OverhangDepthM { get; }
    public double SideFinDepthM { get; }
    public double RevealDepthM { get; }
    public double WindowHeightM { get; }
    public double WindowWidthM { get; }
    public double MinimumDirectSolarReductionFactor { get; }
    public double DiffuseSolarShareUnaffected { get; }

    private WindowShadingParameters(
        double overhangDepthM,
        double sideFinDepthM,
        double revealDepthM,
        double windowHeightM,
        double windowWidthM,
        double minimumDirectSolarReductionFactor,
        double diffuseSolarShareUnaffected)
    {
        OverhangDepthM = overhangDepthM;
        SideFinDepthM = sideFinDepthM;
        RevealDepthM = revealDepthM;
        WindowHeightM = windowHeightM;
        WindowWidthM = windowWidthM;
        MinimumDirectSolarReductionFactor = minimumDirectSolarReductionFactor;
        DiffuseSolarShareUnaffected = diffuseSolarShareUnaffected;
    }

    public static WindowShadingParameters None { get; } = new(
        overhangDepthM: 0,
        sideFinDepthM: 0,
        revealDepthM: 0,
        windowHeightM: 0,
        windowWidthM: 0,
        minimumDirectSolarReductionFactor: 0.15,
        diffuseSolarShareUnaffected: 0.3);

    public static Result<WindowShadingParameters> Create(
        double overhangDepthM = 0,
        double sideFinDepthM = 0,
        double revealDepthM = 0,
        double windowHeightM = 0,
        double windowWidthM = 0,
        double minimumDirectSolarReductionFactor = 0.15,
        double diffuseSolarShareUnaffected = 0.3)
    {
        var finiteValues = new Dictionary<string, double>
        {
            ["Overhang depth"] = overhangDepthM,
            ["Side fin depth"] = sideFinDepthM,
            ["Reveal depth"] = revealDepthM,
            ["Window height"] = windowHeightM,
            ["Window width"] = windowWidthM,
            ["Minimum direct solar reduction factor"] = minimumDirectSolarReductionFactor,
            ["Diffuse solar share unaffected"] = diffuseSolarShareUnaffected
        };

        foreach (var (name, value) in finiteValues)
        {
            var finiteCheck = Guard.AgainstNonFinite(value, name);
            if (finiteCheck.IsFailure)
                return Result<WindowShadingParameters>.Failure(finiteCheck);
        }

        if (overhangDepthM < 0 || sideFinDepthM < 0 || revealDepthM < 0)
            return Result<WindowShadingParameters>.Validation("Window shading depths cannot be negative.");

        if (windowHeightM < 0 || windowWidthM < 0)
            return Result<WindowShadingParameters>.Validation("Window shading dimensions cannot be negative.");

        if (minimumDirectSolarReductionFactor is < 0 or > 1)
            return Result<WindowShadingParameters>.Validation("Minimum direct solar reduction factor must be between 0 and 1.");

        if (diffuseSolarShareUnaffected is < 0 or > 1)
            return Result<WindowShadingParameters>.Validation("Diffuse solar share unaffected must be between 0 and 1.");

        return Result<WindowShadingParameters>.Success(new WindowShadingParameters(
            overhangDepthM,
            sideFinDepthM,
            revealDepthM,
            windowHeightM,
            windowWidthM,
            minimumDirectSolarReductionFactor,
            diffuseSolarShareUnaffected));
    }
}
