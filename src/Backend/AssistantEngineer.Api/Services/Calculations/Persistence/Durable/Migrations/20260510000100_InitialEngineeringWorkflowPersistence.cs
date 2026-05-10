using AssistantEngineer.Api.Services.Calculations.Persistence.Durable;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistantEngineer.Api.Services.Calculations.Persistence.Durable.Migrations;

[DbContext(typeof(EngineeringWorkflowPersistenceDbContext))]
[Migration("20260510000100_InitialEngineeringWorkflowPersistence")]
public partial class InitialEngineeringWorkflowPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "engineering_workflow_projects",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_engineering_workflow_projects", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "engineering_workflow_scenarios",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                BuildingId = table.Column<int>(type: "INTEGER", nullable: true),
                ScenarioKind = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                ExecutionMode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                RequestJson = table.Column<string>(type: "TEXT", nullable: false),
                ResultSummaryJson = table.Column<string>(type: "TEXT", nullable: true),
                DiagnosticsJson = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                DurationMs = table.Column<double>(type: "REAL", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_engineering_workflow_scenarios", x => x.Id);
                table.ForeignKey(
                    name: "FK_engineering_workflow_scenarios_engineering_workflow_projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "engineering_workflow_projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "engineering_workflow_states",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                BuildingId = table.Column<int>(type: "INTEGER", nullable: true),
                Version = table.Column<int>(type: "INTEGER", nullable: false),
                CurrentStep = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                StateJson = table.Column<string>(type: "TEXT", nullable: false),
                ValidationDiagnosticsJson = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_engineering_workflow_states", x => x.Id);
                table.ForeignKey(
                    name: "FK_engineering_workflow_states_engineering_workflow_projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "engineering_workflow_projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "engineering_workflow_artifacts",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 196, nullable: false),
                ScenarioId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                ArtifactKind = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                ContentType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                Content = table.Column<string>(type: "TEXT", nullable: false),
                SizeBytes = table.Column<int>(type: "INTEGER", nullable: true),
                Checksum = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_engineering_workflow_artifacts", x => x.Id);
                table.ForeignKey(
                    name: "FK_engineering_workflow_artifacts_engineering_workflow_scenarios_ScenarioId",
                    column: x => x.ScenarioId,
                    principalTable: "engineering_workflow_scenarios",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "engineering_workflow_history_entries",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 196, nullable: false),
                ScenarioId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                EventKind = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Message = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                DiagnosticsJson = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_engineering_workflow_history_entries", x => x.Id);
                table.ForeignKey(
                    name: "FK_engineering_workflow_history_entries_engineering_workflow_projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "engineering_workflow_projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_engineering_workflow_history_entries_engineering_workflow_scenarios_ScenarioId",
                    column: x => x.ScenarioId,
                    principalTable: "engineering_workflow_scenarios",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "engineering_workflow_jobs",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                ScenarioId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                ExecutionMode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                RequestJson = table.Column<string>(type: "TEXT", nullable: false),
                ResultSummaryJson = table.Column<string>(type: "TEXT", nullable: true),
                DiagnosticsJson = table.Column<string>(type: "TEXT", nullable: true),
                ProgressPercent = table.Column<int>(type: "INTEGER", nullable: false),
                CurrentStep = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                QueuedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DurationMs = table.Column<double>(type: "REAL", nullable: true),
                RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                CancellationRequested = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_engineering_workflow_jobs", x => x.Id);
                table.ForeignKey(
                    name: "FK_engineering_workflow_jobs_engineering_workflow_projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "engineering_workflow_projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_engineering_workflow_jobs_engineering_workflow_scenarios_ScenarioId",
                    column: x => x.ScenarioId,
                    principalTable: "engineering_workflow_scenarios",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "engineering_workflow_job_events",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 196, nullable: false),
                JobId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                ScenarioId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                EventKind = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Message = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                DiagnosticsJson = table.Column<string>(type: "TEXT", nullable: true),
                ProgressPercent = table.Column<int>(type: "INTEGER", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_engineering_workflow_job_events", x => x.Id);
                table.ForeignKey(
                    name: "FK_engineering_workflow_job_events_engineering_workflow_jobs_JobId",
                    column: x => x.JobId,
                    principalTable: "engineering_workflow_jobs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_artifacts_CreatedAtUtc",
            table: "engineering_workflow_artifacts",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_artifacts_ScenarioId",
            table: "engineering_workflow_artifacts",
            column: "ScenarioId");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_artifacts_ScenarioId_ArtifactKind",
            table: "engineering_workflow_artifacts",
            columns: new[] { "ScenarioId", "ArtifactKind" });

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_history_entries_CreatedAtUtc",
            table: "engineering_workflow_history_entries",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_history_entries_ProjectId",
            table: "engineering_workflow_history_entries",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_history_entries_ScenarioId",
            table: "engineering_workflow_history_entries",
            column: "ScenarioId");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_job_events_CreatedAtUtc",
            table: "engineering_workflow_job_events",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_job_events_JobId",
            table: "engineering_workflow_job_events",
            column: "JobId");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_job_events_ProjectId",
            table: "engineering_workflow_job_events",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_job_events_ScenarioId",
            table: "engineering_workflow_job_events",
            column: "ScenarioId");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_jobs_CreatedAtUtc",
            table: "engineering_workflow_jobs",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_jobs_ProjectId",
            table: "engineering_workflow_jobs",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_jobs_ScenarioId",
            table: "engineering_workflow_jobs",
            column: "ScenarioId");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_jobs_Status",
            table: "engineering_workflow_jobs",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_jobs_UpdatedAtUtc",
            table: "engineering_workflow_jobs",
            column: "UpdatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_projects_CreatedAtUtc",
            table: "engineering_workflow_projects",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_projects_UpdatedAtUtc",
            table: "engineering_workflow_projects",
            column: "UpdatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_scenarios_CreatedAtUtc",
            table: "engineering_workflow_scenarios",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_scenarios_ProjectId",
            table: "engineering_workflow_scenarios",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_states_CreatedAtUtc",
            table: "engineering_workflow_states",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_states_ProjectId",
            table: "engineering_workflow_states",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_states_ProjectId_Version",
            table: "engineering_workflow_states",
            columns: new[] { "ProjectId", "Version" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "engineering_workflow_artifacts");
        migrationBuilder.DropTable(name: "engineering_workflow_history_entries");
        migrationBuilder.DropTable(name: "engineering_workflow_job_events");
        migrationBuilder.DropTable(name: "engineering_workflow_states");
        migrationBuilder.DropTable(name: "engineering_workflow_jobs");
        migrationBuilder.DropTable(name: "engineering_workflow_scenarios");
        migrationBuilder.DropTable(name: "engineering_workflow_projects");
    }
}