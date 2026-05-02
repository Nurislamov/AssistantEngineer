using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using FluentValidation;

namespace AssistantEngineer.Modules.Buildings.Application.Validation;

public class CreateWallRequestValidator : AbstractValidator<CreateWallRequest>
{
    public CreateWallRequestValidator()
    {
        RuleFor(x => x.AreaM2).InclusiveBetween(0.1, 1000);
        RuleFor(x => x.UValue).InclusiveBetween(0.1, 10);
        RuleFor(x => x.Orientation).IsInEnum();
        RuleFor(x => x.BoundaryType).IsInEnum();

        RuleFor(x => x.AdjacentRoomId)
            .NotNull()
            .When(x => x.BoundaryType is WallBoundaryTypeDto.AdjacentConditioned or WallBoundaryTypeDto.AdjacentUnconditioned)
            .WithMessage("AdjacentRoomId is required for adjacent wall boundary types.");

        RuleFor(x => x.AdjacentRoomId)
            .Null()
            .When(x => x.BoundaryType is not (WallBoundaryTypeDto.AdjacentConditioned or WallBoundaryTypeDto.AdjacentUnconditioned))
            .WithMessage("AdjacentRoomId can be provided only for adjacent wall boundary types.");
    }
}