using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Domain.Models;

namespace AssistantEngineer.Infrastructure.Data.Repositories;

internal sealed class ClimateZoneRepository : IClimateZoneRepository
{
    private readonly AppDbContext _context;

    public ClimateZoneRepository(AppDbContext context) => _context = context;

    public async Task<ClimateZone?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.ClimateZones.FindAsync([id], cancellationToken);
}
