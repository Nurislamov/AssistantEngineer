using AssistantEngineer.Api.Controllers.Calculations;
using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.CoreStatus;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Tests.Calculations.CoreStatus;

public class EngineeringCoreStatusApiAndRegistrationTests
{
    [Fact]
    public void AddCalculationsModuleRegistersEngineeringCoreStatusFacade()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddCalculationsModule(configuration);

        using var provider = services.BuildServiceProvider();

        var facade = provider.GetRequiredService<IEngineeringCoreStatusFacade>();

        Assert.IsType<EngineeringCoreStatusFacade>(facade);
    }

    [Fact]
    public void EngineeringCoreStatusControllerDependsOnlyOnEngineeringCoreStatusFacade()
    {
        var constructor = Assert.Single(typeof(EngineeringCoreStatusController).GetConstructors());
        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal(typeof(IEngineeringCoreStatusFacade), parameter.ParameterType);
    }

    [Fact]
    public void EngineeringCoreStatusControllerExposesVersionedStatusRoute()
    {
        var controllerRoute = typeof(EngineeringCoreStatusController)
            .GetCustomAttributes(inherit: true)
            .OfType<RouteAttribute>()
            .Single();

        Assert.Equal(
            "api/v{version:apiVersion}/calculations/engineering-core",
            controllerRoute.Template);

        var method = typeof(EngineeringCoreStatusController)
            .GetMethod(nameof(EngineeringCoreStatusController.GetEngineeringCoreV1Status));

        Assert.NotNull(method);

        var getAttribute = method
            .GetCustomAttributes(inherit: true)
            .OfType<HttpGetAttribute>()
            .Single();

        Assert.Equal("v1/status", getAttribute.Template);
    }

    [Fact]
    public void EngineeringCoreStatusControllerActionReturnsTypedStatusResponse()
    {
        var method = typeof(EngineeringCoreStatusController)
            .GetMethod(nameof(EngineeringCoreStatusController.GetEngineeringCoreV1Status));

        Assert.NotNull(method);

        Assert.Equal(
            typeof(ActionResult<EngineeringCoreV1StatusResponse>),
            method.ReturnType);
    }
}