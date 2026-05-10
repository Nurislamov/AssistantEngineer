using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Services.Calculations.Persistence.Durable;

public sealed class EngineeringWorkflowPersistenceDatabaseInitializer
{
    public const string InitialMigrationId = "20260510000100_InitialEngineeringWorkflowPersistence";
    private const string InitialMigrationProductVersion = "10.0.6";

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

        BaselineExistingEnsureCreatedSchemaIfRequired(dbContext);
        dbContext.Database.Migrate();
    }

    private static void BaselineExistingEnsureCreatedSchemaIfRequired(EngineeringWorkflowPersistenceDbContext dbContext)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            if (!SqliteObjectExists(connection, "table", "engineering_workflow_projects"))
            {
                return;
            }

            if (!SqliteObjectExists(connection, "table", "__EFMigrationsHistory"))
            {
                dbContext.Database.ExecuteSqlRaw(
                    "CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (\"MigrationId\" TEXT NOT NULL CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY, \"ProductVersion\" TEXT NOT NULL);");
            }

            if (MigrationHistoryContains(connection, InitialMigrationId))
            {
                return;
            }

            dbContext.Database.ExecuteSqlRaw(
                "INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1});",
                InitialMigrationId,
                InitialMigrationProductVersion);
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }

    private static bool SqliteObjectExists(DbConnection connection, string objectType, string objectName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type = $type AND name = $name";

        var typeParameter = command.CreateParameter();
        typeParameter.ParameterName = "$type";
        typeParameter.Value = objectType;
        command.Parameters.Add(typeParameter);

        var nameParameter = command.CreateParameter();
        nameParameter.ParameterName = "$name";
        nameParameter.Value = objectName;
        command.Parameters.Add(nameParameter);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static bool MigrationHistoryContains(DbConnection connection, string migrationId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = $migrationId";

        var migrationParameter = command.CreateParameter();
        migrationParameter.ParameterName = "$migrationId";
        migrationParameter.Value = migrationId;
        command.Parameters.Add(migrationParameter);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }
}