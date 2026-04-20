using AssistantEngineer.Domain.Models;

namespace AssistantEngineer.Application.Abstractions;

public interface IClimateZoneRepository
{
    Task<ClimateZone?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
