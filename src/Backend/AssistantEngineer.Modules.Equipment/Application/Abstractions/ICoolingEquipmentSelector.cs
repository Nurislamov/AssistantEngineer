using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Domain;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Equipment.Application.Abstractions;

public interface ICoolingEquipmentSelector
{
    Result<EquipmentSelectionResult> SelectForRoom(
        int roomId,
        string systemType,
        string unitType,
        IEnumerable<CoolingEquipmentCatalogItem> catalog,
        double totalHeatLoadKw,
        double designCapacityKw);
}
