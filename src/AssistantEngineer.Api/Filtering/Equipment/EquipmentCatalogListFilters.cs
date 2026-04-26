using AssistantEngineer.Api.Contracts.Equipment;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;

namespace AssistantEngineer.Api.Filtering.Equipment;

internal static class EquipmentCatalogListFilters
{
    public static IEnumerable<EquipmentCatalogItemResponse> ApplyEquipmentCatalogFilters(
        this IEnumerable<EquipmentCatalogItemResponse> source,
        EquipmentCatalogListQueryParameters query) =>
        source
            .ApplyStringEqualsFilter(
                query.Manufacturer,
                item => item.Manufacturer)
            .ApplyStringEqualsFilter(
                query.SystemType,
                item => item.SystemType)
            .ApplyStringEqualsFilter(
                query.UnitType,
                item => item.UnitType)
            .ApplyValueFilter<EquipmentCatalogItemResponse, bool>(
                query.IsActive,
                item => item.IsActive);
}