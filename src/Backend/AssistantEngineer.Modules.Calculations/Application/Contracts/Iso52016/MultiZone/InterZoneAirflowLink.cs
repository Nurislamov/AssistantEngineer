namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

public sealed record InterZoneAirflowLink(
    string LinkId,
    string FromZoneId,
    string ToZoneId,
    double AirflowKilogramsPerSecond);
