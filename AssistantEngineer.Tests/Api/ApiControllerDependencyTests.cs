using AssistantEngineer.Api;

namespace AssistantEngineer.Tests;

public class ApiControllerDependencyTests
{
    [Fact]
    public void ControllersDependOnApplicationFacades()
    {
        var controllers = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type.Namespace == "AssistantEngineer.Api.Controllers" &&
                type.Name.EndsWith("Controller", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(controllers);

        var violations = controllers
            .SelectMany(controller => controller
                .GetConstructors()
                .SelectMany(constructor => constructor
                    .GetParameters()
                    .Select(parameter => new
                    {
                        Controller = controller.Name,
                        Parameter = parameter.ParameterType.FullName ?? parameter.ParameterType.Name
                    })))
            .Where(item => !item.Parameter.Contains(".Application.Facades.", StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Controllers must depend on application facades only: " +
            string.Join(", ", violations.Select(item => $"{item.Controller} -> {item.Parameter}")));
    }
}
