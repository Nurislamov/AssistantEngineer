using AssistantEngineer.Contracts;
using AssistantEngineer.Services;
using ClosedXML.Excel;

namespace AssistantEngineer.Tests;

public class ExcelReportServiceTests
{
    [Fact]
    public void GenerateBuildingReport_ReturnsWorkbookWithExpectedWorksheets()
    {
        // Arrange
        var service = new ExcelReportService();
        var report = new BuildingReportDto
        {
            ProjectName = "Project 1",
            BuildingName = "Building 1",
            GeneratedAtUtc = new DateTime(2026, 4, 12, 12, 0, 0, DateTimeKind.Utc),
            FloorsCount = 1,
            RoomsCount = 1,
            TotalHeatLoadW = 2330,
            TotalHeatLoadKw = 2.33,
            ReserveFactor = 1.1,
            DesignCapacityW = 2563,
            DesignCapacityKw = 2.56,
            FloorSummaries =
            [
                new BuildingFloorSummaryDto
                {
                    FloorId = 1,
                    FloorName = "Floor 1",
                    RoomsCount = 1,
                    TotalHeatLoadW = 2330,
                    TotalHeatLoadKw = 2.33,
                    ReserveFactor = 1.1,
                    DesignCapacityW = 2563,
                    DesignCapacityKw = 2.56
                }
            ],
            Rooms =
            [
                new BuildingRoomReportRowDto
                {
                    RoomId = 1,
                    ProjectName = "Project 1",
                    BuildingName = "Building 1",
                    FloorName = "Floor 1",
                    RoomName = "Room 1",
                    AreaM2 = 10,
                    HeightM = 3,
                    VolumeM3 = 30,
                    IndoorTemperatureC = 24,
                    OutdoorTemperatureC = 24,
                    PeopleCount = 1,
                    EquipmentLoadW = 70,
                    LightingLoadW = 30,
                    TotalWindowAreaM2 = 2,
                    TotalWallAreaM2 = 15,
                    ExternalWallAreaM2 = 10,
                    BaseRoomLoadW = 1000,
                    WindowHeatGainW = 500,
                    WallHeatGainW = 600,
                    InternalHeatGainW = 230,
                    TotalHeatLoadW = 2330,
                    TotalHeatLoadKw = 2.33,
                    ReserveFactor = 1.1,
                    DesignCapacityW = 2563,
                    DesignCapacityKw = 2.56
                }
            ],
            Windows =
            [
                new WindowReportRowDto
                {
                    WindowId = 1,
                    RoomId = 1,
                    FloorName = "Floor 1",
                    RoomName = "Room 1",
                    AreaM2 = 2
                }
            ],
            Walls =
            [
                new WallReportRowDto
                {
                    WallId = 1,
                    RoomId = 1,
                    FloorName = "Floor 1",
                    RoomName = "Room 1",
                    AreaM2 = 10,
                    IsExternal = true
                }
            ]
        };

        // Act
        var content = service.GenerateBuildingReport(report);

        // Assert
        Assert.NotEmpty(content);

        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);

        Assert.True(workbook.Worksheets.Contains("Summary"));
        Assert.True(workbook.Worksheets.Contains("Floors"));
        Assert.True(workbook.Worksheets.Contains("Rooms"));
        Assert.True(workbook.Worksheets.Contains("Windows"));
        Assert.True(workbook.Worksheets.Contains("Walls"));

        var summary = workbook.Worksheet("Summary");
        Assert.Equal("Project 1", summary.Cell(3, 2).GetString());
        Assert.Equal("Building 1", summary.Cell(4, 2).GetString());
        Assert.Equal(2330, summary.Cell(8, 2).GetDouble());
        Assert.Equal(2.33, summary.Cell(9, 2).GetDouble());
        Assert.Equal(1.1, summary.Cell(10, 2).GetDouble());
        Assert.Equal(2563, summary.Cell(11, 2).GetDouble());
        Assert.Equal(2.56, summary.Cell(12, 2).GetDouble());

        var rooms = workbook.Worksheet("Rooms");
        Assert.Equal("Room ID", rooms.Cell(1, 1).GetString());
        Assert.Equal("Room 1", rooms.Cell(2, 5).GetString());
        Assert.Equal(2330, rooms.Cell(2, 21).GetDouble());
        Assert.Equal(1.1, rooms.Cell(2, 23).GetDouble());
        Assert.Equal(2563, rooms.Cell(2, 24).GetDouble());
        Assert.Equal(2.56, rooms.Cell(2, 25).GetDouble());
    }
}
