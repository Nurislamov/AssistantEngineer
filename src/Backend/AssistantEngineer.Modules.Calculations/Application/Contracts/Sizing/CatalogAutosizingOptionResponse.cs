namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class CatalogAutosizingOptionResponse
{
    public int CatalogItemId { get; set; }

    public string Manufacturer { get; set; } = string.Empty;
    public string SystemType { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;

    public double RequiredCapacityKw { get; set; }

    public double UnitNominalCapacityKw { get; set; }
    public int UnitCount { get; set; }

    public double TotalNominalCapacityKw { get; set; }
    public double CoverageRatio { get; set; }
    public double OversizeRatio { get; set; }
    public double CapacityReserveKw { get; set; }

    public double Score { get; set; }
    public int Rank { get; set; }

    public List<string> RankingNotes { get; set; } = new();
}