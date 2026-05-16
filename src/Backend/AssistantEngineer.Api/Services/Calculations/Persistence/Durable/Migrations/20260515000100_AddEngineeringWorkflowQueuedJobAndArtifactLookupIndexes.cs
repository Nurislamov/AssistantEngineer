using AssistantEngineer.Api.Services.Calculations.Persistence.Durable;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistantEngineer.Api.Services.Calculations.Persistence.Durable.Migrations;

[DbContext(typeof(EngineeringWorkflowPersistenceDbContext))]
[Migration("20260515000100_AddEngineeringWorkflowQueuedJobAndArtifactLookupIndexes")]
public partial class AddEngineeringWorkflowQueuedJobAndArtifactLookupIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE INDEX IF NOT EXISTS IX_engineering_workflow_artifacts_ScenarioId_ArtifactKind_CreatedAtUtc_Id
            ON engineering_workflow_artifacts (ScenarioId, ArtifactKind, CreatedAtUtc, Id);
            """);

        migrationBuilder.Sql(
            """
            CREATE INDEX IF NOT EXISTS IX_engineering_workflow_jobs_Status_CancellationRequested_QueuedAtUtc_CreatedAtUtc_Id
            ON engineering_workflow_jobs (Status, CancellationRequested, QueuedAtUtc, CreatedAtUtc, Id);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS IX_engineering_workflow_artifacts_ScenarioId_ArtifactKind_CreatedAtUtc_Id;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS IX_engineering_workflow_jobs_Status_CancellationRequested_QueuedAtUtc_CreatedAtUtc_Id;");
    }
}
