using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Tests.Buildings;

public sealed class ProjectTenantOwnershipTests
{
    [Fact]
    public void NewProject_HasNullOwnershipForLegacyCompatibility()
    {
        var project = Project.Create("Legacy compatible project").Value;

        Assert.Null(project.OrganizationId);
        Assert.Null(project.OwnerUserId);
        Assert.False(project.IsTenantScoped);
    }

    [Fact]
    public void AssignOrganization_AcceptsPositiveId()
    {
        var project = Project.Create("Tenant owned project").Value;

        var result = project.AssignOrganization(1001);

        Assert.True(result.IsSuccess);
        Assert.Equal(1001, project.OrganizationId);
        Assert.True(project.IsTenantScoped);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AssignOrganization_RejectsZeroOrNegative(int organizationId)
    {
        var project = Project.Create("Invalid tenant project").Value;

        var result = project.AssignOrganization(organizationId);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Null(project.OrganizationId);
    }

    [Fact]
    public void AssignOwnerUser_AcceptsPositiveId()
    {
        var project = Project.Create("Owner assigned project").Value;

        var result = project.AssignOwnerUser(2001);

        Assert.True(result.IsSuccess);
        Assert.Equal(2001, project.OwnerUserId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AssignOwnerUser_RejectsZeroOrNegative(int ownerUserId)
    {
        var project = Project.Create("Invalid owner project").Value;

        var result = project.AssignOwnerUser(ownerUserId);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Null(project.OwnerUserId);
    }
}
