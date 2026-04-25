using AssistantEngineer.Api.Contracts.Common;

namespace AssistantEngineer.Api.Contracts.Equipment;

public sealed class EquipmentCatalogListQueryParameters : CollectionQueryParameters
{
    public string? Manufacturer { get; init; }
    public string? SystemType { get; init; }
    public string? UnitType { get; init; }
    public bool? IsActive { get; init; }
}
