using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Persistence.Data.Repositories;

internal sealed class CalculationPreferencesRepository : ICalculationPreferencesRepository
{
    private readonly AppDbContext _context;

    public CalculationPreferencesRepository(AppDbContext context) => _context = context;

    public async Task<CalculationPreferences?> GetByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken = default) =>
        await _context.CalculationPreferences
            .FirstOrDefaultAsync(preferences => preferences.ProjectId == projectId, cancellationToken);
}
