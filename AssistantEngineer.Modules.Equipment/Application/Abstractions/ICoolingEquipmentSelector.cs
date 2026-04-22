using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Domain;

namespace AssistantEngineer.Modules.Equipment.Application.Abstractions;

public interface ICoolingEquipmentSelector
{
    Task<EquipmentSelectionResult?> SelectForRoomAsync(
        Room room,
        string systemType,
        string unitType,
        IEnumerable<CoolingEquipmentCatalogItem> catalog,
        CoolingLoadCalculationMethod method = CoolingLoadCalculationMethod.Simplified,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);
}
