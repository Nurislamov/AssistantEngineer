namespace AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;

public sealed record AnnualEnergyBalanceInput(
    int BuildingId,
    string? BuildingName,
    double BuildingAreaM2,
    int Year,
    IReadOnlyList<AnnualEnergyBalanceHourInput> Hours,
    bool UsesSyntheticWeather = false,
    string? WeatherSource = null,
    string? DiagnosticsContext = null);
