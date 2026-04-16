using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Contracts.Requests;
using AssistantEngineer.Contracts.Responses;
using AssistantEngineer.Domain.Models;

namespace AssistantEngineer.Application.Services.Buildings;

public class BuildingApplicationService
{
    private readonly IAppDbContext _context;

    public BuildingApplicationService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<BuildingResponse?> CreateAsync(int projectId, CreateBuildingRequest request)
    {
        var projectExists = _context.Projects.Any(p => p.Id == projectId);
        if (!projectExists)
            return null;

        var building = new Building
        {
            Name = request.Name,
            ProjectId = projectId
        };

        _context.AddBuilding(building);
        await _context.SaveChangesAsync();

        return ToResponse(building);
    }

    public async Task<List<BuildingResponse>> GetByProjectIdAsync(int projectId)
    {
        return _context.Buildings
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
            .ToList();
    }

    public async Task<BuildingResponse?> GetByIdAsync(int id)
    {
        return _context.Buildings
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
            .FirstOrDefault();
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
