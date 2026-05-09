namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

public sealed record InterZoneConductanceLink(
    string LinkId,
    string FromZoneId,
    string ToZoneId,
    double ConductanceWPerK,
    double AreaSquareMeters = 0.0,
    string? FromBoundaryId = null,
    string? ToBoundaryId = null);
