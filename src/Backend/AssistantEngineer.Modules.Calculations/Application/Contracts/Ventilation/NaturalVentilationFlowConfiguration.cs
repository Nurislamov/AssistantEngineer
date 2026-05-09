namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public enum NaturalVentilationFlowConfiguration
{
    Unknown = 0,
    SingleSided = 1,
    CrossVentilation = 2,
    StackOnly = 3,
    WindOnly = 4,
    CombinedWindAndStack = 5,
    Other = 6,
    ScheduledAirflow = 7,
    CustomAirflow = 8
}
