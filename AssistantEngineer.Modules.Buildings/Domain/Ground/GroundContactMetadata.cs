using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Domain.Ground;

public sealed class GroundContactMetadata
{
    private GroundContactMetadata()
    {
    }

    private GroundContactMetadata(
        GroundContactType contactType,
        double exposedPerimeterM,
        double burialDepthM,
        double wallHeightBelowGradeM,
        double horizontalInsulationWidthM,
        double perimeterInsulationDepthM,
        double underfloorVentilationAirChangesPerHour)
    {
        ContactType = contactType;
        ExposedPerimeterM = exposedPerimeterM;
        BurialDepthM = burialDepthM;
        WallHeightBelowGradeM = wallHeightBelowGradeM;
        HorizontalInsulationWidthM = horizontalInsulationWidthM;
        PerimeterInsulationDepthM = perimeterInsulationDepthM;
        UnderfloorVentilationAirChangesPerHour = underfloorVentilationAirChangesPerHour;
    }

    public GroundContactType ContactType { get; private set; }
    public double ExposedPerimeterM { get; private set; }
    public double BurialDepthM { get; private set; }
    public double WallHeightBelowGradeM { get; private set; }
    public double HorizontalInsulationWidthM { get; private set; }
    public double PerimeterInsulationDepthM { get; private set; }
    public double UnderfloorVentilationAirChangesPerHour { get; private set; }

    public static Result<GroundContactMetadata> Create(
        GroundContactType contactType,
        double exposedPerimeterM,
        double burialDepthM,
        double wallHeightBelowGradeM,
        double horizontalInsulationWidthM,
        double perimeterInsulationDepthM,
        double underfloorVentilationAirChangesPerHour)
    {
        if (exposedPerimeterM <= 0)
            return Result<GroundContactMetadata>.Validation("Exposed perimeter must be positive.");

        if (burialDepthM < 0)
            return Result<GroundContactMetadata>.Validation("Burial depth cannot be negative.");

        if (wallHeightBelowGradeM < 0)
            return Result<GroundContactMetadata>.Validation("Wall height below grade cannot be negative.");

        if (horizontalInsulationWidthM < 0)
            return Result<GroundContactMetadata>.Validation("Horizontal insulation width cannot be negative.");

        if (perimeterInsulationDepthM < 0)
            return Result<GroundContactMetadata>.Validation("Perimeter insulation depth cannot be negative.");

        if (underfloorVentilationAirChangesPerHour < 0)
            return Result<GroundContactMetadata>.Validation("Underfloor ventilation ACH cannot be negative.");

        return Result<GroundContactMetadata>.Success(new GroundContactMetadata(
            contactType,
            exposedPerimeterM,
            burialDepthM,
            wallHeightBelowGradeM,
            horizontalInsulationWidthM,
            perimeterInsulationDepthM,
            underfloorVentilationAirChangesPerHour));
    }

    public Result Update(
        GroundContactType contactType,
        double exposedPerimeterM,
        double burialDepthM,
        double wallHeightBelowGradeM,
        double horizontalInsulationWidthM,
        double perimeterInsulationDepthM,
        double underfloorVentilationAirChangesPerHour)
    {
        var candidate = Create(
            contactType,
            exposedPerimeterM,
            burialDepthM,
            wallHeightBelowGradeM,
            horizontalInsulationWidthM,
            perimeterInsulationDepthM,
            underfloorVentilationAirChangesPerHour);

        if (candidate.IsFailure)
            return Result.Failure(candidate.Error, candidate.ErrorType);

        ContactType = candidate.Value.ContactType;
        ExposedPerimeterM = candidate.Value.ExposedPerimeterM;
        BurialDepthM = candidate.Value.BurialDepthM;
        WallHeightBelowGradeM = candidate.Value.WallHeightBelowGradeM;
        HorizontalInsulationWidthM = candidate.Value.HorizontalInsulationWidthM;
        PerimeterInsulationDepthM = candidate.Value.PerimeterInsulationDepthM;
        UnderfloorVentilationAirChangesPerHour = candidate.Value.UnderfloorVentilationAirChangesPerHour;

        return Result.Success();
    }
}