using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalVerificationOrchestrationTests
{
    [Fact]
    public void LegacyPhysicalVerificationTool_IsRemoved_AndUnifiedToolExists()
    {
        var repoRoot = FindRepositoryRoot();
        var legacyToolName = LegacyToolName();
        var legacyToolDirectory = Path.Combine(
            repoRoot,
            "tools",
            legacyToolName);
        var unifiedToolProject = Path.Combine(
            repoRoot,
            "tools",
            "AssistantEngineer.Tools.Iso52016Verification",
            "AssistantEngineer.Tools.Iso52016Verification.csproj");

        Assert.False(
            Directory.Exists(legacyToolDirectory),
            $"Legacy ISO52016 physical verification tool must be removed: {legacyToolDirectory}");
        Assert.True(
            File.Exists(unifiedToolProject),
            $"Unified ISO52016 verification tool project is missing: {unifiedToolProject}");
    }

    [Fact]
    public void Step07Manifest_ReferencesUnifiedTool()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016PhysicalVerificationOrchestrationStageManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        var manifest = File.ReadAllText(manifestPath);
        Assert.Contains("AE-ISO52016-002-STEP-07", manifest);
        Assert.Contains("AssistantEngineer.Tools.Iso52016Verification", manifest);
        Assert.DoesNotContain(LegacyToolName(), manifest);
    }

    [Fact]
    public void PhysicalModelChainWrapper_UsesUnifiedTool()
    {
        var repoRoot = FindRepositoryRoot();
        var wrapperPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-physical-model-chain.ps1");

        Assert.True(File.Exists(wrapperPath), $"Wrapper script was not found: {wrapperPath}");

        var script = File.ReadAllText(wrapperPath);
        Assert.Contains("AssistantEngineer.Tools.Iso52016Verification.csproj", script);
        Assert.Contains("verify-all", script);
        Assert.DoesNotContain(LegacyToolName(), script);
    }

    [Fact]
    public void Registry_DefinesStep07WithUnifiedToolOwnership()
    {
        var registry = ReadRegistry();

        var step07 = registry.RootElement
            .GetProperty("stages")
            .EnumerateArray()
            .Single(stage => stage.GetProperty("id").GetString() == "AE-ISO52016-002-STEP-07");

        var sourceFiles = step07
            .GetProperty("requiredSourceFiles")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("tools/AssistantEngineer.Tools.Iso52016Verification/Program.cs", sourceFiles);
        Assert.DoesNotContain(
            sourceFiles,
            path => path is not null &&
                    path.Contains(
                        LegacyToolName(),
                        StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("docs/calculations/Iso52016PhysicalVerificationOrchestration.md")]
    [InlineData("docs/calculations/Iso52016PhysicalModelChainReleaseGate.md")]
    [InlineData("docs/releases/Iso52016PhysicalVerificationOrchestrationStageManifest.json")]
    [InlineData("docs/releases/Iso52016PhysicalModelChainReleaseGateManifest.json")]
    [InlineData("docs/releases/Iso52016PhysicalChainFinalReadinessManifest.json")]
    [InlineData("docs/traceability/Iso52016PhysicalChainTraceabilityMatrix.json")]
    [InlineData("docs/verification/Iso52016VerificationRegistry.json")]
    public void CoreDocsAndManifests_DoNotReferenceLegacyTool(string relativePath)
    {
        var repoRoot = FindRepositoryRoot();
        var absolutePath = Path.Combine(
            relativePath.Split('/').Prepend(repoRoot).ToArray());

        Assert.True(File.Exists(absolutePath), $"Expected file was not found: {absolutePath}");

        var text = File.ReadAllText(absolutePath);
        Assert.DoesNotContain(LegacyToolName(), text);
        Assert.DoesNotContain($"tools/{LegacyToolName()}", text);
    }

    [Fact]
    public void CoreDocsAndManifests_ReferenceUnifiedTool()
    {
        var repoRoot = FindRepositoryRoot();
        var files = new[]
        {
            "docs/calculations/Iso52016PhysicalVerificationOrchestration.md",
            "docs/calculations/Iso52016PhysicalModelChainReleaseGate.md",
            "docs/releases/Iso52016PhysicalVerificationOrchestrationStageManifest.json",
            "docs/releases/Iso52016PhysicalModelChainReleaseGateManifest.json",
            "docs/releases/Iso52016PhysicalChainFinalReadinessManifest.json",
            "docs/traceability/Iso52016PhysicalChainTraceabilityMatrix.json"
        };

        foreach (var relativePath in files)
        {
            var absolutePath = Path.Combine(
                relativePath.Split('/').Prepend(repoRoot).ToArray());
            var text = File.ReadAllText(absolutePath);

            Assert.Contains("AssistantEngineer.Tools.Iso52016Verification", text);
        }
    }

    private static JsonDocument ReadRegistry()
    {
        var repoRoot = FindRepositoryRoot();
        var path = Path.Combine(
            repoRoot,
            "docs",
            "verification",
            "Iso52016VerificationRegistry.json");

        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static string LegacyToolName() =>
        "AssistantEngineer.Tools." + "Iso52016Physical" + "Verification";

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var src = Path.Combine(
                directory.FullName,
                "src",
                "Backend",
                "AssistantEngineer.Modules.Calculations");

            var tests = Path.Combine(
                directory.FullName,
                "tests",
                "AssistantEngineer.Tests");

            if (Directory.Exists(src) && Directory.Exists(tests))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate AssistantEngineer repository root from test base directory.");
    }
}
