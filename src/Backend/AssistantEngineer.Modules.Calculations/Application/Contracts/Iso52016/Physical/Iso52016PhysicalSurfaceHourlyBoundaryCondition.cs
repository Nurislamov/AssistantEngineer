namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;

/// <summary>
/// Hourly driving boundary temperature override for one physical surface.
/// This is an internal engineering anchor contract, not an external parity fixture.
/// </summary>
public sealed record Iso52016PhysicalSurfaceHourlyBoundaryCondition(
    string SurfaceId,
    int HourOfYear,
    double BoundaryTemperatureC);
