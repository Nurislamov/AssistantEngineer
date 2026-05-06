namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

public sealed record En15316SystemEnergyInput(
    IReadOnlyList<En15316SystemEnergyEndUseInput> EndUses,
    string? DiagnosticsContext = null);
