using AssistantEngineer.Api.Options.Security;

namespace AssistantEngineer.Tests.Api;

public sealed class ApiRateLimitingOptionsTests
{
    [Fact]
    public void Defaults_PreserveCompatibilityAndExpectedLimits()
    {
        var options = new ApiRateLimitingOptions();

        Assert.False(options.Enabled);
        Assert.True(options.AllowRelaxedLimitsInDevelopment);
        Assert.True(options.UseAuthenticatedPrincipalPartitioning);
        Assert.True(options.UseOrganizationPartitioning);
        Assert.True(options.UseApiKeyFingerprintPartitioning);
        Assert.Equal("AssistantEngineerDefault", options.DefaultPolicyName);
        Assert.Equal(120, options.AnonymousPublicReadLimitPerMinute);
        Assert.Equal(10, options.AnonymousCalculationRunLimitPerMinute);
        Assert.Equal(60, options.AuthenticatedCalculationRunLimitPerMinute);
        Assert.Equal(300, options.OrganizationCalculationRunLimitPerMinute);
        Assert.Equal(30, options.WorkflowExecuteLimitPerMinute);
        Assert.Equal(20, options.ReportGenerateLimitPerMinute);
    }
}
