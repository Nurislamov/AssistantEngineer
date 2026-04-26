using System.Collections.Concurrent;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AssistantEngineer.Api.Filters.Validation;

internal static class FluentValidationActionArgumentValidator
{
    private static readonly ConcurrentDictionary<Type, ValidationArgumentMetadata> MetadataByArgumentType = new();

    public static async Task ValidateAsync(
        ActionExecutingContext context,
        CancellationToken cancellationToken)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var metadata = GetMetadata(argument.GetType());
            var validator = ResolveValidator(context, metadata);

            if (validator is null)
                continue;

            var result = await ValidateAsync(
                validator,
                metadata,
                argument,
                cancellationToken);

            AddValidationErrors(
                context,
                result);
        }
    }

    private static ValidationArgumentMetadata GetMetadata(
        Type argumentType) =>
        MetadataByArgumentType.GetOrAdd(
            argumentType,
            static type => new ValidationArgumentMetadata(
                typeof(IValidator<>).MakeGenericType(type),
                typeof(ValidationContext<>).MakeGenericType(type)));

    private static IValidator? ResolveValidator(
        ActionContext context,
        ValidationArgumentMetadata metadata) =>
        context.HttpContext.RequestServices.GetService(metadata.ValidatorType) as IValidator;

    private static Task<ValidationResult> ValidateAsync(
        IValidator validator,
        ValidationArgumentMetadata metadata,
        object argument,
        CancellationToken cancellationToken)
    {
        var validationContext = (IValidationContext)Activator.CreateInstance(
            metadata.ValidationContextType,
            argument)!;

        return validator.ValidateAsync(
            validationContext,
            cancellationToken);
    }

    private static void AddValidationErrors(
        ActionExecutingContext context,
        ValidationResult result)
    {
        foreach (var error in result.Errors)
        {
            context.ModelState.AddModelError(
                error.PropertyName,
                error.ErrorMessage);
        }
    }
}