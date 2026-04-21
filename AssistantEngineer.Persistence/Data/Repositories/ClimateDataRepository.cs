using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Persistence.Data.Repositories;

internal sealed class ClimateDataRepository : IClimateDataRepository
{
    private readonly AppDbContext _context;

    public ClimateDataRepository(AppDbContext context) => _context = context;

    public async Task<ClimateData?> GetForClimateZoneAsync(
        int climateZoneId,
        int month,
        CancellationToken cancellationToken = default)
    {
        return await _context.ClimateData
            .Include(cd => cd.HourlyData.OrderBy(h => h.Hour))
            .FirstOrDefaultAsync(cd => cd.ClimateZoneId == climateZoneId && cd.Month == month, cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetAvailableMonthsForClimateZoneAsync(
        int climateZoneId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ClimateData
            .AsNoTracking()
            .Where(cd => cd.ClimateZoneId == climateZoneId)
            .OrderBy(cd => cd.Month)
            .Select(cd => cd.Month)
            .ToListAsync(cancellationToken);
    }
}
