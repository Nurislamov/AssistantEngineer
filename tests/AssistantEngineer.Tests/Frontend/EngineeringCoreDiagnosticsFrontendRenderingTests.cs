using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Frontend;

public class EngineeringCoreDiagnosticsFrontendRenderingTests
{
    [Fact]
    public void EngineeringCoreDisclosurePanelRendersCalculationDiagnostics()
    {
        var content = ReadFrontendFile(
            "src",
            "Frontend",
            "src",
            "widgets",
            "engineering-core-disclosure",
            "ui",
            "EngineeringCoreDisclosurePanel.tsx");

        Assert.Contains("CalculationDiagnosticApiResponse", content, StringComparison.Ordinal);
        Assert.Contains("diagnostics", content, StringComparison.Ordinal);
        Assert.Contains("Calculation diagnostics", content, StringComparison.Ordinal);
        Assert.Contains("Iso52016.WeatherSolarContextUsed", content, StringComparison.Ordinal);
        Assert.Contains("Iso52016.SolarGainComponentPathUsed", content, StringComparison.Ordinal);
        Assert.Contains("Iso52016.MatrixSolarRadiationFallbackUsed", content, StringComparison.Ordinal);
        Assert.Contains("matrix solar radiation fallback", content, StringComparison.Ordinal);
    }

    [Fact]
    public void EngineeringCoreDisclosurePanelCanRenderDiagnosticsWithoutCalculationDisclosure()
    {
        var content = ReadFrontendFile(
            "src",
            "Frontend",
            "src",
            "widgets",
            "engineering-core-disclosure",
            "ui",
            "EngineeringCoreDisclosurePanel.tsx");

        Assert.Contains("if (!disclosure && diagnostics.length === 0)", content, StringComparison.Ordinal);
        Assert.Contains("diagnostics.length > 0", content, StringComparison.Ordinal);
        Assert.Contains("extractDiagnostics", content, StringComparison.Ordinal);
    }

    [Fact]
    public void FrontendDiagnosticsDocumentationMentionsIso52016SolarPathCodes()
    {
        var content = ReadRepoFile(
            "docs",
            "frontend",
            "EngineeringCoreV1DiagnosticsUx.md");

        Assert.Contains("Iso52016.WeatherSolarContextUsed", content, StringComparison.Ordinal);
        Assert.Contains("Iso52016.SolarGainComponentPathUsed", content, StringComparison.Ordinal);
        Assert.Contains("Iso52016.MatrixSolarRadiationFallbackUsed", content, StringComparison.Ordinal);
        Assert.Contains("must not be hidden only in raw JSON", content, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadFrontendFile(params string[] parts) =>
        ReadRepoFile(parts);

    private static string ReadRepoFile(params string[] parts)
    {
        var path = Path.Combine(
            parts.Prepend(TestPaths.RepoRoot).ToArray());

        Assert.True(
            File.Exists(path),
            $"Expected file does not exist: {path}");

        return File.ReadAllText(path);
    }
}
