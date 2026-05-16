using System.Text.RegularExpressions;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Identity.Domain.ValueObjects;

public sealed record OrganizationSlug
{
    private static readonly Regex AllowedPattern = new("^[a-z0-9-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Value { get; }

    private OrganizationSlug(string value)
    {
        Value = value;
    }

    public static Result<OrganizationSlug> Create(string? value)
    {
        var normalizedResult = value.ToRequiredTrimmed("Organization slug", maxLength: 128, minLength: 2);
        if (normalizedResult.IsFailure)
        {
            return Result<OrganizationSlug>.Failure(normalizedResult);
        }

        var normalized = normalizedResult.Value.ToLowerInvariant();
        if (normalized.StartsWith("-", StringComparison.Ordinal) || normalized.EndsWith("-", StringComparison.Ordinal))
        {
            return Result<OrganizationSlug>.Validation("Organization slug cannot start or end with hyphen.");
        }

        if (!AllowedPattern.IsMatch(normalized))
        {
            return Result<OrganizationSlug>.Validation("Organization slug can contain only lowercase letters, digits, and hyphen.");
        }

        return Result<OrganizationSlug>.Success(new OrganizationSlug(normalized));
    }

    public override string ToString() => Value;
}
