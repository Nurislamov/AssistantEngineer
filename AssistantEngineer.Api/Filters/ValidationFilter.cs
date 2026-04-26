using AssistantEngineer.Api.Filters.Validation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AssistantEngineer.Api.Filters;

internal sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        ActionArgumentEnumValidator.AddEnumModelErrors(context);

        if (!context.ModelState.IsValid)
        {
            context.Result = ValidationProblemResultFactory.Create(context);
            return;
        }

        await FluentValidationActionArgumentValidator.ValidateAsync(
            context,
            context.HttpContext.RequestAborted);

        if (!context.ModelState.IsValid)
        {
            context.Result = ValidationProblemResultFactory.Create(context);
            return;
        }

        await next();
    }
}