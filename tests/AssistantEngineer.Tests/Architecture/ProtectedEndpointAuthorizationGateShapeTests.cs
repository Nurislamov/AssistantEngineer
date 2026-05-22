using AssistantEngineer.Api.Security.Authorization;

namespace AssistantEngineer.Tests.Architecture;

public sealed class ProtectedEndpointAuthorizationGateShapeTests
{
    [Fact]
    public void Gate_ImplementsStableFacadeInterface()
    {
        Assert.Contains(
            typeof(IProtectedEndpointAuthorizationGate),
            typeof(ProtectedEndpointAuthorizationGate).GetInterfaces());
    }

    [Fact]
    public void FacadeInterface_PublicMethodSetRemainsStable()
    {
        var methods = typeof(IProtectedEndpointAuthorizationGate)
            .GetMethods()
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var expected = new[]
        {
            "RequireArtifactReadPermissionAsync",
            "RequireArtifactWritePermissionAsync",
            "RequireBuildingPermissionAsync",
            "RequireCalculationPermissionAsync",
            "RequirePermissionAsync",
            "RequireProjectPermissionAsync",
            "RequireReportReadPermissionAsync",
            "RequireReportWritePermissionAsync",
            "RequireWorkflowPermissionAsync",
            "RequireWorkflowReadPermissionAsync"
        };

        Assert.Equal(expected, methods);
    }

    [Fact]
    public void GateSource_UsesExtractedAuthorizationCollaborators()
    {
        var gatePath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Security",
            "Authorization",
            "ProtectedEndpointAuthorizationGate.cs");

        var source = File.ReadAllText(gatePath);

        Assert.Contains("IProtectedEndpointAuthorizationDecisionFactory", source, StringComparison.Ordinal);
        Assert.Contains("IProtectedEndpointTenantMismatchPolicy", source, StringComparison.Ordinal);
        Assert.Contains("IProtectedEndpointAuthorizationLogger", source, StringComparison.Ordinal);
        Assert.Contains("IProtectedEndpointPermissionEvaluator", source, StringComparison.Ordinal);
        Assert.Contains("IProtectedEndpointScopeEvaluationService", source, StringComparison.Ordinal);
    }

    [Fact]
    public void GateSource_RemainsFocusedComparedToPreDecompositionHotspot()
    {
        var gatePath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Security",
            "Authorization",
            "ProtectedEndpointAuthorizationGate.cs");

        var nonBlankLineCount = File.ReadAllLines(gatePath)
            .Count(line => !string.IsNullOrWhiteSpace(line));

        Assert.True(
            nonBlankLineCount <= 700,
            $"ProtectedEndpointAuthorizationGate should remain under focused-size guard after staged extraction. Current non-blank lines: {nonBlankLineCount}.");
    }
}
