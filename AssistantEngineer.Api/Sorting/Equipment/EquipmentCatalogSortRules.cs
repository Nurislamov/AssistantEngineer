using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;

namespace AssistantEngineer.Api.Sorting.Equipment;

internal static class EquipmentCatalogSortRules
{
    public static readonly IReadOnlyDictionary<string, SortRule<EquipmentCatalogItemResponse>> ByField =
        new Dictionary<string, SortRule<EquipmentCatalogItemResponse>>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = (items, descending) =>
                items.SortBy(descending, item => item.Id),

            ["manufacturer"] = (items, descending) =>
                items.SortBy(descending, item => item.Manufacturer)
                    .ThenByStable(descending, item => item.Id),

            ["systemtype"] = (items, descending) =>
                items.SortBy(descending, item => item.SystemType)
                    .ThenByStable(descending, item => item.Id),

            ["unittype"] = (items, descending) =>
                items.SortBy(descending, item => item.UnitType)
                    .ThenByStable(descending, item => item.Id),

            ["modelname"] = (items, descending) =>
                items.SortBy(descending, item => item.ModelName)
                    .ThenByStable(descending, item => item.Id),

            ["nominalcoolingcapacitykw"] = (items, descending) =>
                items.SortBy(descending, item => item.NominalCoolingCapacityKw)
                    .ThenByStable(descending, item => item.Id),

            ["isactive"] = (items, descending) =>
                items.SortBy(descending, item => item.IsActive)
                    .ThenByStable(descending, item => item.Id)
        };
}