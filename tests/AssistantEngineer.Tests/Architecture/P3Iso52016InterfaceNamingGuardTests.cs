using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Architecture;

public class P3Iso52016InterfaceNamingGuardTests
{
    [Fact]
    public void SourceAndTestsDoNotContainLegacyDoubleIPrefixIso52016InterfaceNames()
    {
        var productionSourceFiles = Directory.EnumerateFiles(
            Path.Combine(TestPaths.RepoRoot, "src"),
            "*.cs",
            SearchOption.AllDirectories);

        var testSourceFiles = Directory
            .EnumerateFiles(
                Path.Combine(TestPaths.RepoRoot, "tests"),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(path =>
                !string.Equals(
                    Path.GetFileName(path),
                    "P3Iso52016InterfaceNamingGuardTests.cs",
                    StringComparison.OrdinalIgnoreCase));

        var filesToInspect = productionSourceFiles.Concat(testSourceFiles);

        foreach (var file in filesToInspect)
        {
            var source = File.ReadAllText(file);
            Assert.DoesNotContain("interface IIso52016", source, StringComparison.Ordinal);
            Assert.DoesNotContain("IIso52016", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void NoIso52016InterfaceFileUsesLegacyDoubleIPrefix()
    {
        var legacyNamedFiles = Directory
            .EnumerateFiles(
                Path.Combine(
                    TestPaths.RepoRoot,
                    "src",
                    "Backend",
                    "AssistantEngineer.Modules.Calculations"),
                "IIso52016*.cs",
                SearchOption.AllDirectories)
            .ToArray();

        Assert.Empty(legacyNamedFiles);
    }
}
