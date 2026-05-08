using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalChainStageRegistryTests
{
    [Fact]
    public void Registry_DocumentsAllPhysicalStagesAndClaimBoundaries()
    {
        var repoRoot = FindRepositoryRoot();
        var registryPath = Path.Combine(
            repoRoot,
            "docs",
            "traceability",
            "Iso52016PhysicalChainStageRegistry.json");

        Assert.True(File.Exists(registryPath), $"Registry was not found: {registryPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(registryPath));
        var root = document.RootElement;

        Assert.Equal("AE-ISO52016-002-STAGE-REGISTRY", root.GetProperty("stageId").GetString());
        Assert.Equal("internal-engineering-gate", root.GetProperty("status").GetString());
        Assert.True(root.GetProperty("matrixAllVerificationIntegrated").GetBoolean());

        var claimBoundary = root
            .GetProperty("claimBoundary")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("Validation/internal engineering anchors only.", claimBoundary);
        Assert.Contains("Not full ISO 52016 equivalence.", claimBoundary);
        Assert.Contains("Not complete ISO 52016 numerical equivalence.", claimBoundary);
        Assert.Contains("Not StandardReference equivalence.", claimBoundary);
        Assert.Contains("Not EnergyPlus comparison workflow.", claimBoundary);
        Assert.Contains("Not ASHRAE Standard 140 validation.", claimBoundary);
        Assert.Contains("Not ASHRAE Standard 140 benchmark-grade claim.", claimBoundary);

        var stages = root.GetProperty("stages").EnumerateArray().ToArray();
        Assert.True(stages.Length >= 14);

        foreach (var stageId in Enumerable.Range(1, 14).Select(index => $"AE-ISO52016-002-STEP-{index:00}"))
        {
            Assert.Contains(stages, stage => stage.GetProperty("stageId").GetString() == stageId);
        }
    }

    [Fact]
    public void RegistryVerifierTool_OwnsDurablePhysicalStageRegistryChecks()
    {
        var repoRoot = FindRepositoryRoot();
        var tool = ReadIsoToolSourceBundle(repoRoot);

        Assert.Contains("Iso52016VerificationRegistry.json", tool);
        Assert.Contains("VerifyStageManifests", tool);
        Assert.Contains("VerifyStageFiles", tool);
        Assert.Contains("VerifyWrapperScripts", tool);
        Assert.Contains("VerifyClaimBoundaries", tool);
        Assert.Contains("VerifyClaimBoundary", tool);
        Assert.Contains("ForbiddenPositiveClaims", tool);
    }

    [Fact]
    public void VerificationWrapper_IsThinAndUsesCSharpRegistryTool()
    {
        var repoRoot = FindRepositoryRoot();
        var wrapperPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-physical-chain-stage-registry.ps1");

        Assert.True(File.Exists(wrapperPath), $"Wrapper was not found: {wrapperPath}");

        var wrapper = File.ReadAllText(wrapperPath);

        Assert.Contains("dotnet", wrapper);
        Assert.Contains("AssistantEngineer.Tools.Iso52016Verification", wrapper);
        Assert.Contains("verify-stage", wrapper);
        Assert.DoesNotContain("<<<<<<<", wrapper);
        Assert.DoesNotContain(">>>>>>>", wrapper);
    }

    [Fact]
    public void VerificationRegistry_ReferencesPhysicalStageRegistryGate()
    {
        RegistryContainsStageFile(
            "AE-ISO52016-002-STEP-15",
            "requiredDocs",
            "docs/traceability/Iso52016PhysicalChainStageRegistry.json");
        RegistryContainsStageFile(
            "AE-ISO52016-002-STEP-15",
            "relatedManifests",
            "docs/releases/Iso52016PhysicalChainStageRegistryManifest.json");
    }

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

    private static string ReadIsoToolSourceBundle(string repoRoot)
    {
        var programPath = Path.Combine(
            repoRoot,
            "tools",
            "AssistantEngineer.Tools.Iso52016Verification",
            "Program.cs");
        var runnerPath = Path.Combine(
            repoRoot,
            "tools",
            "AssistantEngineer.Tools.Iso52016Verification",
            "Iso52016VerificationRunner.cs");

        Assert.True(File.Exists(programPath), $"Tool program was not found: {programPath}");
        Assert.True(File.Exists(runnerPath), $"Tool runner was not found: {runnerPath}");

        return string.Join(
            Environment.NewLine,
            File.ReadAllText(programPath),
            File.ReadAllText(runnerPath));
    }
}
