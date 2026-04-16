using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Contracts.Requests;
using AssistantEngineer.Contracts.Responses;
using AssistantEngineer.Domain.Models;

namespace AssistantEngineer.Application.Services.Floors;

public class FloorApplicationService
{
    private readonly IAppDbContext _context;

    public FloorApplicationService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<FloorResponse?> CreateAsync(int buildingId, CreateFloorRequest request)
    {
        var buildingExists = _context.Buildings.Any(b => b.Id == buildingId);
        if (!buildingExists)
            return null;

        var floor = new Floor
        {
            Name = request.Name,
            BuildingId = buildingId
        };

        _context.AddFloor(floor);
        await _context.SaveChangesAsync();

        return ToResponse(floor);
    }

    public async Task<List<FloorResponse>> GetByBuildingIdAsync(int buildingId)
    {
        return _context.Floors
            .Where(f => f.BuildingId == buildingId)
            .Select(floor => new FloorResponse
            {
                Id = floor.Id,
                Name = floor.Name,
                BuildingId = floor.BuildingId,
                DesignReserveFactor = floor.DesignReserveFactor,
                DesignCapacityW = floor.DesignCapacityW,
                DesignCapacityKw = floor.DesignCapacityKw
            })
            .ToList();
    }

    public async Task<FloorResponse?> GetByIdAsync(int id)
    {
        return _context.Floors
            .Where(floor => floor.Id == id)
            .Select(floor => new FloorResponse
            {
                Id = floor.Id,
                Name = floor.Name,
                BuildingId = floor.BuildingId,
                DesignReserveFactor = floor.DesignReserveFactor,
                DesignCapacityW = floor.DesignCapacityW,
                DesignCapacityKw = floor.DesignCapacityKw
            })
            .FirstOrDefault();
    }

    private static FloorResponse ToResponse(Floor floor)
    {
        return new FloorResponse
        {
            Id = floor.Id,
            Name = floor.Name,
            BuildingId = floor.BuildingId,
            DesignReserveFactor = floor.DesignReserveFactor,
            DesignCapacityW = floor.DesignCapacityW,
            DesignCapacityKw = floor.DesignCapacityKw
        };
    }
}
