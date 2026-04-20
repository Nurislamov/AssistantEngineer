namespace AssistantEngineer.Domain.Primitives;

public static class Guard
{
    public static Result AgainstNullOrWhiteSpace(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Validation($"{propertyName} is required.");
        return Result.Success();
    }

    public static Result AgainstMaxLength(string value, int maxLength, string propertyName)
    {
        if (value.Length > maxLength)
            return Result.Validation($"{propertyName} is too long.");
        return Result.Success();
    }

    public static Result AgainstMinLength(string value, int minLength, string propertyName)
    {
        if (value.Length < minLength)
            return Result.Validation($"{propertyName} is too short.");
        return Result.Success();
    }

    public static Result AgainstRange(double value, double min, double max, string propertyName)
    {
        var finiteCheck = AgainstNonFinite(value, propertyName);
        if (finiteCheck.IsFailure)
            return finiteCheck;

        if (value < min || value > max)
            return Result.Validation($"{propertyName} must be between {min} and {max}.");
        return Result.Success();
    }

    public static Result AgainstNegative(double value, string propertyName)
    {
        var finiteCheck = AgainstNonFinite(value, propertyName);
        if (finiteCheck.IsFailure)
            return finiteCheck;

        if (value < 0)
            return Result.Validation($"{propertyName} cannot be negative.");
        return Result.Success();
    }

    public static Result AgainstZeroOrNegative(double value, string propertyName)
    {
        var finiteCheck = AgainstNonFinite(value, propertyName);
        if (finiteCheck.IsFailure)
            return finiteCheck;

        if (value <= 0)
            return Result.Validation($"{propertyName} must be positive.");
        return Result.Success();
    }

    public static Result AgainstNonFinite(double value, string propertyName)
    {
        if (!double.IsFinite(value))
            return Result.Validation($"{propertyName} must be a finite number.");
        return Result.Success();
    }
}
