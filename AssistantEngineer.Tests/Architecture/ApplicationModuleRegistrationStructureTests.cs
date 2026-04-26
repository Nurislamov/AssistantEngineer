using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class ApplicationModuleRegistrationStructureTests
{
    [Fact]
    public void ApplicationModuleRegistrationIsThinOrchestrator()
    {
        var type = typeof(Program).Assembly.GetType(
            "AssistantEngineer.Api.Configuration.ApplicationModuleRegistration");

        Assert.NotNull(type);

        var declaredMethods = type
            .GetMethods()
            .Where(method =>
                method.DeclaringType == type &&
                !method.IsSpecialName)
            .Select(method => method.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "AddAssistantEngineerModules"
            ],
            declaredMethods);
    }

    [Fact]
    public void ApplicationModuleRegistrationDoesNotRegisterConcreteModulesDirectly()
    {
        var apiProjectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "AssistantEngineer.Api"));

        var registrationPath = Path.Combine(
            apiProjectPath,
            "Configuration",
            "ApplicationModuleRegistration.cs");

        var text = File.ReadAllText(registrationPath);

        var forbiddenFragments = new[]
        {
            "AddBuildingsModule(",
            "AddCalculationsModule(",
            "AddEquipmentModule(",
            "AddReportingModule(",
            "AddBenchmarksModule(",
            "AddInfrastructure("
        };

        var violations = forbiddenFragments
            .Where(fragment =>
                text.Contains(fragment, StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"ApplicationModuleRegistration must orchestrate module groups, not register concrete modules directly: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void ApplicationModulesRegistrationOwnsApplicationModuleRegistration()
    {
        var apiProjectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "AssistantEngineer.Api"));

        var registrationPath = Path.Combine(
            apiProjectPath,
            "Configuration",
            "ApplicationModulesRegistration.cs");

        var text = File.ReadAllText(registrationPath);

        var expectedFragments = new[]
        {
            "AddBuildingsModule(",
            "AddCalculationsModule(",
            "AddEquipmentModule(",
            "AddReportingModule(",
            "AddBenchmarksModule("
        };

        foreach (var expectedFragment in expectedFragments)
        {
            Assert.Contains(
                expectedFragment,
                text,
                StringComparison.Ordinal);
        }

        Assert.DoesNotContain(
            "AddInfrastructure(",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void InfrastructureAdaptersRegistrationOwnsInfrastructureRegistration()
    {
        var apiProjectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "AssistantEngineer.Api"));

        var registrationPath = Path.Combine(
            apiProjectPath,
            "Configuration",
            "InfrastructureAdaptersRegistration.cs");

        var text = File.ReadAllText(registrationPath);

        Assert.Contains(
            "AddInfrastructure(",
            text,
            StringComparison.Ordinal);

        var forbiddenFragments = new[]
        {
            "AddBuildingsModule(",
            "AddCalculationsModule(",
            "AddEquipmentModule(",
            "AddReportingModule(",
            "AddBenchmarksModule("
        };

        var violations = forbiddenFragments
            .Where(fragment =>
                text.Contains(fragment, StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"InfrastructureAdaptersRegistration must not register application modules: {string.Join(", ", violations)}.");
    }
}