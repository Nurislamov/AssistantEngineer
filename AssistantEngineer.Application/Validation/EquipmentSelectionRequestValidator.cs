using AssistantEngineer.Contracts.Requests;
using FluentValidation;

namespace AssistantEngineer.Application.Validation;

public class EquipmentSelectionRequestValidator : AbstractValidator<EquipmentSelectionRequest>
{
    public EquipmentSelectionRequestValidator()
    {
        RuleFor(x => x.SystemType)
            .NotEmpty()
            .MaximumLength(50);
        RuleFor(x => x.UnitType)
            .NotEmpty()
            .MaximumLength(50);
    }
}
