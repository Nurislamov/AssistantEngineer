using AssistantEngineer.Contracts.Requests;
using FluentValidation;

namespace AssistantEngineer.Application.Validation;

public class CreateRoomRequestValidator : AbstractValidator<CreateRoomRequest>
{
    public CreateRoomRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);
        RuleFor(x => x.AreaM2).InclusiveBetween(1, 10_000);
        RuleFor(x => x.HeightM).InclusiveBetween(1, 20);
        RuleFor(x => x.IndoorTemperatureC).InclusiveBetween(-50, 100);
        RuleFor(x => x.OutdoorTemperatureC).InclusiveBetween(-60, 100);
        RuleFor(x => x.PeopleCount).InclusiveBetween(0, 1_000);
        RuleFor(x => x.EquipmentLoadW).InclusiveBetween(0, 1_000_000);
        RuleFor(x => x.LightingLoadW).InclusiveBetween(0, 1_000_000);
        RuleFor(x => x.FloorId).GreaterThan(0);
    }
}
