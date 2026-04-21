using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports;
using ClosedXML.Excel;

namespace AssistantEngineer.Persistence.Services.Reports;

public class ExcelReportService : IBuildingReportExporter
{
    public byte[] GenerateBuildingReport(BuildingReport report, CancellationToken cancellationToken = default)
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

        using var stream = new MemoryStream();
        cancellationToken.ThrowIfCancellationRequested();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] GenerateEnergyBalanceReport(
        BuildingEnergyBalanceResult report,
        CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();

        cancellationToken.ThrowIfCancellationRequested();
        AddEnergyBalanceSummaryWorksheet(workbook, report);
        cancellationToken.ThrowIfCancellationRequested();
        AddMonthlyEnergyBalanceWorksheet(workbook, report.MonthlyBalances, cancellationToken);

        using var stream = new MemoryStream();
        cancellationToken.ThrowIfCancellationRequested();
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
        worksheet.Cell(5, 1).Value = "Calculation method";
        worksheet.Cell(5, 2).Value = report.CalculationMethod;
        worksheet.Cell(6, 1).Value = "Peak hour";
        if (report.PeakHour.HasValue)
            worksheet.Cell(6, 2).Value = report.PeakHour.Value;
        worksheet.Cell(7, 1).Value = "Generated at UTC";
        worksheet.Cell(7, 2).Value = report.GeneratedAtUtc;
        worksheet.Cell(8, 1).Value = "Floors count";
        worksheet.Cell(8, 2).Value = report.FloorsCount;
        worksheet.Cell(9, 1).Value = "Rooms count";
        worksheet.Cell(9, 2).Value = report.RoomsCount;
        worksheet.Cell(10, 1).Value = "Total heat load, W";
        worksheet.Cell(10, 2).Value = report.TotalHeatLoadW;
        worksheet.Cell(11, 1).Value = "Total heat load, kW";
        worksheet.Cell(11, 2).Value = report.TotalHeatLoadKw;
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
        IReadOnlyCollection<FloorReportSummary> floorSummaries,
        CancellationToken cancellationToken)
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
            cancellationToken.ThrowIfCancellationRequested();

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
        IReadOnlyCollection<RoomReportRow> rooms,
        CancellationToken cancellationToken)
    {
        var worksheet = workbook.Worksheets.Add("Rooms");
        WriteHeader(
            worksheet,
            "Room ID",
            "Calculation method",
            "Peak hour",
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
            if (room.PeakHour.HasValue)
                worksheet.Cell(row, 3).Value = room.PeakHour.Value;
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
            worksheet.Cell(row, 23).Value = room.TotalHeatLoadW;
            worksheet.Cell(row, 24).Value = room.TotalHeatLoadKw;
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

        FormatTable(worksheet, columnCount: 35);
    }

    private static void AddWindowsWorksheet(
        XLWorkbook workbook,
        IReadOnlyCollection<WindowReportRow> windows,
        CancellationToken cancellationToken)
    {
        var worksheet = workbook.Worksheets.Add("Windows");
        WriteHeader(worksheet, "Window ID", "Room ID", "Floor", "Room", "Area, m2");

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

        FormatTable(worksheet, columnCount: 5);
    }

    private static void AddWallsWorksheet(
        XLWorkbook workbook,
        IReadOnlyCollection<WallReportRow> walls,
        CancellationToken cancellationToken)
    {
        var worksheet = workbook.Worksheets.Add("Walls");
        WriteHeader(worksheet, "Wall ID", "Room ID", "Floor", "Room", "Area, m2", "External");

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

        FormatTable(worksheet, columnCount: 6);
    }

    private static void AddEnergyBalanceSummaryWorksheet(
        XLWorkbook workbook,
        BuildingEnergyBalanceResult report)
    {
        var worksheet = workbook.Worksheets.Add("Summary");

        worksheet.Cell(1, 1).Value = "Energy balance report";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;

        worksheet.Cell(3, 1).Value = "Building ID";
        worksheet.Cell(3, 2).Value = report.BuildingId;
        worksheet.Cell(4, 1).Value = "Building";
        worksheet.Cell(4, 2).Value = report.BuildingName;
        worksheet.Cell(5, 1).Value = "Cooling calculation method";
        worksheet.Cell(5, 2).Value = report.CoolingCalculationMethod;
        worksheet.Cell(6, 1).Value = "Heating calculation method";
        worksheet.Cell(6, 2).Value = report.HeatingCalculationMethod;
        worksheet.Cell(7, 1).Value = "Annual cooling demand, kWh";
        worksheet.Cell(7, 2).Value = report.AnnualCoolingDemandKWh;
        worksheet.Cell(8, 1).Value = "Annual heating demand, kWh";
        worksheet.Cell(8, 2).Value = report.AnnualHeatingDemandKWh;
        worksheet.Cell(9, 1).Value = "Annual total demand, kWh";
        worksheet.Cell(9, 2).Value = report.AnnualTotalDemandKWh;

        worksheet.Column(1).Style.Font.Bold = true;
        worksheet.Columns().AdjustToContents();
    }

    private static void AddMonthlyEnergyBalanceWorksheet(
        XLWorkbook workbook,
        IReadOnlyCollection<MonthlyEnergyBalance> monthlyBalances,
        CancellationToken cancellationToken)
    {
        var worksheet = workbook.Worksheets.Add("Monthly balance");
        WriteHeader(
            worksheet,
            "Month",
            "Cooling demand, kWh",
            "Heating demand, kWh",
            "Total demand, kWh");

        var row = 2;
        foreach (var balance in monthlyBalances.OrderBy(balance => balance.Month))
        {
            cancellationToken.ThrowIfCancellationRequested();

            worksheet.Cell(row, 1).Value = balance.Month;
            worksheet.Cell(row, 2).Value = balance.CoolingDemandKWh;
            worksheet.Cell(row, 3).Value = balance.HeatingDemandKWh;
            worksheet.Cell(row, 4).Value = balance.CoolingDemandKWh + balance.HeatingDemandKWh;
            row++;
        }

        FormatTable(worksheet, columnCount: 4);
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
