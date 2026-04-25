using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using FluentValidation;

namespace AssistantEngineer.Modules.Buildings.Application.Validation;

public sealed class CreateThermalZoneRequestValidator : AbstractValidator<CreateThermalZoneRequest>
{
    public CreateThermalZoneRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.RoomIds)
            .NotNull();

        RuleFor(x => x.RoomIds)
            .Must(roomIds => roomIds.Count > 0)
            .WithMessage("At least one room must be assigned to a thermal zone.");

        RuleFor(x => x.RoomIds)
            .Must(roomIds => roomIds.Distinct().Count() == roomIds.Count)
            .WithMessage("RoomIds must be unique.");
    }
}