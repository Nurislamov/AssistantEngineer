namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

public sealed record MultiZoneCalculationInput(
    string BuildingId,
    IReadOnlyList<ThermalZoneNode> Zones,
    IReadOnlyList<ThermalZoneBoundaryLink> BoundaryLinks,
    IReadOnlyList<InterZoneConductanceLink> InterZoneConductanceLinks,
    IReadOnlyList<InterZoneAirflowLink>? InterZoneAirflowLinks = null,
    IReadOnlyList<MultiZoneHourlyBoundaryCondition>? HourlyBoundaryConditions = null,
    IReadOnlyList<MultiZoneZoneHourlyProfile>? ZoneHourlyProfiles = null,
    IReadOnlyList<string>? ClaimFlags = null);
