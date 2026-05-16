using AssistantEngineer.Modules.Identity.Domain.Enums;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Identity.Domain.Entities;

public sealed class OrganizationMembership
{
    public int Id { get; private set; }
    public int OrganizationId { get; private set; }
    public int UserId { get; private set; }
    public OrganizationRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    private OrganizationMembership()
    {
    }

    private OrganizationMembership(
        int organizationId,
        int userId,
        OrganizationRole role,
        DateTimeOffset createdAtUtc)
    {
        OrganizationId = organizationId;
        UserId = userId;
        Role = role;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<OrganizationMembership> Create(
        int organizationId,
        int userId,
        OrganizationRole role,
        DateTimeOffset createdAtUtc)
    {
        if (organizationId <= 0)
        {
            return Result<OrganizationMembership>.Validation("OrganizationId must be positive.");
        }

        if (userId <= 0)
        {
            return Result<OrganizationMembership>.Validation("UserId must be positive.");
        }

        if (!Enum.IsDefined(role))
        {
            return Result<OrganizationMembership>.Validation("Organization role is invalid.");
        }

        if (createdAtUtc == default)
        {
            return Result<OrganizationMembership>.Validation("CreatedAtUtc must be specified.");
        }

        return Result<OrganizationMembership>.Success(new OrganizationMembership(
            organizationId,
            userId,
            role,
            createdAtUtc));
    }

    public Result ChangeRole(OrganizationRole role)
    {
        if (!Enum.IsDefined(role))
        {
            return Result.Validation("Organization role is invalid.");
        }

        Role = role;
        return Result.Success();
    }

    public Result Revoke(DateTimeOffset revokedAtUtc)
    {
        if (revokedAtUtc == default)
        {
            return Result.Validation("RevokedAtUtc must be specified.");
        }

        IsActive = false;
        RevokedAtUtc = revokedAtUtc;
        return Result.Success();
    }

    public Result Reactivate()
    {
        if (OrganizationId <= 0 || UserId <= 0)
        {
            return Result.Validation("Membership identifiers are invalid.");
        }

        IsActive = true;
        RevokedAtUtc = null;
        return Result.Success();
    }
}
