using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalBranchHygieneStageTests
{
    [Fact]
    public void RepositoryHygieneTool_OwnsDurableBranchChecks()
    {
        var repoRoot = FindRepositoryRoot();
        var programPath = Path.Combine(
            repoRoot,
            "tools",
            "AssistantEngineer.Tools.RepositoryHygieneVerification",
            "Program.cs");

        Assert.True(File.Exists(programPath), $"Tool program was not found: {programPath}");

        var program = File.ReadAllText(programPath);

        Assert.Contains("AssertNoRebaseInProgress", program);
        Assert.Contains("AssertNoConflictMarkers", program);
        Assert.Contains("AssertJsonFilesParse", program);
        Assert.Contains("AssertNoRootPatchScripts", program);
        Assert.Contains("AssertWorkingTreeClean", program);
        Assert.Contains("JsonDocument.Parse", program);
        Assert.Contains("GitConflictStartMarker", program);
        Assert.Contains("--check-root-patch-scripts", program);
        Assert.Contains("--require-clean", program);
    }

    [Fact]
    public void Wrapper_IsThinAndInvokesRepositoryHygieneTool()
    {
        var repoRoot = FindRepositoryRoot();
        var wrapperPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "assert-iso52016-physical-branch-hygiene.ps1");

        Assert.True(File.Exists(wrapperPath), $"Wrapper was not found: {wrapperPath}");

        var wrapper = File.ReadAllText(wrapperPath);

        Assert.Contains("AssistantEngineer.Tools.RepositoryHygieneVerification", wrapper);
        Assert.Contains("dotnet", wrapper);
        Assert.Contains("--repo-root", wrapper);
        Assert.Contains("RequireClean", wrapper);
        Assert.Contains("CheckRootPatchScripts", wrapper);
    }

    [Fact]
    public void Manifest_DocumentsBranchHygieneStageAndClaimBoundary()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016PhysicalBranchHygieneStageManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-ISO52016-002-STEP-14", root.GetProperty("stageId").GetString());
        Assert.Equal("internal-engineering-gate", root.GetProperty("status").GetString());
        Assert.True(root.GetProperty("matrixAllVerificationIntegrated").GetBoolean());

        var claimBoundary = root
            .GetProperty("claimBoundary")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("ISO52016 physical branch hygiene and verification orchestration stage.", claimBoundary);
        Assert.Contains("Validation/internal engineering anchors only.", claimBoundary);
        Assert.Contains("Not a new solver.", claimBoundary);
        Assert.Contains("Not complete ISO 52016 numerical equivalence.", claimBoundary);
        Assert.Contains("Not pyBuildingEnergy numerical equivalence.", claimBoundary);
        Assert.Contains("Not EnergyPlus numerical equivalence.", claimBoundary);
        Assert.Contains("Not ASHRAE Standard 140 validation.", claimBoundary);
    }

    [Fact]
    public void StageDocumentation_StatesHygieneScopeAndNoExternalClaims()
    {
        var repoRoot = FindRepositoryRoot();
        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016PhysicalBranchHygieneStage.md");

        Assert.True(File.Exists(docPath), $"Stage document was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("branch hygiene", doc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validation/internal engineering anchors only", doc);
        Assert.Contains("not a new solver", doc);
        Assert.Contains("not complete ISO 52016 numerical equivalence", doc);
        Assert.Contains("not pyBuildingEnergy numerical equivalence", doc);
        Assert.Contains("not EnergyPlus numerical equivalence", doc);
        Assert.Contains("not ASHRAE Standard 140 validation", doc);
    }

    [Fact]
    public void MatrixAllVerificationScript_ReferencesBranchHygieneStage()
    {
        var repoRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-all.ps1");

        Assert.True(File.Exists(scriptPath), $"Matrix all script was not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("verify-iso52016-physical-branch-hygiene-stage.ps1", script);
        Assert.Contains("Iso52016PhysicalBranchHygieneStageManifest.json", script);
        Assert.Contains("AE-ISO52016-002 Step 14", script);
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

        throw new DirectoryNotFoundException("Could not locate AssistantEngineer repository root from test base directory.");
    }
}
