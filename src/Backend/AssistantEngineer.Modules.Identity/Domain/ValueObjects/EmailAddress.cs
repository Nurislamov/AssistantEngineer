using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Identity.Domain.ValueObjects;

public sealed record EmailAddress
{
    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static Result<EmailAddress> Create(string? value)
    {
        var normalizedResult = value.ToRequiredTrimmed("Email", maxLength: 320, minLength: 3);
        if (normalizedResult.IsFailure)
        {
            return Result<EmailAddress>.Failure(normalizedResult);
        }

        var normalized = normalizedResult.Value.ToLowerInvariant();
        if (!normalized.Contains('@', StringComparison.Ordinal))
        {
            return Result<EmailAddress>.Validation("Email must contain '@'.");
        }

        var atIndex = normalized.IndexOf('@');
        if (atIndex <= 0 || atIndex >= normalized.Length - 1)
        {
            return Result<EmailAddress>.Validation("Email format is invalid.");
        }

        return Result<EmailAddress>.Success(new EmailAddress(normalized));
    }

    public override string ToString() => Value;
}
