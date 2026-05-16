using AssistantEngineer.Modules.Identity.Domain.ValueObjects;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Identity.Domain.Entities;

public sealed class User
{
    public int Id { get; private set; }
    public string ExternalSubjectId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    private User()
    {
    }

    private User(
        string externalSubjectId,
        string email,
        string displayName,
        DateTimeOffset createdAtUtc)
    {
        ExternalSubjectId = externalSubjectId;
        Email = email;
        DisplayName = displayName;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<User> Create(
        string? externalSubjectId,
        string? email,
        string? displayName,
        DateTimeOffset createdAtUtc)
    {
        if (createdAtUtc == default)
        {
            return Result<User>.Validation("CreatedAtUtc must be specified.");
        }

        var externalSubjectResult = externalSubjectId.ToRequiredTrimmed("External subject id", maxLength: 200, minLength: 2);
        if (externalSubjectResult.IsFailure)
        {
            return Result<User>.Failure(externalSubjectResult);
        }

        var emailResult = EmailAddress.Create(email);
        if (emailResult.IsFailure)
        {
            return Result<User>.Failure(emailResult);
        }

        var displayNameResult = displayName.ToRequiredTrimmed("Display name", maxLength: 200, minLength: 2);
        if (displayNameResult.IsFailure)
        {
            return Result<User>.Failure(displayNameResult);
        }

        return Result<User>.Success(new User(
            externalSubjectResult.Value,
            emailResult.Value.Value,
            displayNameResult.Value,
            createdAtUtc));
    }

    public Result UpdateDisplayName(string? displayName)
    {
        var displayNameResult = displayName.ToRequiredTrimmed("Display name", maxLength: 200, minLength: 2);
        if (displayNameResult.IsFailure)
        {
            return displayNameResult;
        }

        DisplayName = displayNameResult.Value;
        return Result.Success();
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public Result RecordLogin(DateTimeOffset timestampUtc)
    {
        if (timestampUtc == default)
        {
            return Result.Validation("Login timestamp must be specified.");
        }

        LastLoginAtUtc = timestampUtc;
        return Result.Success();
    }
}
