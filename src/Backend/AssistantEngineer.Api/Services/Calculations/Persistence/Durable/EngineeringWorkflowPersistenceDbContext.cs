using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Api.Services.Calculations.Persistence.Durable;

public sealed class EngineeringWorkflowPersistenceDbContext : DbContext
{
    public EngineeringWorkflowPersistenceDbContext(
        DbContextOptions<EngineeringWorkflowPersistenceDbContext> options)
        : base(options)
    {
    }

    public DbSet<EngineeringProjectEntity> Projects => Set<EngineeringProjectEntity>();

    public DbSet<EngineeringWorkflowStateEntity> WorkflowStates => Set<EngineeringWorkflowStateEntity>();

    public DbSet<EngineeringCalculationScenarioEntity> Scenarios => Set<EngineeringCalculationScenarioEntity>();

    public DbSet<EngineeringCalculationArtifactEntity> Artifacts => Set<EngineeringCalculationArtifactEntity>();

    public DbSet<EngineeringScenarioHistoryEntryEntity> HistoryEntries => Set<EngineeringScenarioHistoryEntryEntity>();

    public DbSet<EngineeringCalculationJobEntity> Jobs => Set<EngineeringCalculationJobEntity>();

    public DbSet<EngineeringCalculationJobEventEntity> JobEvents => Set<EngineeringCalculationJobEventEntity>();

    public DbSet<EngineeringWorkflowIdempotencyRecordEntity> IdempotencyRecords => Set<EngineeringWorkflowIdempotencyRecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EngineeringProjectEntity>(entity =>
        {
            entity.ToTable("engineering_workflow_projects");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedNever();
            entity.Property(item => item.Name).HasMaxLength(256).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(2048);
            entity.Property(item => item.Status).HasMaxLength(64).IsRequired();
            entity.Property(item => item.MetadataJson).HasColumnType("TEXT");
            entity.Property(item => item.CreatedAtUtc).IsRequired();
            entity.Property(item => item.UpdatedAtUtc).IsRequired();
            entity.HasIndex(item => item.CreatedAtUtc);
            entity.HasIndex(item => item.UpdatedAtUtc);
        });

        modelBuilder.Entity<EngineeringWorkflowStateEntity>(entity =>
        {
            entity.ToTable("engineering_workflow_states");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasMaxLength(128).IsRequired();
            entity.Property(item => item.CurrentStep).HasMaxLength(128).IsRequired();
            entity.Property(item => item.StateJson).HasColumnType("TEXT").IsRequired();
            entity.Property(item => item.ValidationDiagnosticsJson).HasColumnType("TEXT");
            entity.Property(item => item.CreatedAtUtc).IsRequired();
            entity.Property(item => item.UpdatedAtUtc).IsRequired();
            entity.HasIndex(item => item.ProjectId);
            entity.HasIndex(item => new { item.ProjectId, item.Version }).IsUnique();
            entity.HasIndex(item => item.CreatedAtUtc);
            entity.HasOne(item => item.Project)
                .WithMany(item => item.WorkflowStates)
                .HasForeignKey(item => item.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EngineeringCalculationScenarioEntity>(entity =>
        {
            entity.ToTable("engineering_workflow_scenarios");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasMaxLength(128).IsRequired();
            entity.Property(item => item.ScenarioKind).HasMaxLength(64).IsRequired();
            entity.Property(item => item.ExecutionMode).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(64).IsRequired();
            entity.Property(item => item.RequestJson).HasColumnType("TEXT").IsRequired();
            entity.Property(item => item.ResultSummaryJson).HasColumnType("TEXT");
            entity.Property(item => item.DiagnosticsJson).HasColumnType("TEXT");
            entity.Property(item => item.DurationMs);
            entity.HasIndex(item => item.ProjectId);
            entity.HasIndex(item => item.CreatedAtUtc);
            entity.HasOne(item => item.Project)
                .WithMany(item => item.Scenarios)
                .HasForeignKey(item => item.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EngineeringCalculationArtifactEntity>(entity =>
        {
            entity.ToTable("engineering_workflow_artifacts");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasMaxLength(196).IsRequired();
            entity.Property(item => item.ScenarioId).HasMaxLength(128).IsRequired();
            entity.Property(item => item.ArtifactKind).HasMaxLength(64).IsRequired();
            entity.Property(item => item.ContentType).HasMaxLength(128).IsRequired();
            entity.Property(item => item.Content).HasColumnType("TEXT").IsRequired();
            entity.Property(item => item.Checksum).HasMaxLength(256);
            entity.HasIndex(item => item.ScenarioId);
            entity.HasIndex(item => new { item.ScenarioId, item.ArtifactKind });
            entity.HasIndex(item => new { item.ScenarioId, item.ArtifactKind, item.CreatedAtUtc, item.Id });
            entity.HasIndex(item => item.CreatedAtUtc);
            entity.HasOne(item => item.Scenario)
                .WithMany(item => item.Artifacts)
                .HasForeignKey(item => item.ScenarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EngineeringScenarioHistoryEntryEntity>(entity =>
        {
            entity.ToTable("engineering_workflow_history_entries");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasMaxLength(196).IsRequired();
            entity.Property(item => item.ScenarioId).HasMaxLength(128).IsRequired();
            entity.Property(item => item.EventKind).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Message).HasMaxLength(4096).IsRequired();
            entity.Property(item => item.DiagnosticsJson).HasColumnType("TEXT");
            entity.HasIndex(item => item.ProjectId);
            entity.HasIndex(item => item.ScenarioId);
            entity.HasIndex(item => item.CreatedAtUtc);
            entity.HasOne(item => item.Scenario)
                .WithMany(item => item.HistoryEntries)
                .HasForeignKey(item => item.ScenarioId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Project)
                .WithMany(item => item.HistoryEntries)
                .HasForeignKey(item => item.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EngineeringCalculationJobEntity>(entity =>
        {
            entity.ToTable("engineering_workflow_jobs");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasMaxLength(128).IsRequired();
            entity.Property(item => item.ScenarioId).HasMaxLength(128).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(64).IsRequired();
            entity.Property(item => item.ExecutionMode).HasMaxLength(64).IsRequired();
            entity.Property(item => item.RequestJson).HasColumnType("TEXT").IsRequired();
            entity.Property(item => item.ResultSummaryJson).HasColumnType("TEXT");
            entity.Property(item => item.DiagnosticsJson).HasColumnType("TEXT");
            entity.Property(item => item.CurrentStep).HasMaxLength(128).IsRequired();
            entity.Property(item => item.DurationMs);
            entity.Property(item => item.CancellationRequested).IsRequired();
            entity.Property(item => item.ClaimedByWorkerId).HasMaxLength(196);
            entity.HasIndex(item => item.ProjectId);
            entity.HasIndex(item => item.ScenarioId);
            entity.HasIndex(item => item.Status);
            entity.HasIndex(item => item.CreatedAtUtc);
            entity.HasIndex(item => item.UpdatedAtUtc);
            entity.HasIndex(item => item.LeaseExpiresAtUtc);
            entity.HasIndex(item => new { item.Status, item.CancellationRequested, item.QueuedAtUtc, item.CreatedAtUtc, item.Id });
            entity.HasOne(item => item.Project)
                .WithMany()
                .HasForeignKey(item => item.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Scenario)
                .WithMany(item => item.Jobs)
                .HasForeignKey(item => item.ScenarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EngineeringCalculationJobEventEntity>(entity =>
        {
            entity.ToTable("engineering_workflow_job_events");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasMaxLength(196).IsRequired();
            entity.Property(item => item.JobId).HasMaxLength(128).IsRequired();
            entity.Property(item => item.ScenarioId).HasMaxLength(128).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(64).IsRequired();
            entity.Property(item => item.EventKind).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Message).HasMaxLength(4096).IsRequired();
            entity.Property(item => item.DiagnosticsJson).HasColumnType("TEXT");
            entity.HasIndex(item => item.ProjectId);
            entity.HasIndex(item => item.ScenarioId);
            entity.HasIndex(item => item.JobId);
            entity.HasIndex(item => item.CreatedAtUtc);
            entity.HasOne(item => item.Job)
                .WithMany(item => item.Events)
                .HasForeignKey(item => item.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EngineeringWorkflowIdempotencyRecordEntity>(entity =>
        {
            entity.ToTable("engineering_workflow_idempotency_records");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Scope).HasMaxLength(256).IsRequired();
            entity.Property(item => item.IdempotencyKey).HasMaxLength(256).IsRequired();
            entity.Property(item => item.RequestFingerprint).HasMaxLength(128).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(32).IsRequired();
            entity.Property(item => item.ResponseJson).HasColumnType("TEXT");
            entity.Property(item => item.ResponseReferenceId).HasMaxLength(196);
            entity.Property(item => item.FailureReason).HasMaxLength(2048);
            entity.HasIndex(item => new { item.Scope, item.IdempotencyKey }).IsUnique();
            entity.HasIndex(item => item.ExpiresAtUtc);
            entity.HasIndex(item => item.UpdatedAtUtc);
            entity.HasIndex(item => item.CreatedAtUtc);
        });

        base.OnModelCreating(modelBuilder);
    }
}
