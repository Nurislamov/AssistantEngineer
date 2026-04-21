using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Infrastructure.Data.Repositories;

internal sealed class AnnualClimateDataRepository : IAnnualClimateDataRepository
{
    private readonly AppDbContext _context;

    public AnnualClimateDataRepository(AppDbContext context) => _context = context;

    public async Task<AnnualClimateData?> GetForClimateZoneAsync(
        int climateZoneId,
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _context.AnnualClimateData
            .Include(x => x.HourlyData.OrderBy(h => h.HourOfYear))
            .FirstOrDefaultAsync(x => x.ClimateZoneId == climateZoneId && x.Year == year, cancellationToken);
    }

    public async Task ReplaceForClimateZoneAsync(
        AnnualClimateData annualClimateData,
        CancellationToken cancellationToken = default)
    {
        await _context.AnnualClimateData
            .Where(data =>
                data.ClimateZoneId == annualClimateData.ClimateZoneId &&
                data.Year == annualClimateData.Year)
            .ExecuteDeleteAsync(cancellationToken);

        _context.AnnualClimateData.Add(annualClimateData);
    }
}
