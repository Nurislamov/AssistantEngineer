using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Services.Calculations.Persistence.Durable;

public sealed class EngineeringWorkflowPersistenceDatabaseInitializer
{
    private readonly EngineeringWorkflowPersistenceOptions _options;
    private int _initialized;

    public EngineeringWorkflowPersistenceDatabaseInitializer(IOptions<EngineeringWorkflowPersistenceOptions> options)
    {
        _options = options.Value;
    }

    public void EnsureInitialized(EngineeringWorkflowPersistenceDbContext dbContext)
    {
        if (_options.Provider != EngineeringWorkflowPersistenceProvider.SQLite || !_options.EnsureCreatedOnStartup)
        {
            return;
        }

        if (Interlocked.Exchange(ref _initialized, 1) == 1)
        {
            return;
        }

        dbContext.Database.EnsureCreated();
    }
}
