namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;

public sealed record NaturalVentilationOccupancyControl(
    bool Enabled = false,
    double OccupancyFraction = 1.0,
    double MinimumOccupancyFractionToEnable = 0.0,
    bool DisableWhenUnoccupied = true);
