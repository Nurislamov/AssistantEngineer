using AssistantEngineer.Api.Services.Calculations.Persistence.Durable;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistantEngineer.Api.Services.Calculations.Persistence.Durable.Migrations;

[DbContext(typeof(EngineeringWorkflowPersistenceDbContext))]
[Migration("20260511000200_AddEngineeringWorkflowIdempotencyRecords")]
public partial class AddEngineeringWorkflowIdempotencyRecords : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "engineering_workflow_idempotency_records",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Scope = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                RequestFingerprint = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                ResponseJson = table.Column<string>(type: "TEXT", nullable: true),
                ResponseReferenceId = table.Column<string>(type: "TEXT", maxLength: 196, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                FailureReason = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_engineering_workflow_idempotency_records", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_idempotency_records_CreatedAtUtc",
            table: "engineering_workflow_idempotency_records",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_idempotency_records_ExpiresAtUtc",
            table: "engineering_workflow_idempotency_records",
            column: "ExpiresAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_idempotency_records_Scope_IdempotencyKey",
            table: "engineering_workflow_idempotency_records",
            columns: new[] { "Scope", "IdempotencyKey" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_engineering_workflow_idempotency_records_UpdatedAtUtc",
            table: "engineering_workflow_idempotency_records",
            column: "UpdatedAtUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "engineering_workflow_idempotency_records");
    }
}
