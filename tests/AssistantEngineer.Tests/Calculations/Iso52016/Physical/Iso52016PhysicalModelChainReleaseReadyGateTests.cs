using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalModelChainReleaseReadyGateTests
{
    [Fact]
    public void ReleaseReadyManifest_DocumentsClaimBoundaryAndCSharpTool()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016PhysicalModelChainReleaseGateManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-ISO52016-002-STEP-08", root.GetProperty("stageId").GetString());
        Assert.True(root.GetProperty("usesCSharpVerificationTool").GetBoolean());
        Assert.True(root.GetProperty("thinPowerShellWrapper").GetBoolean());

        var closedWorkItems = root
            .GetProperty("closedWorkItems")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("AE-ISO52016-002", closedWorkItems);

        var claimBoundary = root
            .GetProperty("claimBoundary")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("ISO52016-inspired physical model release gate.", claimBoundary);
        Assert.Contains("Validation/internal engineering anchors only.", claimBoundary);
        Assert.Contains("Not complete ISO 52016 numerical equivalence.", claimBoundary);
        Assert.Contains("Not pyBuildingEnergy numerical equivalence.", claimBoundary);
        Assert.Contains("Not EnergyPlus numerical equivalence.", claimBoundary);
        Assert.Contains("Not ASHRAE Standard 140 benchmark-grade claim.", claimBoundary);
    }

    [Fact]
    public void ReleaseReadyWrapper_IsThinAndDelegatesToCSharpVerificationTool()
    {
        var repoRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "assert-iso52016-physical-model-chain-release-ready.ps1");

        Assert.True(File.Exists(scriptPath), $"Release-ready script was not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("AssistantEngineer.Tools.Iso52016Verification.csproj", script);
        Assert.Contains("assert-release-ready", script);
        Assert.Contains("--skip-tests", script);
        Assert.Contains("dotnet", script);
        Assert.DoesNotContain("Iso52016PhysicalRoomModelBuilder.cs", script);
        Assert.DoesNotContain("Iso52016MatrixHourlySolver.cs", script);
    }

    [Fact]
    public void CSharpVerificationTool_ContainsReleaseReadyChecksAndClaimBoundary()
    {
        var repoRoot = FindRepositoryRoot();
        var programPath = Path.Combine(
            repoRoot,
            "tools",
            "AssistantEngineer.Tools.Iso52016Verification",
            "Program.cs");

        Assert.True(File.Exists(programPath), $"Verification tool was not found: {programPath}");

        var program = File.ReadAllText(programPath);

        Assert.Contains("assert-release-ready", program);
        Assert.Contains("VerifyReleaseReadyManifests", program);
        Assert.Contains("VerifyClaimBoundaries", program);
        Assert.Contains("validation/internal engineering anchors only", program, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("assert-iso52016-physical-model-chain-release-ready.ps1", program);
    }

    [Fact]
    public void VerificationRegistry_KeepsReleaseReadyDiscoverability()
    {
        RegistryContainsStageFile(
            "AE-ISO52016-002-STEP-08",
            "relatedManifests",
            "docs/releases/Iso52016PhysicalModelChainReleaseGateManifest.json");
        Assert.Contains(
            "docs/releases/Iso52016PhysicalModelChainReleaseGateManifest.json",
            ReadIso52016VerificationRegistry());
    }

    [Fact]
    public void StageDocumentation_StatesNoExternalParityClaims()
    {
        var repoRoot = FindRepositoryRoot();
        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016PhysicalModelChainReleaseGate.md");

        Assert.True(File.Exists(docPath), $"Stage document was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("AE-ISO52016-002 Step 08", doc);
        Assert.Contains("internal engineering anchors only", doc);
        Assert.Contains("not complete ISO 52016 numerical equivalence", doc);
        Assert.Contains("not pyBuildingEnergy numerical equivalence", doc);
        Assert.Contains("not EnergyPlus numerical equivalence", doc);
        Assert.Contains("not ASHRAE Standard 140 benchmark-grade claim", doc);

        Assert.DoesNotContain("complete numerical equivalence achieved", doc, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EnergyPlus numerical equivalence achieved", doc, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pyBuildingEnergy numerical equivalence achieved", doc, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ASHRAE Standard 140 benchmark-grade claim passed", doc, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var solution = Path.Combine(directory.FullName, "AssistantEngineer.sln");
            var tests = Path.Combine(directory.FullName, "tests", "AssistantEngineer.Tests");

            if (File.Exists(solution) && Directory.Exists(tests))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate AssistantEngineer repository root from test base directory.");
    }
}
