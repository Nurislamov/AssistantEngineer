using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalVerificationOrchestrationTests
{
    [Fact]
    public void ToolProject_ExistsAndTargetsNet10()
    {
        var repoRoot = FindRepositoryRoot();
        var projectPath = Path.Combine(
            repoRoot,
            "tools",
            "AssistantEngineer.Tools.Iso52016PhysicalVerification",
            "AssistantEngineer.Tools.Iso52016PhysicalVerification.csproj");

        Assert.True(File.Exists(projectPath), $"Tool project was not found: {projectPath}");

        var project = File.ReadAllText(projectPath);

        Assert.Contains("<OutputType>Exe</OutputType>", project);
        Assert.Contains("<TargetFramework>net10.0</TargetFramework>", project);
        Assert.Contains("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", project);
    }

    [Fact]
    public void ToolProgram_OwnsPhysicalVerificationOrchestration()
    {
        var repoRoot = FindRepositoryRoot();
        var programPath = Path.Combine(
            repoRoot,
            "tools",
            "AssistantEngineer.Tools.Iso52016PhysicalVerification",
            "Program.cs");

        Assert.True(File.Exists(programPath), $"Tool Program.cs was not found: {programPath}");

        var program = File.ReadAllText(programPath);

        Assert.Contains("VerifyStageManifests", program);
        Assert.Contains("VerifyClaimBoundaries", program);
        Assert.Contains("VerifyMatrixAllHook", program);
        Assert.Contains("Iso52016PhysicalRoomModelDiagnosticsStageManifest.json", program);
        Assert.Contains("validation/internal engineering anchors only", program, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet", program);
        Assert.Contains("--skip-tests", program);
    }

    [Fact]
    public void Wrapper_IsThinAndCallsCSharpTool()
    {
        var repoRoot = FindRepositoryRoot();
        var wrapperPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-physical-model-chain.ps1");

        Assert.True(File.Exists(wrapperPath), $"Wrapper script was not found: {wrapperPath}");

        var script = File.ReadAllText(wrapperPath);

        Assert.Contains("AssistantEngineer.Tools.Iso52016PhysicalVerification.csproj", script);
        Assert.Contains("dotnet", script);
        Assert.Contains("--repo-root", script);
        Assert.Contains("--skip-tests", script);
        Assert.DoesNotContain("Get-ChildItem", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConvertFrom-Json", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Manifest_DocumentsStep07AndClaimBoundary()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016PhysicalVerificationOrchestrationStageManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-ISO52016-002-STEP-07", root.GetProperty("stageId").GetString());
        Assert.True(root.GetProperty("matrixAllVerificationIntegrated").GetBoolean());

        var claimBoundary = root
            .GetProperty("claimBoundary")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("ISO52016-inspired physical verification orchestration stage.", claimBoundary);
        Assert.Contains("Validation/internal engineering anchors only.", claimBoundary);
        Assert.Contains("Not complete ISO 52016 numerical equivalence.", claimBoundary);
        Assert.Contains("Not pyBuildingEnergy numerical equivalence.", claimBoundary);
        Assert.Contains("Not EnergyPlus numerical equivalence.", claimBoundary);
        Assert.Contains("Not ASHRAE Standard 140 benchmark-grade claim.", claimBoundary);
    }

    [Fact]
    public void MatrixAllVerificationScript_ReferencesPhysicalModelChainWrapper()
    {
        var repoRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-all.ps1");

        Assert.True(File.Exists(scriptPath), $"Matrix all verification script was not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("verify-iso52016-physical-model-chain.ps1", script);
        Assert.Contains("Iso52016PhysicalVerificationOrchestrationStageManifest.json", script);
        Assert.Contains("Step 07 physical verification orchestration", script);
    }

    [Fact]
    public void StageDocument_StatesNoExternalParityClaims()
    {
        var repoRoot = FindRepositoryRoot();
        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016PhysicalVerificationOrchestration.md");

        Assert.True(File.Exists(docPath), $"Stage document was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("AE-ISO52016-002 Step 07", doc);
        Assert.Contains("C# verifier", doc);
        Assert.Contains("validation/internal engineering anchors only", doc);
        Assert.Contains("not complete ISO 52016 numerical equivalence", doc);
        Assert.Contains("not pyBuildingEnergy numerical equivalence", doc);
        Assert.Contains("not EnergyPlus numerical equivalence", doc);
        Assert.Contains("not ASHRAE Standard 140 benchmark-grade claim", doc);
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
}