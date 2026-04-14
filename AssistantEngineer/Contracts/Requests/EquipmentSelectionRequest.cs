using System.ComponentModel.DataAnnotations;

namespace AssistantEngineer.Contracts.Requests;

public class EquipmentSelectionRequest
{
    [Required]
    [StringLength(50)]
    public string SystemType { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string UnitType { get; set; } = string.Empty;
}