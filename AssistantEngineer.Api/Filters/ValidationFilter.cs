using System.Collections.Concurrent;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AssistantEngineer.Api.Filters;

public class ValidationFilter : IAsyncActionFilter
{
    private static readonly ConcurrentDictionary<Type, ValidationArgumentMetadata> MetadataByArgumentType = new();

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var (argumentName, argument) in context.ActionArguments.Where(argument => argument.Value is not null))
        {
            AddEnumModelErrorIfNeeded(context, argumentName, argument!);
        }

        if (!context.ModelState.IsValid)
        {
            context.Result = CreateValidationProblem(context);
            return;
        }

        foreach (var argument in context.ActionArguments.Values.Where(value => value is not null))
        {
            var metadata = GetMetadata(argument!.GetType());
            var validator = ResolveValidator(context, metadata);
            if (validator is null)
                continue;

            var result = await ValidateAsync(validator, metadata, argument!, context.HttpContext.RequestAborted);
            foreach (var error in result.Errors)
            {
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid)
        {
            context.Result = CreateValidationProblem(context);
            return;
        }

        await next();
    }

    private static void AddEnumModelErrorIfNeeded(ActionExecutingContext context, string argumentName, object argument)
    {
        var argumentType = Nullable.GetUnderlyingType(argument.GetType()) ?? argument.GetType();
        if (!argumentType.IsEnum || Enum.IsDefined(argumentType, argument))
            return;

        context.ModelState.AddModelError(argumentName, $"The value '{argument}' is not valid for {argumentName}.");
    }

    private static ValidationArgumentMetadata GetMetadata(Type argumentType) =>
        MetadataByArgumentType.GetOrAdd(argumentType, static type => new ValidationArgumentMetadata(
            typeof(IValidator<>).MakeGenericType(type),
            typeof(ValidationContext<>).MakeGenericType(type)));

    private static IValidator? ResolveValidator(ActionContext context, ValidationArgumentMetadata metadata) =>
        context.HttpContext.RequestServices.GetService(metadata.ValidatorType) as IValidator;

    private static Task<ValidationResult> ValidateAsync(
        IValidator validator,
        ValidationArgumentMetadata metadata,
        object argument,
        CancellationToken cancellationToken)
    {
        var validationContext = (IValidationContext)Activator.CreateInstance(metadata.ValidationContextType, argument)!;
        return validator.ValidateAsync(validationContext, cancellationToken);
    }

    private static BadRequestObjectResult CreateValidationProblem(ActionContext context) =>
        new(new ValidationProblemDetails(context.ModelState)
        {
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest
        });

    private sealed record ValidationArgumentMetadata(Type ValidatorType, Type ValidationContextType);
}
