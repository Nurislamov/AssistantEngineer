using System.ComponentModel.DataAnnotations;

namespace AssistantEngineer.Contracts.Requests;

public class CreateEquipmentCatalogItemRequest
{
    [Required]
    [StringLength(100)]
    public string Manufacturer { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string SystemType { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string UnitType { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string ModelName { get; set; } = string.Empty;

    [Range(0.1, 1000)]
    public double NominalCoolingCapacityKw { get; set; }

    public bool IsActive { get; set; } = true;
}