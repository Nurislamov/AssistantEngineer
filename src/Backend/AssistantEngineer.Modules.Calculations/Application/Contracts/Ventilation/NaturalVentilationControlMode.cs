namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public enum NaturalVentilationControlMode
{
    Unknown = 0,
    AlwaysClosed = 1,
    AlwaysOpen = 2,
    FixedFraction = 3,
    Schedule = 4,
    Occupancy = 5,
    Temperature = 6,
    OccupancyAndTemperature = 7,
    NightVentilation = 8,
    Manual = 9,
    Other = 10,
    TemperatureDriven = 11,
    CoolingAssist = 12,
    NightPurge = 13,
    Custom = 14
}
