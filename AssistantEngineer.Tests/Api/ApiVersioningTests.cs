using AssistantEngineer.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Tests;

public class ApiVersioningTests
{
    [Theory]
    [InlineData(typeof(BenchmarksController), "api/v1/benchmarks", "api/benchmarks")]
    [InlineData(typeof(BuildingsController), "api/v1/buildings", "api/buildings")]
    [InlineData(typeof(EquipmentCatalogController), "api/v1/equipment-catalog", "api/equipment-catalog")]
    [InlineData(typeof(FloorsController), "api/v1/floors", "api/floors")]
    [InlineData(typeof(ProjectsController), "api/v1/projects", "api/projects")]
    [InlineData(typeof(ReportsController), "api/v1/reports", "api/reports")]
    [InlineData(typeof(RoomsController), "api/v1/rooms", "api/rooms")]
    public void ControllersExposeVersionedRouteAndCompatibilityRoute(
        Type controllerType,
        string versionedRoute,
        string compatibilityRoute)
    {
        var routes = controllerType
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();

        Assert.Contains(versionedRoute, routes);
        Assert.Contains(compatibilityRoute, routes);
    }
}
