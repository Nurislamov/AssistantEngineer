using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016HourlyWindowSolarGainRecord(
    int HourOfYear,
    int Month,
    int Day,
    int Hour,
    CardinalDirection Orientation,
    string SurfaceCode,
    double SolarGainW);