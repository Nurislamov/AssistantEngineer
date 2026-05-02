using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Cooling;
using ClosedXML.Excel;

namespace AssistantEngineer.Infrastructure.Integrations.Reports.Excel;

public sealed class BuildingCoolingExcelReportExporter : IBuildingCoolingReportExporter
{
    public byte[] GenerateCoolingReport(
        BuildingCoolingReport report,
        CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();

        cancellationToken.ThrowIfCancellationRequested();
        AddSummaryWorksheet(workbook, report);

        cancellationToken.ThrowIfCancellationRequested();
        AddFloorsWorksheet(workbook, report.FloorSummaries, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        AddRoomsWorksheet(workbook, report.Rooms, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        AddWindowsWorksheet(workbook, report.Windows, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        AddWallsWorksheet(workbook, report.Walls, cancellationToken);

        return ExcelWorkbookWriter.SaveToBytes(
            workbook,
            cancellationToken);
    }

    private static void AddSummaryWorksheet(
        XLWorkbook workbook,
        BuildingCoolingReport report)
    {
        var worksheet = workbook.Worksheets.Add("Summary");

        ExcelWorkbookWriter.WriteTitle(
            worksheet,
            "Building cooling report");

        worksheet.Cell(3, 1).Value = "Project";
        worksheet.Cell(3, 2).Value = report.ProjectName;

        worksheet.Cell(4, 1).Value = "Building";
        worksheet.Cell(4, 2).Value = report.BuildingName;

        worksheet.Cell(5, 1).Value = "Calculation method";
        worksheet.Cell(5, 2).Value = report.CalculationMethod;

        worksheet.Cell(6, 1).Value = "Peak hour of year";
        if (report.PeakHourOfYear.HasValue)
            worksheet.Cell(6, 2).Value = report.PeakHourOfYear.Value;

        worksheet.Cell(7, 1).Value = "Generated at UTC";
        worksheet.Cell(7, 2).Value = report.GeneratedAtUtc;

        worksheet.Cell(8, 1).Value = "Floors count";
        worksheet.Cell(8, 2).Value = report.FloorsCount;

        worksheet.Cell(9, 1).Value = "Rooms count";
        worksheet.Cell(9, 2).Value = report.RoomsCount;

        worksheet.Cell(10, 1).Value = "Cooling load, W";
        worksheet.Cell(10, 2).Value = report.CoolingLoadW;

        worksheet.Cell(11, 1).Value = "Cooling load, kW";
        worksheet.Cell(11, 2).Value = report.CoolingLoadKw;

        worksheet.Cell(12, 1).Value = "Reserve factor";
        worksheet.Cell(12, 2).Value = report.DesignReserveFactor;

        worksheet.Cell(13, 1).Value = "Design capacity, W";
        worksheet.Cell(13, 2).Value = report.DesignCapacityW;

        worksheet.Cell(14, 1).Value = "Design capacity, kW";
        worksheet.Cell(14, 2).Value = report.DesignCapacityKw;

        worksheet.Cell(15, 1).Value = "Equipment selection requested";
        worksheet.Cell(15, 2).Value = report.EquipmentSelectionRequested ? "Yes" : "No";

        worksheet.Cell(16, 1).Value = "Requested system type";
        worksheet.Cell(16, 2).Value = report.RequestedSystemType;

        worksheet.Cell(17, 1).Value = "Requested unit type";
        worksheet.Cell(17, 2).Value = report.RequestedUnitType;

        worksheet.Cell(18, 1).Value = "Rooms with selected equipment";
        worksheet.Cell(18, 2).Value = report.RoomsWithSelectionCount;

        worksheet.Cell(19, 1).Value = "Rooms without selected equipment";
        worksheet.Cell(19, 2).Value = report.RoomsWithoutSelectionCount;

        worksheet.Cell(20, 1).Value = "Total selected capacity, kW";
        if (report.TotalSelectedCapacityKw.HasValue)
            worksheet.Cell(20, 2).Value = report.TotalSelectedCapacityKw.Value;

        worksheet.Column(1).Style.Font.Bold = true;
        worksheet.Columns().AdjustToContents();
    }

    private static void AddFloorsWorksheet(
        XLWorkbook workbook,
        IReadOnlyCollection<FloorCoolingReportSummary> floorSummaries,
        CancellationToken cancellationToken)
    {
        var worksheet = workbook.Worksheets.Add("Floors");

        ExcelWorkbookWriter.WriteHeader(
            worksheet,
            "Floor ID",
            "Floor",
            "Rooms count",
            "Cooling load, W",
            "Cooling load, kW",
            "Reserve factor",
            "Design capacity, W",
            "Design capacity, kW");

        var row = 2;

        foreach (var floor in floorSummaries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            worksheet.Cell(row, 1).Value = floor.FloorId;
            worksheet.Cell(row, 2).Value = floor.FloorName;
            worksheet.Cell(row, 3).Value = floor.RoomsCount;
            worksheet.Cell(row, 4).Value = floor.CoolingLoadW;
            worksheet.Cell(row, 5).Value = floor.CoolingLoadKw;
            worksheet.Cell(row, 6).Value = floor.DesignReserveFactor;
            worksheet.Cell(row, 7).Value = floor.DesignCapacityW;
            worksheet.Cell(row, 8).Value = floor.DesignCapacityKw;

            row++;
        }

        ExcelWorkbookWriter.FormatTable(
            worksheet,
            columnCount: 8);
    }

    private static void AddRoomsWorksheet(
        XLWorkbook workbook,
        IReadOnlyCollection<RoomCoolingReportRow> rooms,
        CancellationToken cancellationToken)
    {
        var worksheet = workbook.Worksheets.Add("Rooms");

        ExcelWorkbookWriter.WriteHeader(
            worksheet,
            "Room ID",
            "Calculation method",
            "Peak hour of year",
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
            "Design capacity, kW",
            "Requested system type",
            "Requested unit type",
            "Equipment selected",
            "Selected catalog item ID",
            "Selected manufacturer",
            "Selected model",
            "Selected cooling capacity, kW",
            "Selection reserve, kW");

        var row = 2;

        foreach (var room in rooms)
        {
            cancellationToken.ThrowIfCancellationRequested();

            worksheet.Cell(row, 1).Value = room.RoomId;
            worksheet.Cell(row, 2).Value = room.CalculationMethod;

            if (room.PeakHourOfYear.HasValue)
                worksheet.Cell(row, 3).Value = room.PeakHourOfYear.Value;

            worksheet.Cell(row, 4).Value = room.ProjectName;
            worksheet.Cell(row, 5).Value = room.BuildingName;
            worksheet.Cell(row, 6).Value = room.FloorName;
            worksheet.Cell(row, 7).Value = room.RoomName;

            worksheet.Cell(row, 8).Value = room.AreaM2;
            worksheet.Cell(row, 9).Value = room.HeightM;
            worksheet.Cell(row, 10).Value = room.VolumeM3;

            worksheet.Cell(row, 11).Value = room.IndoorTemperatureC;
            worksheet.Cell(row, 12).Value = room.OutdoorTemperatureC;

            worksheet.Cell(row, 13).Value = room.PeopleCount;
            worksheet.Cell(row, 14).Value = room.EquipmentLoadW;
            worksheet.Cell(row, 15).Value = room.LightingLoadW;

            worksheet.Cell(row, 16).Value = room.TotalWindowAreaM2;
            worksheet.Cell(row, 17).Value = room.TotalWallAreaM2;
            worksheet.Cell(row, 18).Value = room.ExternalWallAreaM2;

            worksheet.Cell(row, 19).Value = room.BaseRoomLoadW;
            worksheet.Cell(row, 20).Value = room.WindowHeatGainW;
            worksheet.Cell(row, 21).Value = room.WallHeatGainW;
            worksheet.Cell(row, 22).Value = room.InternalHeatGainW;

            worksheet.Cell(row, 23).Value = room.CoolingLoadW;
            worksheet.Cell(row, 24).Value = room.CoolingLoadKw;

            worksheet.Cell(row, 25).Value = room.DesignReserveFactor;
            worksheet.Cell(row, 26).Value = room.DesignCapacityW;
            worksheet.Cell(row, 27).Value = room.DesignCapacityKw;

            worksheet.Cell(row, 28).Value = room.RequestedSystemType;
            worksheet.Cell(row, 29).Value = room.RequestedUnitType;
            worksheet.Cell(row, 30).Value = room.EquipmentSelected ? "Yes" : "No";

            if (room.SelectedCatalogItemId.HasValue)
                worksheet.Cell(row, 31).Value = room.SelectedCatalogItemId.Value;

            worksheet.Cell(row, 32).Value = room.SelectedManufacturer;
            worksheet.Cell(row, 33).Value = room.SelectedModelName;

            if (room.SelectedNominalCoolingCapacityKw.HasValue)
                worksheet.Cell(row, 34).Value = room.SelectedNominalCoolingCapacityKw.Value;

            if (room.SelectionReserveKw.HasValue)
                worksheet.Cell(row, 35).Value = room.SelectionReserveKw.Value;

            row++;
        }

        ExcelWorkbookWriter.FormatTable(
            worksheet,
            columnCount: 35);
    }

    private static void AddWindowsWorksheet(
        XLWorkbook workbook,
        IReadOnlyCollection<WindowCoolingReportRow> windows,
        CancellationToken cancellationToken)
    {
        var worksheet = workbook.Worksheets.Add("Windows");

        ExcelWorkbookWriter.WriteHeader(
            worksheet,
            "Window ID",
            "Room ID",
            "Floor",
            "Room",
            "Area, m2");

        var row = 2;

        foreach (var window in windows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            worksheet.Cell(row, 1).Value = window.WindowId;
            worksheet.Cell(row, 2).Value = window.RoomId;
            worksheet.Cell(row, 3).Value = window.FloorName;
            worksheet.Cell(row, 4).Value = window.RoomName;
            worksheet.Cell(row, 5).Value = window.AreaM2;

            row++;
        }

        ExcelWorkbookWriter.FormatTable(
            worksheet,
            columnCount: 5);
    }

    private static void AddWallsWorksheet(
        XLWorkbook workbook,
        IReadOnlyCollection<WallCoolingReportRow> walls,
        CancellationToken cancellationToken)
    {
        var worksheet = workbook.Worksheets.Add("Walls");

        ExcelWorkbookWriter.WriteHeader(
            worksheet,
            "Wall ID",
            "Room ID",
            "Floor",
            "Room",
            "Area, m2",
            "External");

        var row = 2;

        foreach (var wall in walls)
        {
            cancellationToken.ThrowIfCancellationRequested();

            worksheet.Cell(row, 1).Value = wall.WallId;
            worksheet.Cell(row, 2).Value = wall.RoomId;
            worksheet.Cell(row, 3).Value = wall.FloorName;
            worksheet.Cell(row, 4).Value = wall.RoomName;
            worksheet.Cell(row, 5).Value = wall.AreaM2;
            worksheet.Cell(row, 6).Value = wall.IsExternal ? "Yes" : "No";

            row++;
        }

        ExcelWorkbookWriter.FormatTable(
            worksheet,
            columnCount: 6);
    }
}
