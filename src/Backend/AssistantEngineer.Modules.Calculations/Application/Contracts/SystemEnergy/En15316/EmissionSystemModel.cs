namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

public sealed record EmissionSystemModel(
    En15316EmissionModelKind ModelKind = En15316EmissionModelKind.Simplified,
    double? Efficiency = null,
    double? LossFactor = null);
