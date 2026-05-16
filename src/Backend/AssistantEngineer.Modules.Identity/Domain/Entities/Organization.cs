using AssistantEngineer.Modules.Identity.Domain.ValueObjects;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Identity.Domain.Entities;

public sealed class Organization
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Organization()
    {
    }

    private Organization(
        string name,
        string slug,
        DateTimeOffset createdAtUtc)
    {
        Name = name;
        Slug = slug;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<Organization> Create(
        string? name,
        string? slug,
        DateTimeOffset createdAtUtc)
    {
        if (createdAtUtc == default)
        {
            return Result<Organization>.Validation("CreatedAtUtc must be specified.");
        }

        var nameResult = name.ToRequiredTrimmed("Organization name", maxLength: 200, minLength: 2);
        if (nameResult.IsFailure)
        {
            return Result<Organization>.Failure(nameResult);
        }

        var slugResult = OrganizationSlug.Create(slug);
        if (slugResult.IsFailure)
        {
            return Result<Organization>.Failure(slugResult);
        }

        return Result<Organization>.Success(new Organization(
            nameResult.Value,
            slugResult.Value.Value,
            createdAtUtc));
    }

    public Result Rename(string? name)
    {
        var nameResult = name.ToRequiredTrimmed("Organization name", maxLength: 200, minLength: 2);
        if (nameResult.IsFailure)
        {
            return nameResult;
        }

        Name = nameResult.Value;
        return Result.Success();
    }

    public Result ChangeSlug(string? slug)
    {
        var slugResult = OrganizationSlug.Create(slug);
        if (slugResult.IsFailure)
        {
            return Result.Failure(slugResult.Error, slugResult.ErrorType);
        }

        Slug = slugResult.Value.Value;
        return Result.Success();
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
