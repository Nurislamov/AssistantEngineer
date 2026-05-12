using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P3BuildingInputValidationRefactorGuardTests
{
    private const int BuildingInputValidationServiceMaxLines = 550;

    [Fact]
    public void BuildingInputValidationService_RemainsFocusedFacade()
    {
        var servicePath = FindRequiredSourceFile("BuildingInputValidationService.cs");
        var lineCount = File.ReadAllLines(servicePath).Length;

        Assert.True(
            lineCount <= BuildingInputValidationServiceMaxLines,
            $"BuildingInputValidationService.cs should remain a focused facade after P3-13. " +
            $"Current line count: {lineCount}. Limit: {BuildingInputValidationServiceMaxLines}.");
    }

    [Fact]
    public void P3_13_RefactorIntroducesFocusedValidationComponents()
    {
        var servicePath = FindRequiredSourceFile("BuildingInputValidationService.cs");
        var serviceDirectory = Path.GetDirectoryName(servicePath)!;
        var sourceFiles = Directory
            .GetFiles(serviceDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !Path.GetFileName(path).Equals("BuildingInputValidationService.cs", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray();

        var expectedDomains = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["room"] = new[] { "Room" },
            ["envelope"] = new[] { "Envelope", "Opaque", "Wall" },
            ["window"] = new[] { "Window", "Transparent", "Opening" },
            ["ventilation"] = new[] { "Ventilation", "Hvac", "Air" },
            ["boundary"] = new[] { "Boundary", "Ground", "Adjacent" },
            ["diagnostics"] = new[] { "Diagnostic", "Issue", "Severity", "Rule" }
        };

        var matchedDomains = expectedDomains
            .Where(domain => sourceFiles.Any(file => domain.Value.Any(token => file.Contains(token, StringComparison.OrdinalIgnoreCase))))
            .Select(domain => domain.Key)
            .ToArray();

        Assert.True(
            matchedDomains.Length >= 4,
            "P3-13 should extract focused validation components/helpers. " +
            $"Matched domains: {string.Join(", ", matchedDomains)}. " +
            $"Candidate files: {string.Join(", ", sourceFiles.OrderBy(x => x))}.");
    }

    [Fact]
    public void P3_13_StatusDocumentIsPresentAndHonest()
    {
        var repoRoot = FindRepositoryRoot();
        var docPath = Path.Combine(repoRoot, "docs", "architecture", "p3-building-input-validation-refactor-status.md");

        Assert.True(File.Exists(docPath), "P3-13 status document should exist.");

        var content = File.ReadAllText(docPath);
        Assert.Contains("P3-13", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Building Input Validation", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No calculation physics changes", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No public API route changes", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("full ISO", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("full production", content, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRequiredSourceFile(string fileName)
    {
        var repoRoot = FindRepositoryRoot();
        var matches = Directory
            .GetFiles(Path.Combine(repoRoot, "src"), fileName, SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(matches.Length == 1, $"Expected exactly one {fileName} under src, found {matches.Length}: {string.Join(", ", matches)}");
        return matches[0];
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AssistantEngineer.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test output directory.");
    }
}