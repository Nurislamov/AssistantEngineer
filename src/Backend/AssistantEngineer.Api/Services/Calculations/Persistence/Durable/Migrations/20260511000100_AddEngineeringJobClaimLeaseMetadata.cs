using AssistantEngineer.Api.Services.Calculations.Persistence.Durable;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistantEngineer.Api.Services.Calculations.Persistence.Durable.Migrations;

[DbContext(typeof(EngineeringWorkflowPersistenceDbContext))]
[Migration("20260511000100_AddEngineeringJobClaimLeaseMetadata")]
public partial class AddEngineeringJobClaimLeaseMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ClaimedByWorkerId",
            table: "engineering_workflow_jobs",
            type: "TEXT",
            maxLength: 196,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ClaimedAtUtc",
            table: "engineering_workflow_jobs",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "LeaseExpiresAtUtc",
            table: "engineering_workflow_jobs",
            type: "TEXT",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_jobs_LeaseExpiresAtUtc",
            table: "engineering_workflow_jobs",
            column: "LeaseExpiresAtUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_engineering_workflow_jobs_LeaseExpiresAtUtc",
            table: "engineering_workflow_jobs");

        migrationBuilder.DropColumn(
            name: "ClaimedByWorkerId",
            table: "engineering_workflow_jobs");

        migrationBuilder.DropColumn(
            name: "ClaimedAtUtc",
            table: "engineering_workflow_jobs");

        migrationBuilder.DropColumn(
            name: "LeaseExpiresAtUtc",
            table: "engineering_workflow_jobs");
    }
}
