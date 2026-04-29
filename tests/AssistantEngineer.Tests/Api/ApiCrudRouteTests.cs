using AssistantEngineer.Api.Controllers.Buildings;
using AssistantEngineer.Api.Controllers.Equipment;
using AssistantEngineer.Api.Controllers.Projects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AssistantEngineer.Tests;

public class ApiCrudRouteTests
{
    [Theory]
    [InlineData(typeof(ProjectsController), nameof(ProjectsController.Update), typeof(HttpPutAttribute), "{id:int}")]
    [InlineData(typeof(ProjectsController), nameof(ProjectsController.Delete), typeof(HttpDeleteAttribute), "{id:int}")]
    [InlineData(typeof(BuildingsController), nameof(BuildingsController.Update), typeof(HttpPutAttribute), "{id:int}")]
    [InlineData(typeof(BuildingsController), nameof(BuildingsController.Delete), typeof(HttpDeleteAttribute), "{id:int}")]
    [InlineData(typeof(FloorsController), nameof(FloorsController.Update), typeof(HttpPutAttribute), "{id:int}")]
    [InlineData(typeof(FloorsController), nameof(FloorsController.Delete), typeof(HttpDeleteAttribute), "{id:int}")]
    [InlineData(typeof(RoomsController), nameof(RoomsController.Update), typeof(HttpPutAttribute), "{id:int}")]
    [InlineData(typeof(RoomsController), nameof(RoomsController.Delete), typeof(HttpDeleteAttribute), "{id:int}")]
    [InlineData(typeof(RoomsController), nameof(RoomsController.UpdateWall), typeof(HttpPutAttribute), "{roomId:int}/walls/{wallId:int}")]
    [InlineData(typeof(RoomsController), nameof(RoomsController.DeleteWall), typeof(HttpDeleteAttribute), "{roomId:int}/walls/{wallId:int}")]
    [InlineData(typeof(RoomsController), nameof(RoomsController.UpdateWindow), typeof(HttpPutAttribute), "{roomId:int}/windows/{windowId:int}")]
    [InlineData(typeof(RoomsController), nameof(RoomsController.DeleteWindow), typeof(HttpDeleteAttribute), "{roomId:int}/windows/{windowId:int}")]
    [InlineData(typeof(EquipmentCatalogController), nameof(EquipmentCatalogController.Update), typeof(HttpPutAttribute), "{id:int}")]
    [InlineData(typeof(EquipmentCatalogController), nameof(EquipmentCatalogController.Delete), typeof(HttpDeleteAttribute), "{id:int}")]
    public void CrudActionsDeclareExpectedRoutes(
        Type controllerType,
        string actionName,
        Type httpAttributeType,
        string routeTemplate)
    {
        var action = controllerType.GetMethod(actionName);
        Assert.NotNull(action);

        var route = action
            .GetCustomAttributes(httpAttributeType, inherit: false)
            .Cast<HttpMethodAttribute>()
            .SingleOrDefault();

        Assert.NotNull(route);
        Assert.Equal(routeTemplate, route.Template);
    }

    [Fact]
    public void BuildingRoomsRouteUsesParentResource()
    {
        var action = typeof(RoomsController).GetMethod(nameof(RoomsController.GetByBuilding));
        Assert.NotNull(action);

        var route = action
            .GetCustomAttributes(typeof(HttpGetAttribute), inherit: false)
            .Cast<HttpMethodAttribute>()
            .SingleOrDefault();

        Assert.NotNull(route);
        Assert.Equal("~/api/v{version:apiVersion}/buildings/{buildingId:int}/rooms", route.Template);
    }
}
