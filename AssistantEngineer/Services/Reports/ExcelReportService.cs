using AssistantEngineer.Contracts.Reports;
using ClosedXML.Excel;

namespace AssistantEngineer.Services.Reports;

public class ExcelReportService
{
    public byte[] GenerateBuildingReport(BuildingReport report)
    {
        using var workbook = new XLWorkbook();

        AddSummaryWorksheet(workbook, report);
        AddFloorsWorksheet(workbook, report.FloorSummaries);
        AddRoomsWorksheet(workbook, report.Rooms);
        AddWindowsWorksheet(workbook, report.Windows);
        AddWallsWorksheet(workbook, report.Walls);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private static void AddSummaryWorksheet(XLWorkbook workbook, BuildingReport report)
    {
        var worksheet = workbook.Worksheets.Add("Summary");

        worksheet.Cell(1, 1).Value = "Building report";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;

        worksheet.Cell(3, 1).Value = "Project";
        worksheet.Cell(3, 2).Value = report.ProjectName;
        worksheet.Cell(4, 1).Value = "Building";
        worksheet.Cell(4, 2).Value = report.BuildingName;
        worksheet.Cell(5, 1).Value = "Generated at UTC";
        worksheet.Cell(5, 2).Value = report.GeneratedAtUtc;
        worksheet.Cell(6, 1).Value = "Floors count";
        worksheet.Cell(6, 2).Value = report.FloorsCount;
        worksheet.Cell(7, 1).Value = "Rooms count";
        worksheet.Cell(7, 2).Value = report.RoomsCount;
        worksheet.Cell(8, 1).Value = "Total heat load, W";
        worksheet.Cell(8, 2).Value = report.TotalHeatLoadW;
        worksheet.Cell(9, 1).Value = "Total heat load, kW";
        worksheet.Cell(9, 2).Value = report.TotalHeatLoadKw;
        worksheet.Cell(10, 1).Value = "Reserve factor";
        worksheet.Cell(10, 2).Value = report.DesignReserveFactor;
        worksheet.Cell(11, 1).Value = "Design capacity, W";
        worksheet.Cell(11, 2).Value = report.DesignCapacityW;
        worksheet.Cell(12, 1).Value = "Design capacity, kW";
        worksheet.Cell(12, 2).Value = report.DesignCapacityKw;

        worksheet.Column(1).Style.Font.Bold = true;
        worksheet.Columns().AdjustToContents();
    }

    private static void AddFloorsWorksheet(
        XLWorkbook workbook,
        IReadOnlyCollection<FloorReportSummary> floorSummaries)
    {
        var worksheet = workbook.Worksheets.Add("Floors");
        WriteHeader(
            worksheet,
            "Floor ID",
            "Floor",
            "Rooms count",
            "Total heat load, W",
            "Total heat load, kW",
            "Reserve factor",
            "Design capacity, W",
            "Design capacity, kW");

        var row = 2;
        foreach (var floor in floorSummaries)
        {
            worksheet.Cell(row, 1).Value = floor.FloorId;
            worksheet.Cell(row, 2).Value = floor.FloorName;
            worksheet.Cell(row, 3).Value = floor.RoomsCount;
            worksheet.Cell(row, 4).Value = floor.TotalHeatLoadW;
            worksheet.Cell(row, 5).Value = floor.TotalHeatLoadKw;
            worksheet.Cell(row, 6).Value = floor.DesignReserveFactor;
            worksheet.Cell(row, 7).Value = floor.DesignCapacityW;
            worksheet.Cell(row, 8).Value = floor.DesignCapacityKw;
            row++;
        }

        FormatTable(worksheet, columnCount: 8);
    }

    private static void AddRoomsWorksheet(
        XLWorkbook workbook,
        IReadOnlyCollection<RoomReportRow> rooms)
    {
        var worksheet = workbook.Worksheets.Add("Rooms");
        WriteHeader(
            worksheet,
            "Room ID",
            "Project",
            "Building",
            "Floor",
            "Room",
            "Area, m2",
            "Height, m",
            "Volume, m3",
            "Indoor temp, C",
            "Outdoor temp, C",
            "People",
            "Equipment load, W",
            "Lighting load, W",
            "Window area, m2",
            "Wall area, m2",
            "External wall area, m2",
            "Base load, W",
            "Window gain, W",
            "Wall gain, W",
            "Internal gain, W",
            "Total load, W",
            "Total load, kW",
            "Reserve factor",
            "Design capacity, W",
            "Design capacity, kW");

        var row = 2;
        foreach (var room in rooms)
        {
            worksheet.Cell(row, 1).Value = room.RoomId;
            worksheet.Cell(row, 2).Value = room.ProjectName;
            worksheet.Cell(row, 3).Value = room.BuildingName;
            worksheet.Cell(row, 4).Value = room.FloorName;
            worksheet.Cell(row, 5).Value = room.RoomName;
            worksheet.Cell(row, 6).Value = room.AreaM2;
            worksheet.Cell(row, 7).Value = room.HeightM;
            worksheet.Cell(row, 8).Value = room.VolumeM3;
            worksheet.Cell(row, 9).Value = room.IndoorTemperatureC;
            worksheet.Cell(row, 10).Value = room.OutdoorTemperatureC;
            worksheet.Cell(row, 11).Value = room.PeopleCount;
            worksheet.Cell(row, 12).Value = room.EquipmentLoadW;
            worksheet.Cell(row, 13).Value = room.LightingLoadW;
            worksheet.Cell(row, 14).Value = room.TotalWindowAreaM2;
            worksheet.Cell(row, 15).Value = room.TotalWallAreaM2;
            worksheet.Cell(row, 16).Value = room.ExternalWallAreaM2;
            worksheet.Cell(row, 17).Value = room.BaseRoomLoadW;
            worksheet.Cell(row, 18).Value = room.WindowHeatGainW;
            worksheet.Cell(row, 19).Value = room.WallHeatGainW;
            worksheet.Cell(row, 20).Value = room.InternalHeatGainW;
            worksheet.Cell(row, 21).Value = room.TotalHeatLoadW;
            worksheet.Cell(row, 22).Value = room.TotalHeatLoadKw;
            worksheet.Cell(row, 23).Value = room.DesignReserveFactor;
            worksheet.Cell(row, 24).Value = room.DesignCapacityW;
            worksheet.Cell(row, 25).Value = room.DesignCapacityKw;
            row++;
        }

        FormatTable(worksheet, columnCount: 25);
    }

    private static void AddWindowsWorksheet(
        XLWorkbook workbook,
        IReadOnlyCollection<WindowReportRow> windows)
    {
        var worksheet = workbook.Worksheets.Add("Windows");
        WriteHeader(worksheet, "Window ID", "Room ID", "Floor", "Room", "Area, m2");

        var row = 2;
        foreach (var window in windows)
        {
            worksheet.Cell(row, 1).Value = window.WindowId;
            worksheet.Cell(row, 2).Value = window.RoomId;
            worksheet.Cell(row, 3).Value = window.FloorName;
            worksheet.Cell(row, 4).Value = window.RoomName;
            worksheet.Cell(row, 5).Value = window.AreaM2;
            row++;
        }

        FormatTable(worksheet, columnCount: 5);
    }

    private static void AddWallsWorksheet(
        XLWorkbook workbook,
        IReadOnlyCollection<WallReportRow> walls)
    {
        var worksheet = workbook.Worksheets.Add("Walls");
        WriteHeader(worksheet, "Wall ID", "Room ID", "Floor", "Room", "Area, m2", "External");

        var row = 2;
        foreach (var wall in walls)
        {
            worksheet.Cell(row, 1).Value = wall.WallId;
            worksheet.Cell(row, 2).Value = wall.RoomId;
            worksheet.Cell(row, 3).Value = wall.FloorName;
            worksheet.Cell(row, 4).Value = wall.RoomName;
            worksheet.Cell(row, 5).Value = wall.AreaM2;
            worksheet.Cell(row, 6).Value = wall.IsExternal ? "Yes" : "No";
            row++;
        }

        FormatTable(worksheet, columnCount: 6);
    }

    private static void WriteHeader(IXLWorksheet worksheet, params string[] headers)
    {
        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }
    }

    private static void FormatTable(IXLWorksheet worksheet, int columnCount)
    {
        var header = worksheet.Range(1, 1, 1, columnCount);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightGray;

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();
    }
}
