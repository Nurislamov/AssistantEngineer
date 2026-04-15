using AssistantEngineer.Contracts.Calculations;
using AssistantEngineer.Contracts.Reports;
using AssistantEngineer.Contracts.Requests;
using AssistantEngineer.Contracts.Responses;
using AssistantEngineer.Data;
using AssistantEngineer.Models;
using AssistantEngineer.Services.Calculations;
using AssistantEngineer.Services.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BuildingsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AggregateCalculationService _aggregateCalculationService;
    private readonly BuildingReportDataService _buildingReportDataService;
    private readonly ExcelReportService _excelReportService;

    public BuildingsController(
        AppDbContext context,
        AggregateCalculationService aggregateCalculationService,
        BuildingReportDataService buildingReportDataService,
        ExcelReportService excelReportService)
    {
        _context = context;
        _aggregateCalculationService = aggregateCalculationService;
        _buildingReportDataService = buildingReportDataService;
        _excelReportService = excelReportService;
    }

    [HttpPost("{projectId}")]
    public async Task<ActionResult<BuildingResponse>> CreateBuilding(
        int projectId,
        CreateBuildingRequest request)
    {
        var projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId);
        if (!projectExists)
            return NotFound($"Project with id {projectId} not found.");

        var building = new Building
        {
            Name = request.Name,
            ProjectId = projectId
        };

        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var response = ToResponse(building);
        return CreatedAtAction(nameof(GetBuilding), new { id = building.Id }, response);
    }

    [HttpGet("{projectId}")]
    public async Task<ActionResult<IEnumerable<BuildingResponse>>> GetBuildings(int projectId)
    {
        var buildings = await _context.Buildings
            .Where(b => b.ProjectId == projectId)
            .Select(building => new BuildingResponse
            {
                Id = building.Id,
                Name = building.Name,
                ProjectId = building.ProjectId,
                DesignReserveFactor = building.DesignReserveFactor,
                DesignCapacityW = building.DesignCapacityW,
                DesignCapacityKw = building.DesignCapacityKw
            })
            .ToListAsync();

        return Ok(buildings);
    }

    [HttpGet("by-id/{id}")]
    public async Task<ActionResult<BuildingResponse>> GetBuilding(int id)
    {
        var building = await _context.Buildings
            .Where(building => building.Id == id)
            .Select(building => new BuildingResponse
            {
                Id = building.Id,
                Name = building.Name,
                ProjectId = building.ProjectId,
                DesignReserveFactor = building.DesignReserveFactor,
                DesignCapacityW = building.DesignCapacityW,
                DesignCapacityKw = building.DesignCapacityKw
            })
            .FirstOrDefaultAsync();

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

    private static BuildingResponse ToResponse(Building building)
    {
        return new BuildingResponse
        {
            Id = building.Id,
            Name = building.Name,
            ProjectId = building.ProjectId,
            DesignReserveFactor = building.DesignReserveFactor,
            DesignCapacityW = building.DesignCapacityW,
            DesignCapacityKw = building.DesignCapacityKw
        };
    }
}
