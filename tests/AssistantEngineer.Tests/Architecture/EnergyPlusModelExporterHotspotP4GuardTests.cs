namespace AssistantEngineer.Tests.Architecture;

public sealed class EnergyPlusModelExporterHotspotP4GuardTests
{
    [Fact]
    public void EnergyPlusModelExporter_RemainsFacadeWithSectionBuilders()
    {
        var exporterPath = Resolve("src/Backend/AssistantEngineer.Infrastructure/Integrations/Benchmarks/EnergyPlusModelExporter.cs");
        Assert.True(File.Exists(exporterPath), $"Exporter file is missing: {exporterPath}");

        var lineCount = File.ReadAllLines(exporterPath).Length;
        Assert.True(
            lineCount <= 600,
            $"EnergyPlusModelExporter should remain a facade after P4 decomposition (actual: {lineCount}, allowed: 600).");

        var sectionBuilderFiles = new[]
        {
            "src/Backend/AssistantEngineer.Infrastructure/Integrations/Benchmarks/EnergyPlusModelExporter.HeaderSectionBuilder.cs",
            "src/Backend/AssistantEngineer.Infrastructure/Integrations/Benchmarks/EnergyPlusModelExporter.ScheduleSectionBuilder.cs",
            "src/Backend/AssistantEngineer.Infrastructure/Integrations/Benchmarks/EnergyPlusModelExporter.GeometrySectionBuilder.cs",
            "src/Backend/AssistantEngineer.Infrastructure/Integrations/Benchmarks/EnergyPlusModelExporter.WindowSectionBuilder.cs",
            "src/Backend/AssistantEngineer.Infrastructure/Integrations/Benchmarks/EnergyPlusModelExporter.InternalGainsSectionBuilder.cs",
            "src/Backend/AssistantEngineer.Infrastructure/Integrations/Benchmarks/EnergyPlusModelExporter.VentilationSectionBuilder.cs",
            "src/Backend/AssistantEngineer.Infrastructure/Integrations/Benchmarks/EnergyPlusModelExporter.IdealLoadsSectionBuilder.cs",
            "src/Backend/AssistantEngineer.Infrastructure/Integrations/Benchmarks/EnergyPlusModelExporter.OutputSectionBuilder.cs"
        };

        foreach (var relativePath in sectionBuilderFiles)
        {
            var fullPath = Resolve(relativePath);
            Assert.True(File.Exists(fullPath), $"Expected section builder file to exist: {relativePath}");
        }
    }

    private static string Resolve(string relativePath) =>
        Path.Combine(relativePath.Split('/').Prepend(TestPaths.RepoRoot).ToArray());
}
