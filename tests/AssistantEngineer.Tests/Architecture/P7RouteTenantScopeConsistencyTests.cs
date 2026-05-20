using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7RouteTenantScopeConsistencyTests
{
    private static readonly HashSet<string> AllowedTenantScopeValues =
    [
        "Project",
        "Building",
        "Workflow",
        "Scenario",
        "Job",
        "Artifact",
        "TenantScoped",
        "LegacyUnscopedAllowed",
        "Deferred",
        "NotApplicable",
        "UnknownNeedsClassification"
    ];

    [Fact]
    public void TenantScopeValuesUseAllowedVocabulary()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var endpoints = inventory.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();
        Assert.NotEmpty(endpoints);

        foreach (var endpoint in endpoints)
        {
            var tenantScope = endpoint.GetProperty("tenantScope").GetString() ?? string.Empty;
            Assert.Contains(tenantScope, AllowedTenantScopeValues);
        }
    }

    [Fact]
    public void ProtectedGroupsMustHaveTenantScopeClassification()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var endpoints = inventory.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();

        var protectedGroups = new HashSet<string>(StringComparer.Ordinal)
        {
            "ProtectedRead",
            "ProtectedWrite",
            "WorkflowRead",
            "WorkflowExecute",
            "CalculationRun",
            "ReportsRead",
            "ReportsWrite",
            "ArtifactRead"
        };

        foreach (var endpoint in endpoints)
        {
            var group = endpoint.GetProperty("endpointGroup").GetString() ?? string.Empty;
            if (!protectedGroups.Contains(group))
                continue;

            var scope = endpoint.GetProperty("tenantScope").GetString() ?? string.Empty;
            Assert.NotEqual("NotApplicable", scope);
            Assert.NotEqual("UnknownNeedsClassification", scope);
        }
    }

    [Fact]
    public void TenantIsolationMatrixReferencesCoreProtectedPermissionGroups()
    {
        using var matrix = GovernanceJsonTestHelper.Parse(TenantIsolationMatrixJsonPath);
        var matrixGroups = matrix.RootElement.GetProperty("endpointGroups")
            .EnumerateArray()
            .Select(group => group.GetProperty("group").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        var requiredGroups = new[]
        {
            "ProjectsRead",
            "ProjectsWrite",
            "BuildingsRead",
            "BuildingsWrite",
            "WorkflowsRead",
            "WorkflowsExecute",
            "ReportsRead",
            "ReportsWrite"
        };

        foreach (var requiredGroup in requiredGroups)
            Assert.Contains(requiredGroup, matrixGroups);
    }

    [Fact]
    public void UnknownClassificationEntriesCarryKnownLimitations()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var unknownEntries = inventory.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Where(endpoint => string.Equals(
                endpoint.GetProperty("tenantScope").GetString(),
                "UnknownNeedsClassification",
                StringComparison.Ordinal))
            .ToArray();

        foreach (var endpoint in unknownEntries)
        {
            var limitations = endpoint.GetProperty("knownLimitations").EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .ToArray();
            Assert.Contains(limitations, item => item.Contains("classification", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static string InventoryJsonPath =>
        GovernancePathHelper.SecurityDocPath("api-endpoint-protection-inventory.json");

    private static string TenantIsolationMatrixJsonPath =>
        GovernancePathHelper.SecurityDocPath("tenant-isolation-integration-matrix.json");
}
