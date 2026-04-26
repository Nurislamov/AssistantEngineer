using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using FluentValidation;

namespace AssistantEngineer.Modules.Equipment.Application.Validation;

public class EquipmentSelectionRequestValidator : AbstractValidator<EquipmentSelectionRequest>
{
    public EquipmentSelectionRequestValidator()
    {
        RuleFor(x => x.SystemType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.UnitType).NotEmpty().MaximumLength(50);
    }
}