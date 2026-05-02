using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using FluentValidation;

namespace AssistantEngineer.Modules.Buildings.Application.Validation;

public sealed class ImportPvgisWeatherRequestValidator : AbstractValidator<ImportPvgisWeatherRequest>
{
    public ImportPvgisWeatherRequestValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(1900, 2100);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180);

        RuleFor(x => x)
            .Must(x => !x.StartYear.HasValue || !x.EndYear.HasValue || x.StartYear.Value <= x.EndYear.Value)
            .WithMessage("StartYear cannot be greater than EndYear.");
    }
}