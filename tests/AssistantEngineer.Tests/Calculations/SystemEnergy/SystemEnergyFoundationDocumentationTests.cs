namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyFoundationDocumentationTests
{
    [Fact]
    public void Stage5SystemEnergyDocument_ExistsWithRequiredSectionsAndNonClaims()
    {
        var path = Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "system-energy.md");
        Assert.True(File.Exists(path), $"Expected documentation file was not found: {path}");

        var content = File.ReadAllText(path);

        var requiredPhrases = new[]
        {
            "System energy chain",
            "Supported uses",
            "Supported carriers",
            "Emission",
            "Distribution",
            "Storage",
            "Generation",
            "Final energy",
            "Primary energy",
            "CO2",
            "Recovered losses",
            "Auxiliary energy",
            "Ownership",
            "NoDoubleCounting",
            "ISO52016",
            "DHW",
            "Validation rules",
            "Fixtures",
            "Known limitations",
            "internal engineering implementation",
            "deterministic fixtures",
            "validation anchors only",
            "simplified engineering implementation",
            "no detailed hydraulic network",
            "no dynamic plant control",
            "no part-load performance curves",
            "no full national annex factor database",
            "no complete equipment catalogue",
            "no claim of full standard compliance"
        };

        foreach (var phrase in requiredPhrases)
        {
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }
}
