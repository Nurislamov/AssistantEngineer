using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;

namespace AssistantEngineer.Api.Searching.Equipment;

internal static class EquipmentCatalogSearchExtensions
{
    public static IEnumerable<EquipmentCatalogItemResponse> ApplyEquipmentCatalogSearch(
        this IEnumerable<EquipmentCatalogItemResponse> source,
        CollectionQueryParameters query) =>
        source.ApplySearch(
            query.Search,
            item => item.Manufacturer,
            item => item.SystemType,
            item => item.UnitType,
            item => item.ModelName);
}