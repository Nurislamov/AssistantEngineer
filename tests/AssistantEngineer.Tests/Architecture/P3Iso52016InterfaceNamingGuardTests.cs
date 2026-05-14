using AssistantEngineer.Tests;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Tests.Architecture;

public class P3Iso52016InterfaceNamingGuardTests
{
    private const string NamingConventionsPath = "docs/architecture/naming-conventions.md";

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
    public void SourceTreeDoesNotContainLegacyDoubleIPrefixIso52016InterfaceFilenames()
    {
        var sourceRoots = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "src"),
            Path.Combine(TestPaths.RepoRoot, "tests")
        };

        var legacyNamedFiles = sourceRoots
            .SelectMany(root => Directory.EnumerateFiles(root, "*IIso52016*.cs", SearchOption.AllDirectories))
            .ToArray();

        Assert.Empty(legacyNamedFiles);
    }

    [Fact]
    public void Iso52016InterfaceDeclarationsUseNormalizedISo52016Prefix()
    {
        var interfaceDeclarationPattern = new Regex(
            @"\binterface\s+(?<name>I\w*Iso52016\w*)\b",
            RegexOptions.CultureInvariant);

        var sourceFiles = Directory.EnumerateFiles(
            Path.Combine(TestPaths.RepoRoot, "src"),
            "*.cs",
            SearchOption.AllDirectories);

        var violations = new List<string>();

        foreach (var file in sourceFiles)
        {
            var source = File.ReadAllText(file);
            var matches = interfaceDeclarationPattern.Matches(source);

            foreach (Match match in matches)
            {
                var interfaceName = match.Groups["name"].Value;
                if (!interfaceName.StartsWith("ISo52016", StringComparison.Ordinal) &&
                    interfaceName.IndexOf("Iso52016", StringComparison.Ordinal) == 1)
                {
                    violations.Add($"{file}: {interfaceName}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            $"ISO52016 interface naming must follow ISo52016* per {NamingConventionsPath}. Violations: {string.Join("; ", violations)}");
    }

    [Fact]
    public void NamingConventionsDocumentDeclaresIso52016InterfaceRule()
    {
        var path = Path.Combine(TestPaths.RepoRoot, NamingConventionsPath.Replace('/', Path.DirectorySeparatorChar));
        var content = File.ReadAllText(path);

        Assert.Contains("ISo52016", content, StringComparison.Ordinal);
        Assert.Contains("IIso52016", content, StringComparison.Ordinal);
    }
}
