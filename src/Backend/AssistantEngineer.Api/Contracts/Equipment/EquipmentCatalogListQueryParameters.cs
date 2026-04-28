using AssistantEngineer.Api.Contracts.Common;

namespace AssistantEngineer.Api.Contracts.Equipment;

public sealed class EquipmentCatalogListQueryParameters : CollectionQueryParameters
{
    public string? Manufacturer { get; set; }

    public string? SystemType { get; set; }

    public string? UnitType { get; set; }

    public bool? IsActive { get; set; }
}