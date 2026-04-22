using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using FluentValidation;

namespace AssistantEngineer.Modules.Buildings.Application.Validation;

public class CreateRoomRequestValidator : AbstractValidator<CreateRoomRequest>
{
    public CreateRoomRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);
        RuleFor(x => x.AreaM2).InclusiveBetween(1, 10000);
        RuleFor(x => x.HeightM).InclusiveBetween(1, 20);
        RuleFor(x => x.IndoorTemperatureC).InclusiveBetween(-50, 100);
        RuleFor(x => x.OutdoorTemperatureOverrideC)
            .InclusiveBetween(-60, 100)
            .When(x => x.OutdoorTemperatureOverrideC.HasValue);
        RuleFor(x => x.PeopleCount).InclusiveBetween(0, 1000);
        RuleFor(x => x.EquipmentLoadW).InclusiveBetween(0, 1000000);
        RuleFor(x => x.LightingLoadW).InclusiveBetween(0, 1000000);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.FloorId).GreaterThan(0);
    }
}
