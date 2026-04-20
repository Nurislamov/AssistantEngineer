using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Infrastructure.Data.Repositories;

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
