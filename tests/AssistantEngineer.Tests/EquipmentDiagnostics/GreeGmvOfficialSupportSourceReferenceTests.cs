using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvOfficialSupportSourceReferenceTests
{
    private const string OfficialSupportCatalogManualId = "gree-official-support-error-catalog";

    private static readonly IReadOnlyDictionary<string, string> ExpectedRuntimeIds =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["A0"] = "gree-gmv6-status-a0",
            ["C0"] = "gree-gmv6-debugging-c0",
            ["C7"] = "gree-gmv6-debugging-c7",
            ["E0"] = "gree-gmv6-outdoor-e0",
            ["E1"] = "gree-gmv6-outdoor-e1",
            ["F3"] = "gree-gmv6-outdoor-f3",
            ["H5"] = "gree-gmv6-outdoor-h5",
            ["L1"] = "gree-gmv6-indoor-l1",
            ["o1"] = "gree-gmv6-indoor-o1",
            ["P0"] = "gree-gmv6-outdoor-p0",
            ["P1"] = "gree-gmv6-outdoor-p1",
            ["P2"] = "gree-gmv6-outdoor-p2",
            ["U0"] = "gree-gmv6-debugging-u0",
            ["U2"] = "gree-gmv6-debugging-u2",
            ["U3"] = "gree-gmv6-debugging-u3",
            ["U4"] = "gree-gmv6-debugging-u4",
            ["U5"] = "gree-gmv6-debugging-u5",
        };

    [Fact]
    public void RuntimeLoaderIncludesOfficialSupportReferenceForApprovedGmvOverlayTargets()
    {
        var source = new JsonErrorKnowledgeLocalizationSource();

        var entries = source.GetEntries();

        Assert.Equal(1293, entries.Count);

        var officialSupportEntries = entries
            .Where(entry => entry.SourceReferences.Any(reference =>
                reference.ManualId == OfficialSupportCatalogManualId))
            .OrderBy(entry => entry.Code, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedRuntimeIds.Count, officialSupportEntries.Length);
        Assert.Equal(
            ExpectedRuntimeIds.Keys.Order(StringComparer.Ordinal).ToArray(),
            officialSupportEntries.Select(entry => entry.Code).Order(StringComparer.Ordinal).ToArray());

        foreach (var entry in officialSupportEntries)
        {
            Assert.True(ExpectedRuntimeIds.TryGetValue(entry.Code, out var expectedId), $"Unexpected official support entry code: {entry.Code}");
            Assert.Equal(expectedId, entry.Id);
            Assert.Equal("GMV6", entry.Series);

            var reference = Assert.Single(
                entry.SourceReferences,
                item => item.ManualId == OfficialSupportCatalogManualId);

            Assert.Equal($"Gree-GMV-{entry.Code}", reference.DocumentCode);
            Assert.Equal("Gree official support error code catalog", reference.SourceName);
            Assert.Equal("Manual", reference.SourceType);
            Assert.Equal("en", reference.SourceLanguage);
            Assert.Equal("ManualVerified", reference.VerificationStatus);
            Assert.Equal("High", reference.Confidence);
            Assert.Equal(entry.PackageId, reference.PackageId);
            Assert.StartsWith("data/reference/gree-official-support-error-catalog/", reference.SourceReference, StringComparison.Ordinal);
            Assert.Contains($"Gree-GMV-{entry.Code}", reference.SourceReference, StringComparison.Ordinal);
            Assert.EndsWith(".json", reference.SourceReference, StringComparison.Ordinal);
            Assert.Contains("ED-24GEC.6D", reference.Notes, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void OfficialSupportReferenceDoesNotMergeIntoGmvMiniC0()
    {
        var source = new JsonErrorKnowledgeLocalizationSource();

        var entries = source.GetEntries();
        var gmv6C0 = Assert.Single(entries, entry => entry.Id == "gree-gmv6-debugging-c0");
        var miniC0 = Assert.Single(entries, entry => entry.Id == "gree-gmv-mini-indoor-c0");

        Assert.Contains(
            gmv6C0.SourceReferences,
            reference => reference.ManualId == OfficialSupportCatalogManualId &&
                reference.DocumentCode == "Gree-GMV-C0");

        Assert.DoesNotContain(
            miniC0.SourceReferences,
            reference => reference.ManualId == OfficialSupportCatalogManualId);
    }
}
