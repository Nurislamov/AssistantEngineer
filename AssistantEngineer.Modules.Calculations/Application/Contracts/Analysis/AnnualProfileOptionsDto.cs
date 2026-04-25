namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Analysis;

public sealed class AnnualProfileOptionsDto
{
    public bool UseAnnualProfiles { get; set; } = false;
    public int Year { get; set; } = DateTime.UtcNow.Year;
    public string CountryCode { get; set; } = "UZ";
}