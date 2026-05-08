using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Verification;

public class Iso52016VerificationRegistryTests
{
    private static readonly string[] RequiredNonClaims =
    {
        "Validation/internal engineering anchors only.",
        "No full ISO 52016 equivalence claim.",
        "No StandardReference equivalence claim.",
        "No EnergyPlus comparison workflow claim.",
        "No ASHRAE 140 / BESTEST-style validation anchor claim."
    };

    [Fact]
    public void Registry_ExistsAndParses()
    {
        using var document = OpenRegistry();

        Assert.Equal(
            "AE-ISO52016-VERIFICATION-REGISTRY",
            document.RootElement.GetProperty("registryId").GetString());
    }

    [Fact]
    public void EveryStage_HasIdNameScopeAndClaimBoundary()
    {
        using var document = OpenRegistry();

        var stages = document.RootElement.GetProperty("stages").EnumerateArray().ToArray();
        Assert.NotEmpty(stages);

        foreach (var stage in stages)
        {
            Assert.False(string.IsNullOrWhiteSpace(stage.GetProperty("id").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(stage.GetProperty("name").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(stage.GetProperty("scope").GetString()));

            var claimBoundary = stage
                .GetProperty("claimBoundary")
                .EnumerateArray()
                .Select(item => item.GetString())
                .ToArray();

            foreach (var required in RequiredNonClaims)
            {
                Assert.Contains(required, claimBoundary);
            }
        }
    }

    [Fact]
    public void EveryListedFile_Exists()
    {
        using var document = OpenRegistry();
        var root = document.RootElement;

        var fileProperties = new[]
        {
            "relatedManifests",
            "requiredDocs",
            "requiredSourceFiles",
            "requiredTestFiles",
            "entrypointWrapperScripts"
        };

        foreach (var script in root.GetProperty("entrypointWrapperScripts").EnumerateArray())
        {
            AssertRepoFileExists(script.GetString());
        }

        foreach (var stage in root.GetProperty("stages").EnumerateArray())
        {
            foreach (var property in fileProperties)
            {
                foreach (var file in stage.GetProperty(property).EnumerateArray())
                {
                    AssertRepoFileExists(file.GetString());
                }
            }

            foreach (var alias in stage.GetProperty("deprecatedWrapperAliases").EnumerateArray())
            {
                AssertRepoFileExists(alias.GetProperty("path").GetString());
                Assert.False(string.IsNullOrWhiteSpace(alias.GetProperty("stageId").GetString()));
            }
        }
    }

    [Fact]
    public void Registry_HasNoPositiveParityClaims()
    {
        var registry = File.ReadAllLines(RegistryPath());

        AssertNoPositiveClaim(registry, "full ISO 52016 equivalence");
        AssertNoPositiveClaim(registry, "StandardReference equivalence");
        AssertNoPositiveClaim(registry, "EnergyPlus comparison workflow");
        AssertNoPositiveClaim(registry, "ASHRAE 140 / BESTEST-style validation anchor");
    }

    private static JsonDocument OpenRegistry() =>
        JsonDocument.Parse(File.ReadAllText(RegistryPath()));

    private static string RegistryPath() =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "verification",
            "Iso52016VerificationRegistry.json");

    private static void AssertRepoFileExists(string? relativePath)
    {
        Assert.False(string.IsNullOrWhiteSpace(relativePath));

        var path = Path.Combine(
            relativePath.Split('/').Prepend(TestPaths.RepoRoot).ToArray());

        Assert.True(File.Exists(path), $"Registry file does not exist: {relativePath}");
    }

    private static void AssertNoPositiveClaim(IEnumerable<string> lines, string claim)
    {
        foreach (var line in lines)
        {
            if (!line.Contains(claim, StringComparison.OrdinalIgnoreCase))
                continue;

            Assert.True(
                line.Contains("No ", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("not ", StringComparison.OrdinalIgnoreCase),
                $"Positive equivalence claim found: {line}");
        }
    }
}
