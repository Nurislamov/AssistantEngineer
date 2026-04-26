using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Domain;

namespace AssistantEngineer.Modules.Equipment.Application.Abstractions;

public interface ICoolingEquipmentSelector
{
    EquipmentSelectionResult? SelectForRoom(
        int roomId,
        string systemType,
        string unitType,
        IEnumerable<CoolingEquipmentCatalogItem> catalog,
        double totalHeatLoadKw,
        double designCapacityKw);
}