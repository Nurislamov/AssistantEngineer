using AssistantEngineer.Contracts;
using AssistantEngineer.Contracts.Results;
using AssistantEngineer.Data;
using AssistantEngineer.Models;
using AssistantEngineer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BuildingsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly StructureCalculationService _structureCalculationService;
    private readonly BuildingReportService _buildingReportService;
    private readonly ExcelReportService _excelReportService;

    public BuildingsController(
        AppDbContext context,
        StructureCalculationService structureCalculationService,
        BuildingReportService buildingReportService,
        ExcelReportService excelReportService)
    {
        _context = context;
        _structureCalculationService = structureCalculationService;
        _buildingReportService = buildingReportService;
        _excelReportService = excelReportService;
    }

    [HttpPost("{projectId}")]
    public async Task<ActionResult<BuildingResponse>> CreateBuilding(int projectId, CreateBuildingRequest request)
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
                ReserveFactor = building.ReserveFactor,
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
                ReserveFactor = building.ReserveFactor,
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
        var result = await _structureCalculationService.CalculateBuildingAsync(buildingId);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("{buildingId}/report/excel")]
    public async Task<IActionResult> DownloadExcelReport(int buildingId)
    {
        var calculation = await _structureCalculationService.CalculateBuildingAsync(buildingId);

        if (calculation == null)
            return NotFound();

        var report = await _buildingReportService.BuildReportAsync(buildingId);

        if (report == null)
            return NotFound();

        var content = _excelReportService.GenerateBuildingReport(report);
        var fileName = $"building-{buildingId}-report.xlsx";

        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private static BuildingResponse ToResponse(Building building)
    {
        return new BuildingResponse
        {
            Id = building.Id,
            Name = building.Name,
            ProjectId = building.ProjectId,
            ReserveFactor = building.ReserveFactor,
            DesignCapacityW = building.DesignCapacityW,
            DesignCapacityKw = building.DesignCapacityKw
        };
    }
}
