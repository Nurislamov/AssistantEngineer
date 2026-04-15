using System.Net.Http.Json;
using System.Text.Json;
using AssistantEngineer.Contracts.Requests;
using AssistantEngineer.Contracts.Responses;

namespace AssistantEngineer.Tests.Integration;

internal static class ApiTestClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<ProjectResponse> CreateProjectAsync(
        this HttpClient client,
        string name = "Integration Project")
    {
        var response = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest
        {
            Name = name
        });

        response.EnsureSuccessStatusCode();
        return await ReadRequiredAsync<ProjectResponse>(response);
    }

    public static async Task<BuildingResponse> CreateBuildingAsync(
        this HttpClient client,
        int projectId,
        string name = "Integration Building")
    {
        var response = await client.PostAsJsonAsync($"/api/buildings/{projectId}", new CreateBuildingRequest
        {
            Name = name
        });

        response.EnsureSuccessStatusCode();
        return await ReadRequiredAsync<BuildingResponse>(response);
    }

    public static async Task<FloorResponse> CreateFloorAsync(
        this HttpClient client,
        int buildingId,
        string name = "Integration Floor")
    {
        var response = await client.PostAsJsonAsync($"/api/floors/{buildingId}", new CreateFloorRequest
        {
            Name = name
        });

        response.EnsureSuccessStatusCode();
        return await ReadRequiredAsync<FloorResponse>(response);
    }

    public static async Task<RoomResponse> CreateRoomAsync(
        this HttpClient client,
        int floorId,
        string name = "Integration Room",
        double areaM2 = 24,
        double heightM = 3,
        double indoorTemperatureC = 24,
        double outdoorTemperatureC = 38,
        int peopleCount = 4,
        double equipmentLoadW = 900,
        double lightingLoadW = 320)
    {
        var response = await client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest
        {
            Name = name,
            AreaM2 = areaM2,
            HeightM = heightM,
            IndoorTemperatureC = indoorTemperatureC,
            OutdoorTemperatureC = outdoorTemperatureC,
            PeopleCount = peopleCount,
            EquipmentLoadW = equipmentLoadW,
            LightingLoadW = lightingLoadW,
            FloorId = floorId
        });

        response.EnsureSuccessStatusCode();
        return await ReadRequiredAsync<RoomResponse>(response);
    }

    public static async Task<WindowResponse> AddWindowAsync(
        this HttpClient client,
        int roomId,
        double areaM2)
    {
        var response = await client.PostAsJsonAsync($"/api/rooms/{roomId}/windows", new CreateWindowRequest
        {
            AreaM2 = areaM2
        });

        response.EnsureSuccessStatusCode();
        return await ReadRequiredAsync<WindowResponse>(response);
    }

    public static async Task<WallResponse> AddWallAsync(
        this HttpClient client,
        int roomId,
        double areaM2,
        bool isExternal)
    {
        var response = await client.PostAsJsonAsync($"/api/rooms/{roomId}/walls", new CreateWallRequest
        {
            AreaM2 = areaM2,
            IsExternal = isExternal
        });

        response.EnsureSuccessStatusCode();
        return await ReadRequiredAsync<WallResponse>(response);
    }

    public static async Task<EquipmentCatalogItemResponse> CreateEquipmentAsync(
        this HttpClient client,
        string systemType,
        string unitType,
        string modelName,
        double nominalCoolingCapacityKw,
        bool isActive = true,
        string manufacturer = "CoolTech")
    {
        var response = await client.PostAsJsonAsync("/api/equipment-catalog-items", new CreateEquipmentCatalogItemRequest
        {
            Manufacturer = manufacturer,
            SystemType = systemType,
            UnitType = unitType,
            ModelName = modelName,
            NominalCoolingCapacityKw = nominalCoolingCapacityKw,
            IsActive = isActive
        });

        response.EnsureSuccessStatusCode();
        return await ReadRequiredAsync<EquipmentCatalogItemResponse>(response);
    }

    public static async Task<T> ReadRequiredAsync<T>(HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        Assert.NotNull(value);
        return value;
    }
}
