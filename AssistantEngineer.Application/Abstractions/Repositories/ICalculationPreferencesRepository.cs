using AssistantEngineer.Domain.Models;

namespace AssistantEngineer.Application.Abstractions;

public interface ICalculationPreferencesRepository
{
    Task<CalculationPreferences?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
}
