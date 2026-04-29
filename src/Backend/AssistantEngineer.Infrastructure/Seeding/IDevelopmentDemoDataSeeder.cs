using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Infrastructure.Seeding;

public interface IDevelopmentDemoDataSeeder
{
    Task<Result<DevelopmentDemoSeedResult>> SeedAsync(CancellationToken cancellationToken = default);
}
