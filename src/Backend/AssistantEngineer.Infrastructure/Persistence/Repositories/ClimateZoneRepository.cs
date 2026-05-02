using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Climate;

namespace AssistantEngineer.Infrastructure.Persistence.Repositories;

internal sealed class ClimateZoneRepository : IClimateZoneRepository
{
    private readonly AppDbContext _context;

    public ClimateZoneRepository(AppDbContext context) => _context = context;

    public async Task<ClimateZone?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.ClimateZones.FindAsync([id], cancellationToken);
}
