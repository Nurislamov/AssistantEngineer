using AssistantEngineer.Api.Contracts.Buildings;
using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/rooms")]
public class RoomsController : ControllerBase
{
    private readonly IBuildingsFacade _buildings;
    private readonly ICalculationsFacade _calculations;
    private readonly IEquipmentFacade _equipment;

    public RoomsController(
        IBuildingsFacade buildings,
        ICalculationsFacade calculations,
        IEquipmentFacade equipment)
    {
        _buildings = buildings;
        _calculations = calculations;
        _equipment = equipment;
    }

    [HttpPost]
    public async Task<ActionResult<RoomResponse>> Create(
        [FromBody] CreateRoomRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.CreateRoomAsync(request, cancellationToken);
        return result.ToCreatedResult(this, nameof(GetById), room => room.Id);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<RoomResponse>>> GetAll(
        [FromQuery] RoomListQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetRoomsAsync(cancellationToken);
        if (result.IsFailure)
            return ApiProblemDetailsFactory.CreateResult(HttpContext, result);

        IEnumerable<RoomResponse> items = result.Value;

        if (query.FloorId.HasValue)
            items = items.Where(room => room.FloorId == query.FloorId.Value);

        if (query.Type.HasValue)
            items = items.Where(room => room.Type == query.Type.Value);

        items = SortRooms(
            items.ApplySearch(query.Search, room => room.Name, room => room.Type.ToString()),
            query);

        return Ok(items.ToPagedResponse(query));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetRoomByIdAsync(id, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:int}/cooling-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<RoomCalculationResult>> CalculateCoolingLoad(
        int id,
        [FromQuery] CoolingLoadCalculationMethodDto method = CoolingLoadCalculationMethodDto.Simplified,
        CancellationToken cancellationToken = default)
    {
        var result = await _calculations.CalculateRoomCoolingLoadAsync(id, method, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:int}/heating-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<RoomHeatingLoadResult>> CalculateHeatingLoad(
        int id,
        [FromQuery] HeatingLoadCalculationMethodDto method = HeatingLoadCalculationMethodDto.En12831,
        CancellationToken cancellationToken = default)
    {
        var result = await _calculations.CalculateRoomHeatingLoadAsync(id, method, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id:int}/windows")]
    public async Task<ActionResult<WindowResponse>> AddWindow(
        int id,
        [FromBody] CreateWindowRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.AddWindowAsync(id, request, cancellationToken);
        return result.ToOkResult(this);
    }

    [HttpPost("{id:int}/walls")]
    public async Task<ActionResult<WallResponse>> AddWall(
        int id,
        [FromBody] CreateWallRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.AddWallAsync(id, request, cancellationToken);
        return result.ToOkResult(this);
    }

    [HttpPost("{id:int}/select-equipment")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<EquipmentSelectionResult>> SelectEquipment(
        int id,
        [FromBody] EquipmentSelectionRequest request,
        [FromQuery] CoolingLoadCalculationMethodDto method = CoolingLoadCalculationMethodDto.Simplified,
        CancellationToken cancellationToken = default)
    {
        var result = await _equipment.SelectRoomEquipmentAsync(
            id,
            request,
            method,
            cancellationToken);

        return result.ToOkResult(this);
    }

    [HttpGet("{id:int}/windows")]
    public async Task<ActionResult<PagedResponse<WindowResponse>>> GetWindows(
        int id,
        [FromQuery] WindowListQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetRoomWindowsAsync(id, cancellationToken);
        if (result.IsFailure)
            return ApiProblemDetailsFactory.CreateResult(HttpContext, result);

        IEnumerable<WindowResponse> items = result.Value;

        if (query.Orientation.HasValue)
            items = items.Where(window => window.Orientation == query.Orientation.Value);

        items = SortWindows(
            items.ApplySearch(query.Search, window => window.Orientation.ToString(), window => window.Id.ToString()),
            query);

        return Ok(items.ToPagedResponse(query));
    }

    [HttpGet("{id:int}/walls")]
    public async Task<ActionResult<PagedResponse<WallResponse>>> GetWalls(
        int id,
        [FromQuery] WallListQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetRoomWallsAsync(id, cancellationToken);
        if (result.IsFailure)
            return ApiProblemDetailsFactory.CreateResult(HttpContext, result);

        IEnumerable<WallResponse> items = result.Value;

        if (query.Orientation.HasValue)
            items = items.Where(wall => wall.Orientation == query.Orientation.Value);

        if (query.BoundaryType.HasValue)
            items = items.Where(wall => wall.BoundaryType == query.BoundaryType.Value);

        if (query.IsExternal.HasValue)
            items = items.Where(wall => wall.IsExternal == query.IsExternal.Value);

        items = SortWalls(
            items.ApplySearch(
                query.Search,
                wall => wall.Orientation.ToString(),
                wall => wall.BoundaryType.ToString(),
                wall => wall.Id.ToString()),
            query);

        return Ok(items.ToPagedResponse(query));
    }

    private static IEnumerable<RoomResponse> SortRooms(
        IEnumerable<RoomResponse> source,
        CollectionQueryParameters query) =>
        (query.SortBy ?? "id").ToLowerInvariant() switch
        {
            "name" => query.SortDescending ? source.OrderByDescending(room => room.Name).ThenByDescending(room => room.Id) : source.OrderBy(room => room.Name).ThenBy(room => room.Id),
            "aream2" => query.SortDescending ? source.OrderByDescending(room => room.AreaM2).ThenByDescending(room => room.Id) : source.OrderBy(room => room.AreaM2).ThenBy(room => room.Id),
            "volumem3" => query.SortDescending ? source.OrderByDescending(room => room.VolumeM3).ThenByDescending(room => room.Id) : source.OrderBy(room => room.VolumeM3).ThenBy(room => room.Id),
            "indoortemperaturec" => query.SortDescending ? source.OrderByDescending(room => room.IndoorTemperatureC).ThenByDescending(room => room.Id) : source.OrderBy(room => room.IndoorTemperatureC).ThenBy(room => room.Id),
            "peoplecount" => query.SortDescending ? source.OrderByDescending(room => room.PeopleCount).ThenByDescending(room => room.Id) : source.OrderBy(room => room.PeopleCount).ThenBy(room => room.Id),
            "type" => query.SortDescending ? source.OrderByDescending(room => room.Type).ThenByDescending(room => room.Id) : source.OrderBy(room => room.Type).ThenBy(room => room.Id),
            _ => query.SortDescending ? source.OrderByDescending(room => room.Id) : source.OrderBy(room => room.Id)
        };

    private static IEnumerable<WindowResponse> SortWindows(
        IEnumerable<WindowResponse> source,
        CollectionQueryParameters query) =>
        (query.SortBy ?? "id").ToLowerInvariant() switch
        {
            "aream2" => query.SortDescending ? source.OrderByDescending(window => window.AreaM2).ThenByDescending(window => window.Id) : source.OrderBy(window => window.AreaM2).ThenBy(window => window.Id),
            "uvalue" => query.SortDescending ? source.OrderByDescending(window => window.UValue).ThenByDescending(window => window.Id) : source.OrderBy(window => window.UValue).ThenBy(window => window.Id),
            "shgc" => query.SortDescending ? source.OrderByDescending(window => window.Shgc).ThenByDescending(window => window.Id) : source.OrderBy(window => window.Shgc).ThenBy(window => window.Id),
            "orientation" => query.SortDescending ? source.OrderByDescending(window => window.Orientation).ThenByDescending(window => window.Id) : source.OrderBy(window => window.Orientation).ThenBy(window => window.Id),
            _ => query.SortDescending ? source.OrderByDescending(window => window.Id) : source.OrderBy(window => window.Id)
        };

    private static IEnumerable<WallResponse> SortWalls(
        IEnumerable<WallResponse> source,
        CollectionQueryParameters query) =>
        (query.SortBy ?? "id").ToLowerInvariant() switch
        {
            "aream2" => query.SortDescending ? source.OrderByDescending(wall => wall.AreaM2).ThenByDescending(wall => wall.Id) : source.OrderBy(wall => wall.AreaM2).ThenBy(wall => wall.Id),
            "uvalue" => query.SortDescending ? source.OrderByDescending(wall => wall.UValue).ThenByDescending(wall => wall.Id) : source.OrderBy(wall => wall.UValue).ThenBy(wall => wall.Id),
            "orientation" => query.SortDescending ? source.OrderByDescending(wall => wall.Orientation).ThenByDescending(wall => wall.Id) : source.OrderBy(wall => wall.Orientation).ThenBy(wall => wall.Id),
            "boundarytype" => query.SortDescending ? source.OrderByDescending(wall => wall.BoundaryType).ThenByDescending(wall => wall.Id) : source.OrderBy(wall => wall.BoundaryType).ThenBy(wall => wall.Id),
            "isexternal" => query.SortDescending ? source.OrderByDescending(wall => wall.IsExternal).ThenByDescending(wall => wall.Id) : source.OrderBy(wall => wall.IsExternal).ThenBy(wall => wall.Id),
            _ => query.SortDescending ? source.OrderByDescending(wall => wall.Id) : source.OrderBy(wall => wall.Id)
        };
}
