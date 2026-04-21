namespace AssistantEngineer.Modules.Calculations.Application.Models.Profiles;

public sealed record AnnualProfile(
    string Name,
    int Year,
    IReadOnlyList<double> Values);