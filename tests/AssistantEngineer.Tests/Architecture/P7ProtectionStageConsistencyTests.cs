using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7ProtectionStageConsistencyTests
{
    private static readonly HashSet<string> AllowedProtectionStages =
    [
        "P5-09",
        "P5-10",
        "P5-11",
        "P5-12",
        "P5-13",
        "P5-14",
        "P5-15",
        "P5-16C",
        "P5-16D",
        "Deferred",
        "Compatibility",
        "Public",
        "UnknownNeedsClassification"
    ];

    [Fact]
    public void ProtectionStageValuesUseAllowedVocabulary()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var endpoints = inventory.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();
        Assert.NotEmpty(endpoints);

        foreach (var endpoint in endpoints)
        {
            var protectionStage = endpoint.GetProperty("protectionStage").GetString() ?? string.Empty;
            Assert.Contains(protectionStage, AllowedProtectionStages);
        }
    }

    [Fact]
    public void RolloutDocsHaveInventoryCoverageOrDocumentedAbsence()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var stages = inventory.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Select(endpoint => endpoint.GetProperty("protectionStage").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        var expectedStageDocs = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["P5-09"] = GovernancePathHelper.SecurityDocPath("protected-endpoint-pilot-rollout.md"),
            ["P5-10"] = GovernancePathHelper.SecurityDocPath("protected-read-endpoints-rollout.md"),
            ["P5-11"] = GovernancePathHelper.SecurityDocPath("protected-write-endpoints-rollout.md"),
            ["P5-12"] = GovernancePathHelper.SecurityDocPath("protected-execution-endpoints-rollout.md"),
            ["P5-13"] = GovernancePathHelper.SecurityDocPath("protected-report-artifact-endpoints-rollout.md"),
            ["P5-14"] = GovernancePathHelper.SecurityDocPath("protected-workflow-read-history-rollout.md"),
            ["P5-15"] = GovernancePathHelper.SecurityDocPath("tenant-isolation-integration-matrix.md")
        };

        foreach (var pair in expectedStageDocs)
        {
            Assert.True(File.Exists(pair.Value), $"Missing rollout doc: {pair.Value}");
            if (pair.Key == "P5-15")
            {
                var matrixDoc = File.ReadAllText(pair.Value);
                Assert.Contains("matrix", matrixDoc, StringComparison.OrdinalIgnoreCase);
                continue;
            }

            Assert.Contains(pair.Key, stages);
        }
    }

    [Fact]
    public void ProtectedGroupsCannotBeMarkedAsPublicStage()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var endpoints = inventory.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();

        foreach (var endpoint in endpoints)
        {
            var group = endpoint.GetProperty("endpointGroup").GetString() ?? string.Empty;
            var stage = endpoint.GetProperty("protectionStage").GetString() ?? string.Empty;

            if (!group.StartsWith("Protected", StringComparison.Ordinal))
                continue;

            Assert.NotEqual("Public", stage);
        }
    }

    [Fact]
    public void PublicStageDoesNotRequireProtectedPermissionWithoutKnownLimitation()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var endpoints = inventory.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();

        foreach (var endpoint in endpoints)
        {
            var stage = endpoint.GetProperty("protectionStage").GetString() ?? string.Empty;
            if (!string.Equals(stage, "Public", StringComparison.Ordinal))
                continue;

            var permission = endpoint.GetProperty("permission").GetString() ?? string.Empty;
            if (string.Equals(permission, "NotApplicable", StringComparison.Ordinal))
                continue;

            var knownLimitations = endpoint.GetProperty("knownLimitations").EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .ToArray();
            Assert.Contains(knownLimitations, item =>
                item.Contains("public", StringComparison.OrdinalIgnoreCase) ||
                item.Contains("staged", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static string InventoryJsonPath =>
        GovernancePathHelper.SecurityDocPath("api-endpoint-protection-inventory.json");
}
