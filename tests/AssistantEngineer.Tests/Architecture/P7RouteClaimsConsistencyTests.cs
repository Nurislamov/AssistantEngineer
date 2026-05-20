using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7RouteClaimsConsistencyTests
{
    [Fact]
    public void InventoryDoesNotContainForbiddenPositiveClaims()
    {
        var files = new[]
        {
            GovernancePathHelper.SecurityDocPath("api-endpoint-protection-inventory.md"),
            GovernancePathHelper.SecurityDocPath("api-endpoint-protection-inventory.json"),
            GovernancePathHelper.SecurityDocPath("authorization-policy-rollout.md")
        };

        var forbiddenPhrases = new[]
        {
            "full tenant isolation complete",
            "production security certification complete",
            "database row-level security enabled",
            "global ef query filters enabled",
            "ownership backfill executed",
            "production apply enabled"
        };

        GovernanceAssertions.AssertNoFalseSecurityClaims(files, forbiddenPhrases);
    }

    [Fact]
    public void DeferredEndpointGroupsMustCarryKnownLimitations()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var endpoints = document.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();
        Assert.NotEmpty(endpoints);

        foreach (var endpoint in endpoints)
        {
            var group = endpoint.GetProperty("endpointGroup").GetString() ?? string.Empty;
            if (!group.Contains("Deferred", StringComparison.Ordinal))
                continue;

            var knownLimitations = endpoint.GetProperty("knownLimitations").EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();
            Assert.NotEmpty(knownLimitations);
        }
    }

    [Fact]
    public void ArtifactWriteDeferredRemainsDeferredUnlessRealEndpointsExist()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var endpoints = document.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();
        var artifactWriteDeferred = endpoints
            .Where(endpoint => string.Equals(endpoint.GetProperty("endpointGroup").GetString(), "ArtifactWriteDeferred", StringComparison.Ordinal))
            .ToArray();

        if (artifactWriteDeferred.Length == 0)
        {
            var authRollout = File.ReadAllText(AuthorizationRolloutPath);
            Assert.Contains("artifact write/delete endpoints are not exposed and remain deferred", authRollout, StringComparison.OrdinalIgnoreCase);
            return;
        }

        foreach (var endpoint in artifactWriteDeferred)
        {
            Assert.Equal("Deferred", endpoint.GetProperty("protectionStage").GetString());
            Assert.Contains(
                endpoint.GetProperty("knownLimitations").EnumerateArray().Select(item => item.GetString() ?? string.Empty),
                item => item.Contains("deferred", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void StagedProtectedRoutesDoNotClaimAlwaysOnAuthorization()
    {
        var content = File.ReadAllText(AuthorizationRolloutPath);
        Assert.DoesNotContain("always-on authorization for all routes is enabled", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("staged", content, StringComparison.OrdinalIgnoreCase);
    }

    private static string InventoryJsonPath =>
        GovernancePathHelper.SecurityDocPath("api-endpoint-protection-inventory.json");

    private static string AuthorizationRolloutPath =>
        GovernancePathHelper.SecurityDocPath("authorization-policy-rollout.md");
}
