namespace AssistantEngineer.Tests.Calculations.Ventilation;

public sealed class VentilationHotspotArchitectureTests
{
    [Fact]
    public void VentilationHotspotFacadesStayWithinExtractionThresholds()
    {
        var ventilationEnginePath = Resolve(
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ventilation/VentilationAndInfiltrationLoadEngine.cs");
        var zoneCalculatorPath = Resolve(
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ventilation/NaturalVentilationZoneLoadCalculator.cs");

        Assert.True(File.Exists(ventilationEnginePath), $"Ventilation engine was not found: {ventilationEnginePath}");
        Assert.True(File.Exists(zoneCalculatorPath), $"Natural ventilation zone calculator was not found: {zoneCalculatorPath}");

        var ventilationEngineLines = File.ReadAllLines(ventilationEnginePath).Length;
        var zoneCalculatorLines = File.ReadAllLines(zoneCalculatorPath).Length;

        Assert.True(
            ventilationEngineLines <= 260,
            $"VentilationAndInfiltrationLoadEngine exceeded size threshold (actual: {ventilationEngineLines}, allowed: 260).");
        Assert.True(
            zoneCalculatorLines <= 260,
            $"NaturalVentilationZoneLoadCalculator exceeded size threshold (actual: {zoneCalculatorLines}, allowed: 260).");
    }

    [Fact]
    public void VentilationScenarioHelpersExistAndRemainInternal()
    {
        var helperFiles = new[]
        {
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ventilation/VentilationInputNormalizer.cs",
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ventilation/MechanicalVentilationScenarioEvaluator.cs",
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ventilation/NaturalVentilationScenarioEvaluator.cs",
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ventilation/InfiltrationScenarioEvaluator.cs",
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ventilation/VentilationDiagnosticsBuilder.cs",
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ventilation/NaturalVentilationZoneInputNormalizer.cs",
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ventilation/NaturalVentilationZoneResultAggregator.cs",
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ventilation/NaturalVentilationZoneDiagnosticsBuilder.cs"
        };

        foreach (var relativePath in helperFiles)
        {
            var fullPath = Resolve(relativePath);
            Assert.True(File.Exists(fullPath), $"Expected helper file to exist: {relativePath}");

            var content = File.ReadAllText(fullPath);
            Assert.Contains("internal ", content, StringComparison.Ordinal);
            Assert.DoesNotContain("public class", content, StringComparison.Ordinal);
            Assert.DoesNotContain("public static class", content, StringComparison.Ordinal);
            Assert.DoesNotContain("public sealed class", content, StringComparison.Ordinal);
        }
    }

    private static string Resolve(string relativePath) =>
        Path.Combine(relativePath.Split('/').Prepend(TestPaths.RepoRoot).ToArray());
}
