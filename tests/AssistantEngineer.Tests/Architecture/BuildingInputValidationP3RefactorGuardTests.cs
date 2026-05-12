using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AssistantEngineer.Tests.Architecture;

public sealed class BuildingInputValidationP3RefactorGuardTests
{
    [Fact]
    public void BuildingInputValidationService_ShouldRemainFocusedFacade()
    {
        var root = FindRepositoryRoot();
        var file = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "BuildingInputValidationService.cs", SearchOption.AllDirectories)
            .Single();

        var lines = File.ReadAllLines(file);

        Assert.True(
            lines.Length <= 550,
            $"BuildingInputValidationService.cs should remain below 550 lines after P3-13 refactor. Actual: {lines.Length}.");
    }

    [Fact]
    public void BuildingInputValidation_ShouldHaveFocusedValidatorComponents()
    {
        var root = FindRepositoryRoot();
        var sourceFileNames = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains("Validation", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray();

        Assert.Contains(sourceFileNames, name => name.Contains("Room", StringComparison.OrdinalIgnoreCase) && name.Contains("Validator", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(sourceFileNames, name => name.Contains("Envelope", StringComparison.OrdinalIgnoreCase) && name.Contains("Validator", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(sourceFileNames, name => name.Contains("Ventilation", StringComparison.OrdinalIgnoreCase) && name.Contains("Validator", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(sourceFileNames, name => name.Contains("Diagnostic", StringComparison.OrdinalIgnoreCase) && (name.Contains("Factory", StringComparison.OrdinalIgnoreCase) || name.Contains("Builder", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void BuildingInputValidationRefactor_ShouldNotIntroducePublicApiRouteMarkers()
    {
        var root = FindRepositoryRoot();
        var controllerFiles = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*Controller.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("BuildingInputValidation", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var controllerFile in controllerFiles)
        {
            var text = File.ReadAllText(controllerFile);

            Assert.DoesNotContain("p3-13", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("refactor", text, StringComparison.OrdinalIgnoreCase);
        }
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

        throw new InvalidOperationException("Repository root was not found.");
    }
}