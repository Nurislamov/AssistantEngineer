using AssistantEngineer.Contracts.Requests;
using FluentValidation;

namespace AssistantEngineer.Application.Validation;

public class CreateEquipmentCatalogItemRequestValidator : AbstractValidator<CreateEquipmentCatalogItemRequest>
{
    public CreateEquipmentCatalogItemRequestValidator()
    {
        RuleFor(x => x.Manufacturer)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(x => x.SystemType)
            .NotEmpty()
            .MaximumLength(50);
        RuleFor(x => x.UnitType)
            .NotEmpty()
            .MaximumLength(50);
        RuleFor(x => x.ModelName)
            .NotEmpty()
            .MaximumLength(150);
        RuleFor(x => x.NominalCoolingCapacityKw).InclusiveBetween(0.1, 1_000);
    }
}
