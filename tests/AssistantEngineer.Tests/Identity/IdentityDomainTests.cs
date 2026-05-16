using AssistantEngineer.Modules.Identity.Domain.Entities;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using AssistantEngineer.Modules.Identity.Domain.ValueObjects;

namespace AssistantEngineer.Tests.Identity;

public sealed class IdentityDomainTests
{
    [Fact]
    public void UserCreate_ValidatesRequiredFields()
    {
        var missingEmail = User.Create("ext-001", "", "Display Name", DateTimeOffset.UtcNow);
        var missingDisplayName = User.Create("ext-001", "user@example.com", "", DateTimeOffset.UtcNow);
        var missingSubject = User.Create("", "user@example.com", "Display Name", DateTimeOffset.UtcNow);

        Assert.True(missingEmail.IsFailure);
        Assert.True(missingDisplayName.IsFailure);
        Assert.True(missingSubject.IsFailure);
    }

    [Fact]
    public void UserDeactivateAndActivate_WorkAsExpected()
    {
        var created = User.Create("ext-001", "user@example.com", "Display Name", DateTimeOffset.UtcNow);
        Assert.True(created.IsSuccess);

        var user = created.Value;
        Assert.True(user.IsActive);

        user.Deactivate();
        Assert.False(user.IsActive);

        user.Activate();
        Assert.True(user.IsActive);
    }

    [Fact]
    public void UserRecordLogin_SetsLastLogin()
    {
        var created = User.Create("ext-001", "user@example.com", "Display Name", DateTimeOffset.UtcNow);
        var user = created.Value;
        var timestamp = DateTimeOffset.UtcNow;

        var result = user.RecordLogin(timestamp);

        Assert.True(result.IsSuccess);
        Assert.Equal(timestamp, user.LastLoginAtUtc);
    }

    [Fact]
    public void OrganizationCreate_ValidatesNameAndSlug()
    {
        var missingName = Organization.Create("", "valid-slug", DateTimeOffset.UtcNow);
        var invalidSlug = Organization.Create("Org Name", "Invalid Slug", DateTimeOffset.UtcNow);

        Assert.True(missingName.IsFailure);
        Assert.True(invalidSlug.IsFailure);
    }

    [Fact]
    public void OrganizationChangeSlug_ValidatesSlug()
    {
        var created = Organization.Create("Org Name", "org-name", DateTimeOffset.UtcNow);
        var organization = created.Value;

        var invalidResult = organization.ChangeSlug("Org Slug");
        var validResult = organization.ChangeSlug("org-new-slug");

        Assert.True(invalidResult.IsFailure);
        Assert.True(validResult.IsSuccess);
        Assert.Equal("org-new-slug", organization.Slug);
    }

    [Fact]
    public void MembershipCreate_ValidatesIdsAndRole()
    {
        var invalidOrganizationId = OrganizationMembership.Create(0, 1, OrganizationRole.Engineer, DateTimeOffset.UtcNow);
        var invalidUserId = OrganizationMembership.Create(1, 0, OrganizationRole.Engineer, DateTimeOffset.UtcNow);
        var invalidRole = OrganizationMembership.Create(1, 1, (OrganizationRole)999, DateTimeOffset.UtcNow);

        Assert.True(invalidOrganizationId.IsFailure);
        Assert.True(invalidUserId.IsFailure);
        Assert.True(invalidRole.IsFailure);
    }

    [Fact]
    public void MembershipRevoke_MarksInactiveAndSetsRevokedAt()
    {
        var created = OrganizationMembership.Create(11, 7, OrganizationRole.Engineer, DateTimeOffset.UtcNow);
        var membership = created.Value;
        var revokedAt = DateTimeOffset.UtcNow;

        var revokeResult = membership.Revoke(revokedAt);

        Assert.True(revokeResult.IsSuccess);
        Assert.False(membership.IsActive);
        Assert.Equal(revokedAt, membership.RevokedAtUtc);
    }

    [Fact]
    public void MembershipReactivated_ClearsRevokedState()
    {
        var created = OrganizationMembership.Create(11, 7, OrganizationRole.Engineer, DateTimeOffset.UtcNow);
        var membership = created.Value;
        membership.Revoke(DateTimeOffset.UtcNow);

        var reactivateResult = membership.Reactivate();

        Assert.True(reactivateResult.IsSuccess);
        Assert.True(membership.IsActive);
        Assert.Null(membership.RevokedAtUtc);
    }

    [Fact]
    public void EmailAddress_NormalizesAndValidates()
    {
        var valid = EmailAddress.Create("  USER@Example.Com ");
        var invalid = EmailAddress.Create("invalid-email");

        Assert.True(valid.IsSuccess);
        Assert.Equal("user@example.com", valid.Value.Value);
        Assert.True(invalid.IsFailure);
    }

    [Fact]
    public void OrganizationSlug_ValidatesKebabCase()
    {
        var valid = OrganizationSlug.Create("tenant-001");
        var invalidSpace = OrganizationSlug.Create("tenant 001");
        var invalidHyphen = OrganizationSlug.Create("-tenant");

        Assert.True(valid.IsSuccess);
        Assert.Equal("tenant-001", valid.Value.Value);
        Assert.True(invalidSpace.IsFailure);
        Assert.True(invalidHyphen.IsFailure);
    }
}
