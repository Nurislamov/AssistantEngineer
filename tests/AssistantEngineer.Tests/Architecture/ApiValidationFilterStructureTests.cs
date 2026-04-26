using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class ApiValidationFilterStructureTests
{
    [Fact]
    public void ValidationFilterIsThinOrchestrator()
    {
        var type = typeof(Program).Assembly.GetType(
            "AssistantEngineer.Api.Filters.ValidationFilter");

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
                "OnActionExecutionAsync"
            ],
            declaredMethods);
    }

    [Fact]
    public void ValidationComponentsLiveInValidationNamespace()
    {
        var expectedTypes = new[]
        {
            "AssistantEngineer.Api.Filters.Validation.ActionArgumentEnumValidator",
            "AssistantEngineer.Api.Filters.Validation.FluentValidationActionArgumentValidator",
            "AssistantEngineer.Api.Filters.Validation.ValidationArgumentMetadata",
            "AssistantEngineer.Api.Filters.Validation.ValidationProblemResultFactory"
        };

        var typeNames = typeof(Program).Assembly
            .GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var expectedType in expectedTypes)
        {
            Assert.Contains(
                expectedType,
                typeNames);
        }
    }

    [Fact]
    public void ValidationFilterDoesNotContainLowLevelValidationDetails()
    {
        var apiProjectPath = global::AssistantEngineer.Tests.TestPaths.ApiProjectPath;

        var validationFilterPath = Path.Combine(
            apiProjectPath,
            "Filters",
            "ValidationFilter.cs");

        var text = File.ReadAllText(validationFilterPath);

        var forbiddenFragments = new[]
        {
            "ConcurrentDictionary",
            "IValidator<>",
            "ValidationContext<>",
            "Activator.CreateInstance",
            "Enum.IsDefined",
            "AddModelError("
        };

        var violations = forbiddenFragments
            .Where(fragment =>
                text.Contains(fragment, StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"ValidationFilter must delegate low-level validation details: {string.Join(", ", violations)}.");
    }
}