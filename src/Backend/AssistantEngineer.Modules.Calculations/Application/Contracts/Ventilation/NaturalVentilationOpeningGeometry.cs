using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationOpeningGeometry(
    string OpeningId,
    string? RoomId,
    string? ZoneId,
    string? SurfaceId,
    NaturalVentilationOpeningType OpeningType,
    double OpeningAreaSquareMeters,
    double? OpeningHeightMeters,
    double? OpeningWidthMeters,
    double? OpeningCenterHeightMeters,
    double? BottomHeightMeters,
    double? TopHeightMeters,
    double? OpeningFraction,
    double? DischargeCoefficient,
    double? WindPressureCoefficient,
    double? OppositeWindPressureCoefficient,
    double? OrientationAzimuthDegrees,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics,
    string? BoundaryId = null,
    string? OpeningName = null,
    double? GrossOpeningAreaSquareMeters = null,
    double? EffectiveOpeningAreaSquareMeters = null,
    double? SillHeightMeters = null,
    bool? IsOperable = null,
    double? MaximumOpeningFraction = null);
