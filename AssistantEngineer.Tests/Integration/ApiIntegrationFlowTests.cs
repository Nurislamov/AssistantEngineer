using System.Net;
using System.Net.Http.Json;
using AssistantEngineer.Contracts.Reports;
using AssistantEngineer.Contracts.Requests;
using AssistantEngineer.Contracts.Responses;
using AssistantEngineer.Domain.Contracts.Calculations;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssistantEngineer.Tests.Integration;

public class ApiIntegrationFlowTests
{
    [Fact]
    public async Task CalculationFlow_CreatesStructureCalculatesBuildingAndPersistsDesignCapacity()
    {
        // Arrange
        await using var factory = new AssistantEngineerWebApplicationFactory();
        var client = CreateClient(factory);

        // Act
        var project = await client.CreateProjectAsync("Calculation Flow Project");
        var building = await client.CreateBuildingAsync(project.Id, "Main Building");
        var floor = await client.CreateFloorAsync(building.Id, "Floor 1");

        var primaryRoom = await client.CreateRoomAsync(
            floor.Id,
            name: "Load Lab",
            areaM2: 24,
            heightM: 3,
            indoorTemperatureC: 24,
            outdoorTemperatureC: 38,
            peopleCount: 4,
            equipmentLoadW: 900,
            lightingLoadW: 320);

        await client.AddWindowAsync(primaryRoom.Id, 3.2);
        await client.AddWindowAsync(primaryRoom.Id, 1.8);
        await client.AddWallAsync(primaryRoom.Id, 18.5, isExternal: true);
        await client.AddWallAsync(primaryRoom.Id, 12, isExternal: false);

        await client.CreateRoomAsync(
            floor.Id,
            name: "Small Office",
            areaM2: 10,
            heightM: 3,
            indoorTemperatureC: 24,
            outdoorTemperatureC: 24,
            peopleCount: 1,
            equipmentLoadW: 0,
            lightingLoadW: 0);

        var calculationResponse = await client.GetAsync($"/api/buildings/{building.Id}/calculate");

        // Assert
        calculationResponse.EnsureSuccessStatusCode();
        var calculation = await ApiTestClientExtensions.ReadRequiredAsync<BuildingCalculationResult>(calculationResponse);

        Assert.Equal(building.Id, calculation.BuildingId);
        Assert.Equal("Main Building", calculation.BuildingName);
        Assert.Equal(1, calculation.FloorsCount);
        Assert.Equal(2, calculation.RoomsCount);
        Assert.Equal(8302, calculation.TotalHeatLoadW);
        Assert.Equal(8.3, calculation.TotalHeatLoadKw);
        Assert.Equal(1.1, calculation.DesignReserveFactor);
        Assert.Equal(9132.2, calculation.DesignCapacityW);
        Assert.Equal(9.13, calculation.DesignCapacityKw);

        var persistedBuildingResponse = await client.GetAsync($"/api/buildings/by-id/{building.Id}");
        persistedBuildingResponse.EnsureSuccessStatusCode();
        var persistedBuilding = await ApiTestClientExtensions.ReadRequiredAsync<BuildingResponse>(persistedBuildingResponse);

        Assert.Equal(1.1, persistedBuilding.DesignReserveFactor);
        Assert.Equal(9132.2, persistedBuilding.DesignCapacityW);
        Assert.Equal(9.13, persistedBuilding.DesignCapacityKw);
    }

    [Fact]
    public async Task EquipmentAndReportFlow_SelectsEquipmentAndReturnsJsonAndExcelReports()
    {
        // Arrange
        await using var factory = new AssistantEngineerWebApplicationFactory();
        var client = CreateClient(factory);

        var project = await client.CreateProjectAsync("Equipment Report Project");
        var building = await client.CreateBuildingAsync(project.Id, "Equipment Building");
        var floor = await client.CreateFloorAsync(building.Id, "Equipment Floor");
        var room = await client.CreateRoomAsync(
            floor.Id,
            name: "Equipment Room",
            areaM2: 24,
            heightM: 3,
            indoorTemperatureC: 24,
            outdoorTemperatureC: 38,
            peopleCount: 4,
            equipmentLoadW: 900,
            lightingLoadW: 320);

        await client.AddWindowAsync(room.Id, 3.2);
        await client.AddWindowAsync(room.Id, 1.8);
        await client.AddWallAsync(room.Id, 18.5, isExternal: true);
        await client.AddWallAsync(room.Id, 12, isExternal: false);

        const string systemType = "SplitIntegration";
        const string unitType = "WallIntegration";

        await client.CreateEquipmentAsync(systemType, unitType, "WM-7.5", 7.5);
        var selectedEquipment = await client.CreateEquipmentAsync(systemType, unitType, "WM-8.0", 8.0);
        await client.CreateEquipmentAsync(systemType, unitType, "WM-10.0-INACTIVE", 10.0, isActive: false);

        // Act
        var selectionResponse = await client.PostAsJsonAsync(
            $"/api/rooms/{room.Id}/select-equipment",
            new EquipmentSelectionRequest
            {
                SystemType = systemType,
                UnitType = unitType
            });

        var reportResponse = await client.GetAsync(
            $"/api/buildings/{building.Id}/report?systemType={systemType}&unitType={unitType}");

        var excelResponse = await client.GetAsync(
            $"/api/buildings/{building.Id}/report/excel?systemType={systemType}&unitType={unitType}");

        // Assert
        selectionResponse.EnsureSuccessStatusCode();
        var selection = await ApiTestClientExtensions.ReadRequiredAsync<EquipmentSelectionResult>(selectionResponse);

        Assert.Equal(room.Id, selection.RoomId);
        Assert.Equal(selectedEquipment.Id, selection.SelectedCatalogItemId);
        Assert.Equal("WM-8.0", selection.SelectedModelName);
        Assert.Equal(8.0, selection.SelectedNominalCoolingCapacityKw);
        Assert.Equal(0.11, selection.CapacityReserveKw);

        reportResponse.EnsureSuccessStatusCode();
        var report = await ApiTestClientExtensions.ReadRequiredAsync<BuildingReport>(reportResponse);

        Assert.True(report.EquipmentSelectionRequested);
        Assert.Equal(systemType, report.RequestedSystemType);
        Assert.Equal(unitType, report.RequestedUnitType);
        Assert.Equal(1, report.RoomsWithSelectionCount);
        Assert.Equal(0, report.RoomsWithoutSelectionCount);
        Assert.Equal(8.0, report.TotalSelectedCapacityKw);

        var roomReport = Assert.Single(report.Rooms);
        Assert.Equal(room.Id, roomReport.RoomId);
        Assert.True(roomReport.EquipmentSelected);
        Assert.Equal(selectedEquipment.Id, roomReport.SelectedCatalogItemId);
        Assert.Equal("WM-8.0", roomReport.SelectedModelName);
        Assert.Equal(8.0, roomReport.SelectedNominalCoolingCapacityKw);
        Assert.Equal(0.11, roomReport.SelectionReserveKw);

        excelResponse.EnsureSuccessStatusCode();
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            excelResponse.Content.Headers.ContentType?.MediaType);

        await using var excelStream = await excelResponse.Content.ReadAsStreamAsync();
        using var workbook = new XLWorkbook(excelStream);

        var summary = workbook.Worksheet("Summary");
        Assert.Equal("Yes", summary.Cell(13, 2).GetString());
        Assert.Equal(systemType, summary.Cell(14, 2).GetString());
        Assert.Equal(unitType, summary.Cell(15, 2).GetString());
        Assert.Equal(1, summary.Cell(16, 2).GetDouble());
        Assert.Equal(8.0, summary.Cell(18, 2).GetDouble());

        var rooms = workbook.Worksheet("Rooms");
        Assert.Equal("Equipment Room", rooms.Cell(2, 5).GetString());
        Assert.Equal("Yes", rooms.Cell(2, 28).GetString());
        Assert.Equal(selectedEquipment.Id, rooms.Cell(2, 29).GetDouble());
        Assert.Equal("WM-8.0", rooms.Cell(2, 31).GetString());
        Assert.Equal(8.0, rooms.Cell(2, 32).GetDouble());
    }

    [Fact]
    public async Task ValidationFlow_ReturnsExpectedBadRequestAndNotFoundResponses()
    {
        // Arrange
        await using var factory = new AssistantEngineerWebApplicationFactory();
        var client = CreateClient(factory);

        // Act
        var invalidProjectResponse = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest
        {
            Name = "A"
        });

        var missingBuildingResponse = await client.GetAsync("/api/buildings/by-id/999999");
        var incompleteReportFilterResponse = await client.GetAsync(
            "/api/buildings/1/report?systemType=Split");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, invalidProjectResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingBuildingResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, incompleteReportFilterResponse.StatusCode);

        var incompleteReportFilterMessage = await incompleteReportFilterResponse.Content.ReadAsStringAsync();
        Assert.Contains(
            "Both systemType and unitType must be provided to include equipment selection.",
            incompleteReportFilterMessage);
    }

    private static HttpClient CreateClient(AssistantEngineerWebApplicationFactory factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }
}
