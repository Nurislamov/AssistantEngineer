using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Infrastructure.Integrations.Reports.Excel;
using ClosedXML.Excel;

namespace AssistantEngineer.Tests;

public class ExcelReportServiceTests
{
    [Fact]
    public void GenerateEnergyBalanceReportCreatesSummaryAndMonthlyWorksheets()
    {
        var report = new BuildingEnergyBalanceResult
        {
            BuildingId = 10,
            BuildingName = "Main",
            CoolingCalculationMethod = "Iso52016",
            HeatingCalculationMethod = "En12831",
            AnnualCoolingDemandKWh = 1200,
            AnnualHeatingDemandKWh = 800,
            AnnualTotalDemandKWh = 2000,
            MonthlyBalances =
            [
                new MonthlyEnergyBalance
                {
                    Month = 2,
                    CoolingDemandKWh = 100,
                    HeatingDemandKWh = 50
                },
                new MonthlyEnergyBalance
                {
                    Month = 1,
                    CoolingDemandKWh = 90,
                    HeatingDemandKWh = 60
                }
            ]
        };
        var service = new BuildingEnergyBalanceExcelReportExporter();

        var content = service.GenerateEnergyBalanceReport(report);

        using var workbook = new XLWorkbook(new MemoryStream(content));
        var summary = workbook.Worksheet("Summary");
        Assert.Equal("Energy balance report", summary.Cell(1, 1).GetString());
        Assert.Equal("Main", summary.Cell(4, 2).GetString());
        Assert.Equal(2000, summary.Cell(9, 2).GetDouble());

        var monthly = workbook.Worksheet("Monthly balance");
        Assert.Equal("Month", monthly.Cell(1, 1).GetString());
        Assert.Equal(1, monthly.Cell(2, 1).GetDouble());
        Assert.Equal(150, monthly.Cell(2, 4).GetDouble());
        Assert.Equal(2, monthly.Cell(3, 1).GetDouble());
        Assert.Equal(150, monthly.Cell(3, 4).GetDouble());
    }
}

