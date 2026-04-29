using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using FluentValidation;

namespace AssistantEngineer.Modules.Buildings.Application.Validation;

public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200);
    }
}
