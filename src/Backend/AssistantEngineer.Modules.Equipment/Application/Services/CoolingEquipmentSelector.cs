using AssistantEngineer.Modules.Equipment.Application.Abstractions;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Domain;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Equipment.Application.Services;

public sealed class CoolingEquipmentSelector : ICoolingEquipmentSelector
{
    public Result<EquipmentSelectionResult> SelectForRoom(
        int roomId,
        string systemType,
        string unitType,
        IEnumerable<CoolingEquipmentCatalogItem> catalog,
        double totalHeatLoadKw,
        double designCapacityKw)
    {
        if (roomId <= 0)
            return Result<EquipmentSelectionResult>.Validation("Room id must be greater than zero.");

        if (string.IsNullOrWhiteSpace(systemType))
            return Result<EquipmentSelectionResult>.Validation("System type is required.");

        if (string.IsNullOrWhiteSpace(unitType))
            return Result<EquipmentSelectionResult>.Validation("Unit type is required.");

        if (catalog is null)
            return Result<EquipmentSelectionResult>.Validation("Equipment catalog is required.");

        if (totalHeatLoadKw < 0)
            return Result<EquipmentSelectionResult>.Validation("Cooling load cannot be negative.");

        if (designCapacityKw <= 0)
            return Result<EquipmentSelectionResult>.Validation("Design capacity must be greater than zero.");

        var catalogItems = catalog.ToArray();

        if (catalogItems.Length == 0)
            return Result<EquipmentSelectionResult>.NotFound("Equipment catalog contains no items.");

        var accepted = catalogItems
            .Where(item =>
                item.IsActive &&
                item.SystemType == systemType &&
                item.UnitType == unitType &&
                item.NominalCoolingCapacity.Kilowatts >= designCapacityKw)
            .OrderBy(item => item.NominalCoolingCapacity.Kilowatts)
            .ToArray();

        var suitable = accepted.FirstOrDefault();

        if (suitable is null)
            return Result<EquipmentSelectionResult>.NotFound("No suitable equipment found.");

        var coolingLoadW = totalHeatLoadKw * 1000.0;
        var designCapacityW = designCapacityKw * 1000.0;
        var coolingSafetyFactor = totalHeatLoadKw > 0
            ? Math.Round(designCapacityKw / totalHeatLoadKw, 6, MidpointRounding.AwayFromZero)
            : 1.0;

        return Result<EquipmentSelectionResult>.Success(new EquipmentSelectionResult
        {
            RoomId = roomId,
            EquipmentSelected = true,
            CalculationMethod = "MatrixCoolingEquipmentSelectorAdapter",
            CoolingLoadKw = totalHeatLoadKw,
            DesignCapacityKw = designCapacityKw,
            RequiredCoolingCapacityW = coolingLoadW,
            RequiredHeatingCapacityW = 0,
            CapacityWithReserveW = designCapacityW,
            SafetyFactor = coolingSafetyFactor,
            HeatingSafetyFactor = 1.0,
            CoolingSafetyFactor = coolingSafetyFactor,
            RequestedSystemType = systemType,
            RequestedUnitType = unitType,
            SelectedCatalogItemId = suitable.Id,
            SelectedManufacturer = suitable.Manufacturer,
            SelectedModelName = suitable.ModelName,
            SelectedNominalCoolingCapacityKw = suitable.NominalCoolingCapacity.Kilowatts,
            CapacityReserveKw = Math.Round(
                suitable.NominalCoolingCapacity.Kilowatts - designCapacityKw,
                2,
                MidpointRounding.AwayFromZero),
            AcceptedCandidates = accepted
                .Select(item => new EquipmentSelectionCandidateResult
                {
                    CatalogItemId = item.Id,
                    Manufacturer = item.Manufacturer,
                    ModelName = item.ModelName,
                    HeatingCapacityW = null,
                    CoolingCapacityW = item.NominalCoolingCapacity.Watts,
                    HeatingMarginW = 0,
                    CoolingMarginW = Math.Round(
                        item.NominalCoolingCapacity.Watts - designCapacityW,
                        2,
                        MidpointRounding.AwayFromZero),
                    Score = Math.Round(
                        item.NominalCoolingCapacity.Kilowatts - designCapacityKw,
                        2,
                        MidpointRounding.AwayFromZero),
                    Notes =
                    [
                        "Matrix cooling selector ranks active matching candidates by nearest cooling capacity."
                    ]
                })
                .ToList(),
            RejectedCandidates = catalogItems
                .Except(accepted)
                .Select(item => new EquipmentSelectionRejectedCandidate
                {
                    CatalogItemId = item.Id,
                    Manufacturer = item.Manufacturer,
                    ModelName = item.ModelName,
                    Reasons = GetRejectionReasons(item, systemType, unitType, designCapacityKw)
                })
                .Where(item => item.Reasons.Count > 0)
                .ToList(),
            Diagnostics =
            [
                new EquipmentSelectionDiagnostic
                {
                    Severity = "Info",
                    Code = "EquipmentSelection.MatrixCoolingSelectorAdapter",
                    Message = "Matrix cooling selector uses cooling catalog capacity only; the Standard-Based Calculation equipment sizing pipeline provides multi-criteria heating/cooling diagnostics."
                },
                new EquipmentSelectionDiagnostic
                {
                    Severity = "Warning",
                    Code = "EquipmentSelection.HeatingCapacityNotEvaluated",
                    Message = "Required heating capacity is not evaluated by this legacy cooling selector adapter."
                }
            ]
        });
    }

    private static List<string> GetRejectionReasons(
        CoolingEquipmentCatalogItem item,
        string systemType,
        string unitType,
        double designCapacityKw)
    {
        var reasons = new List<string>();

        if (!item.IsActive)
            reasons.Add("Catalog item is inactive.");

        if (item.SystemType != systemType)
            reasons.Add("System type does not match the request.");

        if (item.UnitType != unitType)
            reasons.Add("Unit type does not match the request.");

        if (item.NominalCoolingCapacity.Kilowatts < designCapacityKw)
            reasons.Add("Nominal cooling capacity is below required design capacity.");

        return reasons;
    }
}
