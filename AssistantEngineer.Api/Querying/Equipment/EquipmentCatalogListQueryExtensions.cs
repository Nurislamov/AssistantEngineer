using AssistantEngineer.Api.Contracts.Equipment;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Api.Filtering.Equipment;
using AssistantEngineer.Api.Searching.Equipment;
using AssistantEngineer.Api.Sorting.Equipment;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;

namespace AssistantEngineer.Api.Querying.Equipment;

internal static class EquipmentCatalogListQueryExtensions
{
    public static IEnumerable<EquipmentCatalogItemResponse> ApplyEquipmentCatalogListQuery(
        this IEnumerable<EquipmentCatalogItemResponse> source,
        EquipmentCatalogListQueryParameters query) =>
        source
            .ApplyEquipmentCatalogFilters(query)
            .ApplyEquipmentCatalogSearch(query)
            .ApplySort(
                query,
                defaultSortBy: "id",
                EquipmentCatalogSortRules.ByField);
}