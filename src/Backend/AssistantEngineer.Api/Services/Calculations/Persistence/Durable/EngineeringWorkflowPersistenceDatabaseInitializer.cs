using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Services.Calculations.Persistence.Durable;

public sealed class EngineeringWorkflowPersistenceDatabaseInitializer
{
    public const string InitialMigrationId = "20260510000100_InitialEngineeringWorkflowPersistence";
    public const string JobClaimLeaseMigrationId = "20260511000100_AddEngineeringJobClaimLeaseMetadata";
    public const string IdempotencyRecordsMigrationId = "20260511000200_AddEngineeringWorkflowIdempotencyRecords";
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

            dbContext.Database.ExecuteSqlRaw(
                "INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1});",
                InitialMigrationId,
                InitialMigrationProductVersion);

            if (SqliteColumnExists(connection, "engineering_workflow_jobs", "ClaimedByWorkerId") &&
                SqliteColumnExists(connection, "engineering_workflow_jobs", "ClaimedAtUtc") &&
                SqliteColumnExists(connection, "engineering_workflow_jobs", "LeaseExpiresAtUtc"))
            {
                dbContext.Database.ExecuteSqlRaw(
                    "INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1});",
                    JobClaimLeaseMigrationId,
                    InitialMigrationProductVersion);
            }

            if (SqliteObjectExists(connection, "table", "engineering_workflow_idempotency_records"))
            {
                dbContext.Database.ExecuteSqlRaw(
                    "INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1});",
                    IdempotencyRecordsMigrationId,
                    InitialMigrationProductVersion);
            }
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

    private static bool SqliteColumnExists(DbConnection connection, string tableName, string columnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\")";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var name = reader.GetString(1);
            if (name.Equals(columnName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
