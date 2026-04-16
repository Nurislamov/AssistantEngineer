using AssistantEngineer.Application.Services.Buildings;
using AssistantEngineer.Contracts.Reports;
using AssistantEngineer.Contracts.Requests;
using AssistantEngineer.Contracts.Responses;
using AssistantEngineer.Domain.Contracts.Calculations;
using AssistantEngineer.Infrastructure.Services.Reports;
using AssistantEngineer.Services.Calculations;
using AssistantEngineer.Services.Reports;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BuildingsController : ControllerBase
{
    private readonly BuildingApplicationService _buildings;
    private readonly AggregateCalculationService _aggregateCalculationService;
    private readonly BuildingReportDataService _buildingReportDataService;
    private readonly ExcelReportService _excelReportService;

    public BuildingsController(
        BuildingApplicationService buildings,
        AggregateCalculationService aggregateCalculationService,
        BuildingReportDataService buildingReportDataService,
        ExcelReportService excelReportService)
    {
        _buildings = buildings;
        _aggregateCalculationService = aggregateCalculationService;
        _buildingReportDataService = buildingReportDataService;
        _excelReportService = excelReportService;
    }

    [HttpPost("{projectId}")]
    public async Task<ActionResult<BuildingResponse>> CreateBuilding(
        int projectId,
        CreateBuildingRequest request)
    {
        var response = await _buildings.CreateAsync(projectId, request);

        if (response == null)
            return NotFound($"Project with id {projectId} not found.");

        return CreatedAtAction(nameof(GetBuilding), new { id = response.Id }, response);
    }

    [HttpGet("{projectId}")]
    public async Task<ActionResult<IEnumerable<BuildingResponse>>> GetBuildings(int projectId)
    {
        return Ok(await _buildings.GetByProjectIdAsync(projectId));
    }

    [HttpGet("by-id/{id}")]
    public async Task<ActionResult<BuildingResponse>> GetBuilding(int id)
    {
        var building = await _buildings.GetByIdAsync(id);

        if (building == null)
            return NotFound();

        return Ok(building);
    }

    [HttpGet("{buildingId}/calculate")]
    public async Task<ActionResult<BuildingCalculationResult>> CalculateBuilding(int buildingId)
    {
        var buildingCalculationResult =
            await _aggregateCalculationService.CalculateBuildingAsync(buildingId);

        if (buildingCalculationResult == null)
            return NotFound();

        return Ok(buildingCalculationResult);
    }

    [HttpGet("{buildingId}/report/excel")]
    public async Task<IActionResult> DownloadExcelReport(
        int buildingId,
        [FromQuery] string? systemType,
        [FromQuery] string? unitType)
    {
        if (HasPartialEquipmentSelectionFilter(systemType, unitType))
            return BadRequest("Both systemType and unitType must be provided to include equipment selection.");

        var report = await _buildingReportDataService.BuildReportAsync(
            buildingId,
            systemType,
            unitType);

        if (report == null)
            return NotFound();

        var content = _excelReportService.GenerateBuildingReport(report);
        var fileName = $"building-{buildingId}-report.xlsx";

        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet("{buildingId}/report")]
    public async Task<ActionResult<BuildingReport>> GetReport(
        int buildingId,
        [FromQuery] string? systemType,
        [FromQuery] string? unitType)
    {
        if (HasPartialEquipmentSelectionFilter(systemType, unitType))
            return BadRequest("Both systemType and unitType must be provided to include equipment selection.");

        var report = await _buildingReportDataService.BuildReportAsync(
            buildingId,
            systemType,
            unitType);

        if (report == null)
            return NotFound();

        return Ok(report);
    }

    private static bool HasPartialEquipmentSelectionFilter(string? systemType, string? unitType)
    {
        return string.IsNullOrWhiteSpace(systemType) != string.IsNullOrWhiteSpace(unitType);
    }
}
