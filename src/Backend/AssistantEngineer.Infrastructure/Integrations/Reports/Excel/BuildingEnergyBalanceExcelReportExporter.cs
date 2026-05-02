using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using ClosedXML.Excel;

namespace AssistantEngineer.Infrastructure.Integrations.Reports.Excel;

public sealed class BuildingEnergyBalanceExcelReportExporter : IBuildingEnergyBalanceReportExporter
{
    public byte[] GenerateEnergyBalanceReport(
        BuildingEnergyBalanceResult report,
        CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();

        cancellationToken.ThrowIfCancellationRequested();
        AddSummaryWorksheet(workbook, report);

        cancellationToken.ThrowIfCancellationRequested();
        AddMonthlyWorksheet(
            workbook,
            report.MonthlyBalances,
            cancellationToken);

        return ExcelWorkbookWriter.SaveToBytes(
            workbook,
            cancellationToken);
    }

    private static void AddSummaryWorksheet(
        XLWorkbook workbook,
        BuildingEnergyBalanceResult report)
    {
        var worksheet = workbook.Worksheets.Add("Summary");

        ExcelWorkbookWriter.WriteTitle(
            worksheet,
            "Energy balance report");

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

    private static void AddMonthlyWorksheet(
        XLWorkbook workbook,
        IReadOnlyCollection<MonthlyEnergyBalance> monthlyBalances,
        CancellationToken cancellationToken)
    {
        var worksheet = workbook.Worksheets.Add("Monthly balance");

        ExcelWorkbookWriter.WriteHeader(
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

        ExcelWorkbookWriter.FormatTable(
            worksheet,
            columnCount: 4);
    }
}