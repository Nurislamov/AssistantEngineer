using AssistantEngineer.Api.Controllers;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Tests;

public class ApiVersioningTests
{
    [Theory]
    [InlineData(typeof(BenchmarksController), "api/v{version:apiVersion}/benchmarks")]
    [InlineData(typeof(BuildingArchetypesController), "api/v{version:apiVersion}/building-performance")]
    [InlineData(typeof(BuildingPerformanceController), "api/v{version:apiVersion}/building-performance")]
    [InlineData(typeof(BuildingsController), "api/v{version:apiVersion}/buildings")]
    [InlineData(typeof(ClimateDataController), "api/v{version:apiVersion}/climate-zones")]
    [InlineData(typeof(EquipmentCatalogController), "api/v{version:apiVersion}/equipment-catalog")]
    [InlineData(typeof(FloorsController), "api/v{version:apiVersion}/floors")]
    [InlineData(typeof(ProjectsController), "api/v{version:apiVersion}/projects")]
    [InlineData(typeof(ReportsController), "api/v{version:apiVersion}/reports")]
    [InlineData(typeof(RoomsController), "api/v{version:apiVersion}/rooms")]
    public void ControllersDeclareExplicitV1UrlSegmentRoute(
        Type controllerType,
        string versionedRoute)
    {
        var routes = controllerType
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();

        Assert.Contains(versionedRoute, routes);
        Assert.DoesNotContain(routes, route => route is not null && !route.Contains("{version:apiVersion}"));

        var apiVersions = controllerType
            .GetCustomAttributes(typeof(ApiVersionAttribute), inherit: false)
            .Cast<ApiVersionAttribute>()
            .SelectMany(attribute => attribute.Versions)
            .ToArray();

        Assert.Contains(apiVersions, version => version.MajorVersion == 1 && version.MinorVersion == 0);
    }
}
