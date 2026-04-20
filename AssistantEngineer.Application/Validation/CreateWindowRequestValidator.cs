using AssistantEngineer.Application.Contracts.Requests;
using FluentValidation;

namespace AssistantEngineer.Application.Validation;

public class CreateWindowRequestValidator : AbstractValidator<CreateWindowRequest>
{
    public CreateWindowRequestValidator()
    {
        RuleFor(x => x.AreaM2).InclusiveBetween(0.1, 100);
        RuleFor(x => x.UValue).InclusiveBetween(0.1, 10);
        RuleFor(x => x.Shgc).InclusiveBetween(0, 1);
        RuleFor(x => x.Orientation).IsInEnum();
        RuleFor(x => x.Shading).NotNull();
        RuleFor(x => x.Shading.OverhangDepthM).InclusiveBetween(0, 20);
        RuleFor(x => x.Shading.SideFinDepthM).InclusiveBetween(0, 20);
        RuleFor(x => x.Shading.RevealDepthM).InclusiveBetween(0, 5);
        RuleFor(x => x.Shading.WindowHeightM).InclusiveBetween(0, 20);
        RuleFor(x => x.Shading.WindowWidthM).InclusiveBetween(0, 50);
        RuleFor(x => x.Shading.MinimumDirectSolarReductionFactor).InclusiveBetween(0, 1);
        RuleFor(x => x.Shading.DiffuseSolarShareUnaffected).InclusiveBetween(0, 1);
    }
}
