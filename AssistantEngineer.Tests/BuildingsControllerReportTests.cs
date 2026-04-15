using AssistantEngineer.Controllers;
using AssistantEngineer.Data;
using AssistantEngineer.Services.Calculations;
using AssistantEngineer.Services.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Tests;

public class BuildingsControllerReportTests
{
    [Fact]
    public async Task GetReport_WhenOnlySystemTypeIsProvided_ReturnsBadRequest()
    {
        // Arrange
        await using var context = CreateContext();
        var controller = CreateController(context);

        // Act
        var actionResult = await controller.GetReport(
            buildingId: 1,
            systemType: "Split",
            unitType: null);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal(
            "Both systemType and unitType must be provided to include equipment selection.",
            badRequest.Value);
    }

    [Fact]
    public async Task DownloadExcelReport_WhenOnlyUnitTypeIsProvided_ReturnsBadRequest()
    {
        // Arrange
        await using var context = CreateContext();
        var controller = CreateController(context);

        // Act
        var actionResult = await controller.DownloadExcelReport(
            buildingId: 1,
            systemType: null,
            unitType: "WallMounted");

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(
            "Both systemType and unitType must be provided to include equipment selection.",
            badRequest.Value);
    }

    private static BuildingsController CreateController(AppDbContext context)
    {
        var roomCalculationService = new RoomCalculationService();
        var aggregateCalculationService = new AggregateCalculationService(context, roomCalculationService);
        var buildingReportDataService = new BuildingReportDataService(context, roomCalculationService);
        var excelReportService = new ExcelReportService();

        return new BuildingsController(
            context,
            aggregateCalculationService,
            buildingReportDataService,
            excelReportService);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
