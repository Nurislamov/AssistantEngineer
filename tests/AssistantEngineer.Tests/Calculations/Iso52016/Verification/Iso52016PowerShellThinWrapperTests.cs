namespace AssistantEngineer.Tests.Calculations.Iso52016.Verification;

public class Iso52016PowerShellThinWrapperTests
{
    private static readonly string[] ForbiddenTokens =
    {
        "requiredFiles = @(",
        "Assert-NoForbiddenPositiveClaims",
        "dotnet test",
        "BEGIN ISO52016",
        "BEGIN AE-ISO52016",
        "Invoke-RepoScript",
        "Invoke-RepoCommand",
        "SkipPhysical"
    };

    [Theory]
    [InlineData("verify-iso52016-matrix-all.ps1", "verify-all")]
    [InlineData("assert-iso52016-matrix-release-ready.ps1", "assert-release-ready")]
    public void MatrixEntrypoints_AreThinWrappers(string scriptName, string command)
    {
        var script = ReadIsoScript(scriptName);

        Assert.Contains("AssistantEngineer.Tools.Iso52016Verification.csproj", script);
        Assert.Contains(command, script);
        Assert.Contains("--repo-root", script);

        foreach (var token in ForbiddenTokens)
        {
            Assert.DoesNotContain(token, script, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("verify-iso52016-physical-node-model-stage.ps1", "AE-ISO52016-002-STEP-01")]
    [InlineData("verify-iso52016-physical-surface-model-stage.ps1", "AE-ISO52016-002-STEP-02")]
    [InlineData("verify-iso52016-physical-boundary-profile-stage.ps1", "AE-ISO52016-002-STEP-03")]
    [InlineData("verify-iso52016-physical-operation-profile-stage.ps1", "AE-ISO52016-002-STEP-04")]
    [InlineData("verify-iso52016-physical-room-simulation-service-stage.ps1", "AE-ISO52016-002-STEP-05")]
    [InlineData("verify-iso52016-physical-room-model-diagnostics-stage.ps1", "AE-ISO52016-002-STEP-06")]
    [InlineData("verify-iso52016-physical-model-selection-stage.ps1", "AE-ISO52016-002-STEP-10")]
    [InlineData("verify-iso52016-physical-model-selection-application-guard.ps1", "AE-ISO52016-002-STEP-11")]
    [InlineData("verify-iso52016-physical-selection-application-integration-hardening.ps1", "AE-ISO52016-002-STEP-13")]
    public void PhysicalStageCompatibilityScripts_CallVerifyStage(string scriptName, string stageId)
    {
        var script = ReadIsoScript(scriptName);

        Assert.Contains("AssistantEngineer.Tools.Iso52016Verification.csproj", script);
        Assert.Contains("verify-stage", script);
        Assert.Contains("--stage-id", script);
        Assert.Contains(stageId, script);

        foreach (var token in ForbiddenTokens)
        {
            Assert.DoesNotContain(token, script, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ReadIsoScript(string fileName)
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "iso52016",
            fileName);

        Assert.True(File.Exists(path), $"Expected wrapper does not exist: {path}");
        return File.ReadAllText(path);
    }
}
