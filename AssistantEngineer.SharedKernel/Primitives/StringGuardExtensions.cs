namespace AssistantEngineer.SharedKernel.Primitives;

public static class StringGuardExtensions
{
    public static Result<string> ToRequiredTrimmed(
        this string? value,
        string propertyName,
        int? maxLength = null,
        int? minLength = null)
    {
        var requiredCheck = Guard.AgainstNullOrWhiteSpace(value ?? string.Empty, propertyName);
        if (requiredCheck.IsFailure)
            return Result<string>.Failure(requiredCheck);

        var trimmed = value!.Trim();

        if (minLength.HasValue)
        {
            var minLengthCheck = Guard.AgainstMinLength(trimmed, minLength.Value, propertyName);
            if (minLengthCheck.IsFailure)
                return Result<string>.Failure(minLengthCheck);
        }

        if (maxLength.HasValue)
        {
            var maxLengthCheck = Guard.AgainstMaxLength(trimmed, maxLength.Value, propertyName);
            if (maxLengthCheck.IsFailure)
                return Result<string>.Failure(maxLengthCheck);
        }

        return Result<string>.Success(trimmed);
    }
}
