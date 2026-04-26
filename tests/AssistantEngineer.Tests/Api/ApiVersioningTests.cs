using AssistantEngineer.Api.Controllers.Analysis;
using AssistantEngineer.Api.Controllers.Benchmarks;
using AssistantEngineer.Api.Controllers.Buildings;
using AssistantEngineer.Api.Controllers.Calculations;
using AssistantEngineer.Api.Controllers.Equipment;
using AssistantEngineer.Api.Controllers.Profiles;
using AssistantEngineer.Api.Controllers.Projects;
using AssistantEngineer.Api.Controllers.ReferenceData;
using AssistantEngineer.Api.Controllers.Reports;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AssistantEngineer.Tests;

public class ApiVersioningTests
{
    [Theory]
    [InlineData(typeof(BenchmarksController), "api/v{version:apiVersion}/benchmarks")]
    [InlineData(typeof(BuildingArchetypesController), "api/v{version:apiVersion}/building-archetypes")]
    [InlineData(typeof(BuildingComfortAnalysisController), "api/v{version:apiVersion}/buildings/{buildingId:int}/comfort-analysis")]
    [InlineData(typeof(BuildingEnergyAnalysisController), "api/v{version:apiVersion}/buildings/{buildingId:int}/energy-analysis")]
    [InlineData(typeof(BuildingSizingAnalysisController), "api/v{version:apiVersion}/buildings/{buildingId:int}/sizing-analysis")]
    [InlineData(typeof(BuildingReadinessController), "api/v{version:apiVersion}/buildings/{buildingId:int}/readiness")]
    [InlineData(typeof(BuildingValidationController), "api/v{version:apiVersion}/buildings/{buildingId:int}/validation")]
    [InlineData(typeof(BuildingsController), "api/v{version:apiVersion}/buildings")]
    [InlineData(typeof(AnnualClimateDataController), "api/v{version:apiVersion}/climate-zones/{climateZoneId:int}/annual-climate-data")]
    [InlineData(typeof(BuildingLoadCalculationsController), "api/v{version:apiVersion}/buildings/{buildingId:int}/load-calculations")]
    [InlineData(typeof(DomesticHotWaterController), "api/v{version:apiVersion}/domestic-hot-water")]
    [InlineData(typeof(FloorLoadCalculationsController), "api/v{version:apiVersion}/floors/{floorId:int}/load-calculations")]
    [InlineData(typeof(GroundTemperatureController), "api/v{version:apiVersion}/buildings/{buildingId:int}/ground-temperature")]
    [InlineData(typeof(RoomLoadCalculationsController), "api/v{version:apiVersion}/rooms/{roomId:int}/load-calculations")]
    [InlineData(typeof(EquipmentCatalogController), "api/v{version:apiVersion}/equipment-catalog")]
    [InlineData(typeof(RoomEquipmentSelectionController), "api/v{version:apiVersion}/rooms/{roomId:int}/equipment-selection")]
    [InlineData(typeof(FloorsController), "api/v{version:apiVersion}/floors")]
    [InlineData(typeof(AnnualProfilesController), "api/v{version:apiVersion}/profiles/annual")]
    [InlineData(typeof(StandardProfilesController), "api/v{version:apiVersion}/standard-profiles/en16798")]
    [InlineData(typeof(ProjectsController), "api/v{version:apiVersion}/projects")]
    [InlineData(typeof(StandardTablesController), "api/v{version:apiVersion}/standard-tables")]
    [InlineData(typeof(BuildingCoolingReportsController), "api/v{version:apiVersion}/reports/buildings/{buildingId:int}/cooling")]
    [InlineData(typeof(BuildingEnergyBalanceReportsController), "api/v{version:apiVersion}/reports/buildings/{buildingId:int}/energy-balance")]
    [InlineData(typeof(BuildingHeatingReportsController), "api/v{version:apiVersion}/reports/buildings/{buildingId:int}/heating")]
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

    [Theory]
    [InlineData(
        typeof(RoomGroundContactController),
        nameof(RoomGroundContactController.Get),
        "api/v{version:apiVersion}/rooms/{roomId:int}/ground-contact")]
    [InlineData(
        typeof(RoomVentilationController),
        nameof(RoomVentilationController.Get),
        "api/v{version:apiVersion}/rooms/{roomId:int}/ventilation-parameters")]
    [InlineData(
        typeof(ThermalZonesController),
        nameof(ThermalZonesController.GetByBuilding),
        "api/v{version:apiVersion}/buildings/{buildingId:int}/thermal-zones")]
    public void ActionLevelRoutedControllersDeclareVersionedRouteOnActions(
        Type controllerType,
        string actionName,
        string versionedRoute)
    {
        var action = controllerType.GetMethod(actionName);
        Assert.NotNull(action);

        var routes = action
            .GetCustomAttributes(typeof(HttpMethodAttribute), inherit: false)
            .Cast<HttpMethodAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();

        Assert.Contains(versionedRoute, routes);
        Assert.DoesNotContain(routes, route => route is not null && !route.Contains("{version:apiVersion}"));
    }

    [Theory]
    [InlineData(
        typeof(BuildingsController),
        nameof(BuildingsController.Create),
        "~/api/v{version:apiVersion}/projects/{projectId:int}/buildings")]
    [InlineData(
        typeof(BuildingsController),
        nameof(BuildingsController.GetByProject),
        "~/api/v{version:apiVersion}/projects/{projectId:int}/buildings")]
    [InlineData(
        typeof(BuildingsController),
        nameof(BuildingsController.CreateFromArchetype),
        "~/api/v{version:apiVersion}/projects/{projectId:int}/buildings/from-archetype")]
    [InlineData(
        typeof(FloorsController),
        nameof(FloorsController.Create),
        "~/api/v{version:apiVersion}/buildings/{buildingId:int}/floors")]
    [InlineData(
        typeof(FloorsController),
        nameof(FloorsController.GetByBuilding),
        "~/api/v{version:apiVersion}/buildings/{buildingId:int}/floors")]
    public void NestedResourceActionsUseParentFirstRoutes(
        Type controllerType,
        string actionName,
        string routeTemplate)
    {
        var action = controllerType.GetMethod(actionName);
        Assert.NotNull(action);

        var routes = action
            .GetCustomAttributes(typeof(HttpMethodAttribute), inherit: false)
            .Cast<HttpMethodAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();

        Assert.Contains(routeTemplate, routes);
    }
}
