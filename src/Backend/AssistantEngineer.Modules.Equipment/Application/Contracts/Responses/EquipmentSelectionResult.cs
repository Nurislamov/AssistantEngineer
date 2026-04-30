namespace AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;

public class EquipmentSelectionResult
{
    public int RoomId { get; set; }

    public double TotalHeatLoadKw { get; set; }
    public double DesignCapacityKw { get; set; }
    public double RequiredCoolingCapacityW { get; set; }
    public double RequiredHeatingCapacityW { get; set; }
    public double CapacityWithReserveW { get; set; }
    public double SafetyFactor { get; set; }

    public string RequestedSystemType { get; set; } = string.Empty;
    public string RequestedUnitType { get; set; } = string.Empty;

    public int SelectedCatalogItemId { get; set; }
    public string SelectedManufacturer { get; set; } = string.Empty;
    public string SelectedModelName { get; set; } = string.Empty;
    public double SelectedNominalCoolingCapacityKw { get; set; }

    public double CapacityReserveKw { get; set; }
    public List<EquipmentSelectionCandidateResult> AcceptedCandidates { get; set; } = new();
    public List<EquipmentSelectionRejectedCandidate> RejectedCandidates { get; set; } = new();
    public List<EquipmentSelectionDiagnostic> Diagnostics { get; set; } = new();
}

public sealed class EquipmentSelectionCandidateResult
{
    public int CatalogItemId { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public double? HeatingCapacityW { get; set; }
    public double? CoolingCapacityW { get; set; }
    public double HeatingMarginW { get; set; }
    public double CoolingMarginW { get; set; }
    public double Score { get; set; }
    public List<string> Notes { get; set; } = new();
}

public sealed class EquipmentSelectionRejectedCandidate
{
    public int CatalogItemId { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public List<string> Reasons { get; set; } = new();
}

public sealed class EquipmentSelectionDiagnostic
{
    public string Severity { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Context { get; set; }
}
