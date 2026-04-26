using Microsoft.AspNetCore.Mvc.Filters;

namespace AssistantEngineer.Api.Filters.Validation;

internal static class ActionArgumentEnumValidator
{
    public static void AddEnumModelErrors(
        ActionExecutingContext context)
    {
        foreach (var (argumentName, argument) in context.ActionArguments)
        {
            if (argument is null)
                continue;

            AddEnumModelErrorIfNeeded(
                context,
                argumentName,
                argument);
        }
    }

    private static void AddEnumModelErrorIfNeeded(
        ActionExecutingContext context,
        string argumentName,
        object argument)
    {
        var argumentType = Nullable.GetUnderlyingType(argument.GetType()) ?? argument.GetType();

        if (!argumentType.IsEnum)
            return;

        if (Enum.IsDefined(argumentType, argument))
            return;

        context.ModelState.AddModelError(
            argumentName,
            $"The value '{argument}' is not valid for {argumentName}.");
    }
}