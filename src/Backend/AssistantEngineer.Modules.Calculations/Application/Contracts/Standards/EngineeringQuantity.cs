namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

public sealed record EngineeringQuantity(
    double Value,
    EngineeringUnit Unit,
    string? Name = null,
    string? Source = null);
