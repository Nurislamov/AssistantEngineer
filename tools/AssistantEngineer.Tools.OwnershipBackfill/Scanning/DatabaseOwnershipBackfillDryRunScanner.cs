using AssistantEngineer.Api.Services.Calculations.Persistence.Durable;
using AssistantEngineer.Infrastructure.Persistence;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace AssistantEngineer.Tools.OwnershipBackfill.Scanning;

public sealed class DatabaseOwnershipBackfillDryRunScanner : IOwnershipBackfillDryRunScanner
{
    public async Task<OwnershipBackfillDryRunResult> ScanAsync(
        OwnershipBackfillOptions options,
        CancellationToken cancellationToken = default)
    {
        var provider = NormalizeProvider(options.DatabaseProvider);
        if (provider == "None")
            throw new InvalidOperationException("Database scanner cannot run with provider None.");

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException(
                "Connection string is required for database dry-run scanner.");
        }

        var startedAtUtc = DateTimeOffset.UtcNow;
        var runId = BuildRunId(startedAtUtc);

        await using var appContext = CreateAppDbContext(provider, options.ConnectionString);
        await using var workflowContext = CreateWorkflowDbContext(provider, options.ConnectionString);

        if (!await appContext.Database.CanConnectAsync(cancellationToken))
            throw new InvalidOperationException("Unable to connect to application persistence database.");

        if (!await workflowContext.Database.CanConnectAsync(cancellationToken))
            throw new InvalidOperationException("Unable to connect to workflow persistence database.");

        var accumulator = new MetricsAccumulator();
        var projectOwnership = await ScanProjectsAsync(appContext, accumulator, options.BatchSize, cancellationToken);
        var buildingOwnership = await ScanBuildingsAsync(appContext, projectOwnership, accumulator, options.BatchSize, cancellationToken);
        var scenarioOwnership = await ScanScenariosAsync(workflowContext, projectOwnership, buildingOwnership, accumulator, options.BatchSize, cancellationToken);
        var jobOwnership = await ScanJobsAsync(workflowContext, projectOwnership, scenarioOwnership, accumulator, options.BatchSize, cancellationToken);

        await ScanWorkflowStatesAsync(workflowContext, projectOwnership, buildingOwnership, accumulator, options.BatchSize, cancellationToken);
        await ScanJobEventsAsync(workflowContext, projectOwnership, scenarioOwnership, jobOwnership, accumulator, options.BatchSize, cancellationToken);
        await ScanScenarioHistoryAsync(workflowContext, projectOwnership, scenarioOwnership, accumulator, options.BatchSize, cancellationToken);

        var completedAtUtc = DateTimeOffset.UtcNow;
        var summary = accumulator.BuildSummary(runId, startedAtUtc, completedAtUtc);

        return new OwnershipBackfillDryRunResult
        {
            Summary = summary,
            UnresolvedRecords = accumulator.UnresolvedRecords,
            PreviousValues = accumulator.PreviousValueSnapshots
        };
    }

    private static async Task<Dictionary<int, int?>> ScanProjectsAsync(
        AppDbContext appContext,
        MetricsAccumulator accumulator,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var ownershipMap = new Dictionary<int, int?>();
        var offset = 0;

        try
        {
            while (true)
            {
                var batch = await appContext.Projects
                    .AsNoTracking()
                    .OrderBy(project => project.Id)
                    .Skip(offset)
                    .Take(batchSize)
                    .Select(project => new ProjectRow(project.Id, project.OrganizationId, project.OwnerUserId))
                    .ToListAsync(cancellationToken);

                if (batch.Count == 0)
                    break;

                foreach (var row in batch)
                {
                    ownershipMap[row.Id] = row.OrganizationId;

                    accumulator.AddPrevious("Project", row.Id.ToString(), row.Id, null, row.OrganizationId, row.OwnerUserId);

                    if (row.OrganizationId.HasValue)
                    {
                        accumulator.AddResolvable("Project");
                    }
                    else
                    {
                        accumulator.AddUnresolved(
                            recordType: "Project",
                            recordId: row.Id.ToString(),
                            reason: OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing,
                            candidateProjectId: row.Id,
                            candidateBuildingId: null,
                            candidateOrganizationId: null,
                            notes: "Project.OrganizationId is null.",
                            ambiguous: false);
                    }
                }

                offset += batch.Count;
            }
        }
        catch (Exception exception) when (IsMissingTableException(exception))
        {
            return ownershipMap;
        }

        return ownershipMap;
    }

    private static async Task<Dictionary<int, BuildingOwnershipRow>> ScanBuildingsAsync(
        AppDbContext appContext,
        IReadOnlyDictionary<int, int?> projectOwnership,
        MetricsAccumulator accumulator,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<int, BuildingOwnershipRow>();
        var offset = 0;

        try
        {
            while (true)
            {
                var batch = await appContext.Buildings
                    .AsNoTracking()
                    .OrderBy(building => building.Id)
                    .Skip(offset)
                    .Take(batchSize)
                    .Select(building => new BuildingOwnershipRow(building.Id, building.ProjectId))
                    .ToListAsync(cancellationToken);

                if (batch.Count == 0)
                    break;

                foreach (var row in batch)
                {
                    map[row.Id] = row;
                    projectOwnership.TryGetValue(row.ProjectId, out var organizationId);

                    accumulator.AddPrevious("Building", row.Id.ToString(), row.ProjectId, row.Id, organizationId, null);

                    if (!projectOwnership.ContainsKey(row.ProjectId))
                    {
                        accumulator.AddUnresolved(
                            recordType: "Building",
                            recordId: row.Id.ToString(),
                            reason: OwnershipBackfillUnresolvedReasons.BuildingProjectMissing,
                            candidateProjectId: row.ProjectId,
                            candidateBuildingId: row.Id,
                            candidateOrganizationId: null,
                            notes: "Building references a project that does not exist.",
                            ambiguous: false);
                        continue;
                    }

                    if (!organizationId.HasValue)
                    {
                        accumulator.AddUnresolved(
                            recordType: "Building",
                            recordId: row.Id.ToString(),
                            reason: OwnershipBackfillUnresolvedReasons.BuildingProjectOrganizationMissing,
                            candidateProjectId: row.ProjectId,
                            candidateBuildingId: row.Id,
                            candidateOrganizationId: null,
                            notes: "Building parent project has null OrganizationId.",
                            ambiguous: false);
                        continue;
                    }

                    accumulator.AddResolvable("Building");
                }

                offset += batch.Count;
            }
        }
        catch (Exception exception) when (IsMissingTableException(exception))
        {
            return map;
        }

        return map;
    }

    private static async Task<Dictionary<string, OwnershipResolution>> ScanScenariosAsync(
        EngineeringWorkflowPersistenceDbContext workflowContext,
        IReadOnlyDictionary<int, int?> projectOwnership,
        IReadOnlyDictionary<int, BuildingOwnershipRow> buildingOwnership,
        MetricsAccumulator accumulator,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, OwnershipResolution>(StringComparer.Ordinal);
        var offset = 0;

        try
        {
            while (true)
            {
                var batch = await workflowContext.Scenarios
                    .AsNoTracking()
                    .OrderBy(scenario => scenario.Id)
                    .Skip(offset)
                    .Take(batchSize)
                    .Select(scenario => new ScenarioRow(scenario.Id, scenario.ProjectId, scenario.BuildingId))
                    .ToListAsync(cancellationToken);

                if (batch.Count == 0)
                    break;

                foreach (var row in batch)
                {
                    var sourceResults = new List<SourceResult>(2)
                    {
                        ResolveProjectSource(row.ProjectId, projectOwnership)
                    };

                    if (row.BuildingId.HasValue)
                        sourceResults.Add(ResolveBuildingSource(row.BuildingId.Value, projectOwnership, buildingOwnership));

                    var resolution = ResolveFromSources(
                        sourceResults,
                        OwnershipBackfillUnresolvedReasons.ScenarioOwnershipMetadataMissing,
                        OwnershipBackfillUnresolvedReasons.ScenarioOwnershipAmbiguous);

                    map[row.Id] = resolution;

                    accumulator.AddPrevious("Scenario", row.Id, row.ProjectId, row.BuildingId, resolution.OrganizationId, null);

                    if (resolution.IsResolvable)
                    {
                        accumulator.AddResolvable("Scenario");
                    }
                    else
                    {
                        accumulator.AddUnresolved(
                            recordType: "Scenario",
                            recordId: row.Id,
                            reason: resolution.Reason,
                            candidateProjectId: row.ProjectId,
                            candidateBuildingId: row.BuildingId,
                            candidateOrganizationId: resolution.OrganizationId,
                            notes: resolution.Notes,
                            ambiguous: resolution.IsAmbiguous);
                    }
                }

                offset += batch.Count;
            }
        }
        catch (Exception exception) when (IsMissingTableException(exception))
        {
            return map;
        }

        return map;
    }

    private static async Task<Dictionary<string, OwnershipResolution>> ScanJobsAsync(
        EngineeringWorkflowPersistenceDbContext workflowContext,
        IReadOnlyDictionary<int, int?> projectOwnership,
        IReadOnlyDictionary<string, OwnershipResolution> scenarioOwnership,
        MetricsAccumulator accumulator,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, OwnershipResolution>(StringComparer.Ordinal);
        var offset = 0;

        try
        {
            while (true)
            {
                var batch = await workflowContext.Jobs
                    .AsNoTracking()
                    .OrderBy(job => job.Id)
                    .Skip(offset)
                    .Take(batchSize)
                    .Select(job => new JobRow(job.Id, job.ProjectId, job.ScenarioId))
                    .ToListAsync(cancellationToken);

                if (batch.Count == 0)
                    break;

                foreach (var row in batch)
                {
                    var sourceResults = new List<SourceResult>(2)
                    {
                        ResolveScenarioSource(row.ScenarioId, scenarioOwnership),
                        ResolveProjectSource(row.ProjectId, projectOwnership)
                    };

                    var resolution = ResolveFromSources(
                        sourceResults,
                        OwnershipBackfillUnresolvedReasons.JobOwnershipMetadataMissing,
                        OwnershipBackfillUnresolvedReasons.JobOwnershipAmbiguous);

                    map[row.Id] = resolution;

                    accumulator.AddPrevious("Job", row.Id, row.ProjectId, null, resolution.OrganizationId, null);

                    if (resolution.IsResolvable)
                    {
                        accumulator.AddResolvable("Job");
                    }
                    else
                    {
                        accumulator.AddUnresolved(
                            recordType: "Job",
                            recordId: row.Id,
                            reason: resolution.Reason,
                            candidateProjectId: row.ProjectId,
                            candidateBuildingId: null,
                            candidateOrganizationId: resolution.OrganizationId,
                            notes: resolution.Notes,
                            ambiguous: resolution.IsAmbiguous);
                    }
                }

                offset += batch.Count;
            }
        }
        catch (Exception exception) when (IsMissingTableException(exception))
        {
            return map;
        }

        return map;
    }

    private static async Task ScanWorkflowStatesAsync(
        EngineeringWorkflowPersistenceDbContext workflowContext,
        IReadOnlyDictionary<int, int?> projectOwnership,
        IReadOnlyDictionary<int, BuildingOwnershipRow> buildingOwnership,
        MetricsAccumulator accumulator,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var offset = 0;

        try
        {
            while (true)
            {
                var batch = await workflowContext.WorkflowStates
                    .AsNoTracking()
                    .OrderBy(state => state.Id)
                    .Skip(offset)
                    .Take(batchSize)
                    .Select(state => new WorkflowStateRow(state.Id, state.ProjectId, state.BuildingId))
                    .ToListAsync(cancellationToken);

                if (batch.Count == 0)
                    break;

                foreach (var row in batch)
                {
                    var sourceResults = new List<SourceResult>(2)
                    {
                        ResolveProjectSource(row.ProjectId, projectOwnership)
                    };

                    if (row.BuildingId.HasValue)
                        sourceResults.Add(ResolveBuildingSource(row.BuildingId.Value, projectOwnership, buildingOwnership));

                    var resolution = ResolveFromSources(
                        sourceResults,
                        OwnershipBackfillUnresolvedReasons.WorkflowStateOwnershipMetadataMissing,
                        OwnershipBackfillUnresolvedReasons.WorkflowStateOwnershipAmbiguous);

                    accumulator.AddPrevious("WorkflowState", row.Id, row.ProjectId, row.BuildingId, resolution.OrganizationId, null);

                    if (resolution.IsResolvable)
                    {
                        accumulator.AddResolvable("WorkflowState");
                    }
                    else
                    {
                        accumulator.AddUnresolved(
                            recordType: "WorkflowState",
                            recordId: row.Id,
                            reason: resolution.Reason,
                            candidateProjectId: row.ProjectId,
                            candidateBuildingId: row.BuildingId,
                            candidateOrganizationId: resolution.OrganizationId,
                            notes: resolution.Notes,
                            ambiguous: resolution.IsAmbiguous);
                    }
                }

                offset += batch.Count;
            }
        }
        catch (Exception exception) when (IsMissingTableException(exception))
        {
            return;
        }
    }

    private static async Task ScanJobEventsAsync(
        EngineeringWorkflowPersistenceDbContext workflowContext,
        IReadOnlyDictionary<int, int?> projectOwnership,
        IReadOnlyDictionary<string, OwnershipResolution> scenarioOwnership,
        IReadOnlyDictionary<string, OwnershipResolution> jobOwnership,
        MetricsAccumulator accumulator,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var offset = 0;

        try
        {
            while (true)
            {
                var batch = await workflowContext.JobEvents
                    .AsNoTracking()
                    .OrderBy(jobEvent => jobEvent.Id)
                    .Skip(offset)
                    .Take(batchSize)
                    .Select(jobEvent => new JobEventRow(jobEvent.Id, jobEvent.JobId, jobEvent.ScenarioId, jobEvent.ProjectId))
                    .ToListAsync(cancellationToken);

                if (batch.Count == 0)
                    break;

                foreach (var row in batch)
                {
                    var sourceResults = new List<SourceResult>(3)
                    {
                        ResolveJobSource(row.JobId, jobOwnership),
                        ResolveScenarioSource(row.ScenarioId, scenarioOwnership),
                        ResolveProjectSource(row.ProjectId, projectOwnership)
                    };

                    var resolution = ResolveFromSources(
                        sourceResults,
                        OwnershipBackfillUnresolvedReasons.JobEventOwnershipMetadataMissing,
                        OwnershipBackfillUnresolvedReasons.JobEventOwnershipAmbiguous);

                    accumulator.AddPrevious("JobEvent", row.Id, row.ProjectId, null, resolution.OrganizationId, null);

                    if (resolution.IsResolvable)
                    {
                        accumulator.AddResolvable("JobEvent");
                    }
                    else
                    {
                        accumulator.AddUnresolved(
                            recordType: "JobEvent",
                            recordId: row.Id,
                            reason: resolution.Reason,
                            candidateProjectId: row.ProjectId,
                            candidateBuildingId: null,
                            candidateOrganizationId: resolution.OrganizationId,
                            notes: resolution.Notes,
                            ambiguous: resolution.IsAmbiguous);
                    }
                }

                offset += batch.Count;
            }
        }
        catch (Exception exception) when (IsMissingTableException(exception))
        {
            return;
        }
    }

    private static async Task ScanScenarioHistoryAsync(
        EngineeringWorkflowPersistenceDbContext workflowContext,
        IReadOnlyDictionary<int, int?> projectOwnership,
        IReadOnlyDictionary<string, OwnershipResolution> scenarioOwnership,
        MetricsAccumulator accumulator,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var offset = 0;

        try
        {
            while (true)
            {
                var batch = await workflowContext.HistoryEntries
                    .AsNoTracking()
                    .OrderBy(entry => entry.Id)
                    .Skip(offset)
                    .Take(batchSize)
                    .Select(entry => new ScenarioHistoryRow(entry.Id, entry.ScenarioId, entry.ProjectId))
                    .ToListAsync(cancellationToken);

                if (batch.Count == 0)
                    break;

                foreach (var row in batch)
                {
                    var sourceResults = new List<SourceResult>(2)
                    {
                        ResolveScenarioSource(row.ScenarioId, scenarioOwnership),
                        ResolveProjectSource(row.ProjectId, projectOwnership)
                    };

                    var resolution = ResolveFromSources(
                        sourceResults,
                        OwnershipBackfillUnresolvedReasons.ScenarioHistoryOwnershipMetadataMissing,
                        OwnershipBackfillUnresolvedReasons.ScenarioHistoryOwnershipAmbiguous);

                    accumulator.AddPrevious("ScenarioHistory", row.Id, row.ProjectId, null, resolution.OrganizationId, null);

                    if (resolution.IsResolvable)
                    {
                        accumulator.AddResolvable("ScenarioHistory");
                    }
                    else
                    {
                        accumulator.AddUnresolved(
                            recordType: "ScenarioHistory",
                            recordId: row.Id,
                            reason: resolution.Reason,
                            candidateProjectId: row.ProjectId,
                            candidateBuildingId: null,
                            candidateOrganizationId: resolution.OrganizationId,
                            notes: resolution.Notes,
                            ambiguous: resolution.IsAmbiguous);
                    }
                }

                offset += batch.Count;
            }
        }
        catch (Exception exception) when (IsMissingTableException(exception))
        {
            return;
        }
    }

    private static SourceResult ResolveProjectSource(
        int projectId,
        IReadOnlyDictionary<int, int?> projectOwnership)
    {
        if (!projectOwnership.TryGetValue(projectId, out var organizationId))
            return SourceResult.Unresolved(OwnershipBackfillUnresolvedReasons.ProjectMissing, "Referenced project was not found.");

        if (!organizationId.HasValue)
            return SourceResult.Unresolved(OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing, "Referenced project has null OrganizationId.");

        return SourceResult.Resolved(organizationId.Value);
    }

    private static SourceResult ResolveBuildingSource(
        int buildingId,
        IReadOnlyDictionary<int, int?> projectOwnership,
        IReadOnlyDictionary<int, BuildingOwnershipRow> buildingOwnership)
    {
        if (!buildingOwnership.TryGetValue(buildingId, out var building))
            return SourceResult.Unresolved(OwnershipBackfillUnresolvedReasons.BuildingMissing, "Referenced building was not found.");

        if (!projectOwnership.TryGetValue(building.ProjectId, out var organizationId))
            return SourceResult.Unresolved(OwnershipBackfillUnresolvedReasons.BuildingProjectMissing, "Referenced building project was not found.");

        if (!organizationId.HasValue)
        {
            return SourceResult.Unresolved(
                OwnershipBackfillUnresolvedReasons.BuildingProjectOrganizationMissing,
                "Referenced building project has null OrganizationId.");
        }

        return SourceResult.Resolved(organizationId.Value);
    }

    private static SourceResult ResolveScenarioSource(
        string? scenarioId,
        IReadOnlyDictionary<string, OwnershipResolution> scenarioOwnership)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
            return SourceResult.Unresolved(OwnershipBackfillUnresolvedReasons.ScenarioMissing, "ScenarioId is missing.");

        if (!scenarioOwnership.TryGetValue(scenarioId, out var resolution))
            return SourceResult.Unresolved(OwnershipBackfillUnresolvedReasons.ScenarioMissing, "Referenced scenario was not found.");

        return resolution.IsResolvable
            ? SourceResult.Resolved(resolution.OrganizationId!.Value)
            : SourceResult.Unresolved(resolution.Reason, resolution.Notes);
    }

    private static SourceResult ResolveJobSource(
        string? jobId,
        IReadOnlyDictionary<string, OwnershipResolution> jobOwnership)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return SourceResult.Unresolved(OwnershipBackfillUnresolvedReasons.JobMissing, "JobId is missing.");

        if (!jobOwnership.TryGetValue(jobId, out var resolution))
            return SourceResult.Unresolved(OwnershipBackfillUnresolvedReasons.JobMissing, "Referenced job was not found.");

        return resolution.IsResolvable
            ? SourceResult.Resolved(resolution.OrganizationId!.Value)
            : SourceResult.Unresolved(resolution.Reason, resolution.Notes);
    }

    private static OwnershipResolution ResolveFromSources(
        IReadOnlyList<SourceResult> sources,
        string metadataMissingReason,
        string ambiguousReason)
    {
        var candidateOrganizations = sources
            .Where(source => source.IsResolved)
            .Select(source => source.OrganizationId!.Value)
            .Distinct()
            .ToArray();

        if (candidateOrganizations.Length > 1)
        {
            return OwnershipResolution.Ambiguous(
                candidateOrganizations[0],
                ambiguousReason,
                "Multiple ownership sources resolved to different OrganizationId values.");
        }

        if (candidateOrganizations.Length == 1)
            return OwnershipResolution.Resolved(candidateOrganizations[0]);

        var firstUnresolved = sources.FirstOrDefault(source => !source.IsResolved);
        if (firstUnresolved is not null)
        {
            return OwnershipResolution.Unresolved(
                firstUnresolved.Reason ?? metadataMissingReason,
                firstUnresolved.Notes);
        }

        return OwnershipResolution.Unresolved(metadataMissingReason, "No ownership metadata source produced a tenant scope.");
    }

    private static string NormalizeProvider(string provider)
    {
        if (string.Equals(provider, "SQLite", StringComparison.OrdinalIgnoreCase))
            return "SQLite";

        if (string.Equals(provider, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
            return "PostgreSQL";

        return "None";
    }

    private static AppDbContext CreateAppDbContext(string provider, string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        ConfigureProvider(optionsBuilder, provider, connectionString);
        return new AppDbContext(optionsBuilder.Options);
    }

    private static EngineeringWorkflowPersistenceDbContext CreateWorkflowDbContext(string provider, string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EngineeringWorkflowPersistenceDbContext>();
        ConfigureProvider(optionsBuilder, provider, connectionString);
        return new EngineeringWorkflowPersistenceDbContext(optionsBuilder.Options);
    }

    private static void ConfigureProvider(DbContextOptionsBuilder optionsBuilder, string provider, string connectionString)
    {
        if (string.Equals(provider, "SQLite", StringComparison.Ordinal))
        {
            optionsBuilder.UseSqlite(connectionString);
            return;
        }

        if (string.Equals(provider, "PostgreSQL", StringComparison.Ordinal))
        {
            optionsBuilder.UseNpgsql(connectionString);
            return;
        }

        throw new InvalidOperationException("Unsupported database provider for database scanner.");
    }

    private static string BuildRunId(DateTimeOffset startedAtUtc)
    {
        return $"{startedAtUtc:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..23];
    }

    private static bool IsMissingTableException(Exception exception)
    {
        if (exception is SqliteException sqliteException)
        {
            return sqliteException.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase);
        }

        if (exception is PostgresException postgresException)
        {
            return string.Equals(postgresException.SqlState, "42P01", StringComparison.Ordinal);
        }

        return false;
    }

    private sealed class MetricsAccumulator
    {
        private readonly Dictionary<string, RecordMetricsAccumulator> _recordTypeMetrics = OwnershipBackfillConstants.KnownRecordTypes
            .ToDictionary(
                recordType => recordType,
                _ => new RecordMetricsAccumulator(),
                StringComparer.Ordinal);

        private readonly Dictionary<string, int> _summaryUnresolvedByReason = new(StringComparer.Ordinal);
        private readonly List<OwnershipBackfillUnresolvedRecord> _unresolvedRecords = [];
        private readonly List<OwnershipBackfillPreviousValueSnapshot> _previousValueSnapshots = [];

        public IReadOnlyList<OwnershipBackfillUnresolvedRecord> UnresolvedRecords => _unresolvedRecords;

        public IReadOnlyList<OwnershipBackfillPreviousValueSnapshot> PreviousValueSnapshots => _previousValueSnapshots;

        public void AddResolvable(string recordType)
        {
            var metrics = GetMetrics(recordType);
            metrics.TotalRecords++;
            metrics.ResolvableRecords++;
        }

        public void AddUnresolved(
            string recordType,
            string recordId,
            string reason,
            int? candidateProjectId,
            int? candidateBuildingId,
            int? candidateOrganizationId,
            string? notes,
            bool ambiguous)
        {
            var metrics = GetMetrics(recordType);
            metrics.TotalRecords++;
            metrics.UnresolvedRecords++;

            if (ambiguous)
                metrics.AmbiguousRecords++;

            IncrementReason(metrics.UnresolvedByReason, reason);
            IncrementReason(_summaryUnresolvedByReason, reason);

            _unresolvedRecords.Add(new OwnershipBackfillUnresolvedRecord
            {
                RecordType = recordType,
                RecordId = recordId,
                Reason = reason,
                CandidateProjectId = candidateProjectId,
                CandidateBuildingId = candidateBuildingId,
                CandidateOrganizationId = candidateOrganizationId,
                Notes = notes
            });
        }

        public void AddPrevious(
            string recordType,
            string recordId,
            int? previousProjectId,
            int? previousBuildingId,
            int? previousOrganizationId,
            int? previousOwnerUserId)
        {
            _previousValueSnapshots.Add(new OwnershipBackfillPreviousValueSnapshot
            {
                RecordType = recordType,
                RecordId = recordId,
                PreviousProjectId = previousProjectId,
                PreviousBuildingId = previousBuildingId,
                PreviousOrganizationId = previousOrganizationId,
                PreviousOwnerUserId = previousOwnerUserId
            });
        }

        public OwnershipBackfillDryRunSummary BuildSummary(
            string runId,
            DateTimeOffset startedAtUtc,
            DateTimeOffset completedAtUtc)
        {
            var metrics = _recordTypeMetrics
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => new OwnershipBackfillRecordTypeMetrics
                {
                    RecordType = item.Key,
                    TotalRecords = item.Value.TotalRecords,
                    ResolvableRecords = item.Value.ResolvableRecords,
                    UnresolvedRecords = item.Value.UnresolvedRecords,
                    AmbiguousRecords = item.Value.AmbiguousRecords,
                    ResolvableRate = item.Value.TotalRecords == 0
                        ? 0d
                        : (double)item.Value.ResolvableRecords / item.Value.TotalRecords,
                    UnresolvedByReason = new Dictionary<string, int>(item.Value.UnresolvedByReason, StringComparer.Ordinal)
                })
                .ToArray();

            var totalScanned = metrics.Sum(item => item.TotalRecords);
            var totalResolvable = metrics.Sum(item => item.ResolvableRecords);
            var totalUnresolved = metrics.Sum(item => item.UnresolvedRecords);

            return new OwnershipBackfillDryRunSummary
            {
                RunId = runId,
                StartedAtUtc = startedAtUtc,
                CompletedAtUtc = completedAtUtc,
                Mode = OwnershipBackfillRunMode.DryRun.ToString(),
                TotalRecordsScanned = totalScanned,
                TotalRecordsResolvable = totalResolvable,
                TotalRecordsUnresolved = totalUnresolved,
                UnresolvedByReason = new Dictionary<string, int>(_summaryUnresolvedByReason, StringComparer.Ordinal),
                RecordTypeMetrics = metrics,
                NonClaims = OwnershipBackfillConstants.NonClaims
            };
        }

        private RecordMetricsAccumulator GetMetrics(string recordType)
        {
            if (_recordTypeMetrics.TryGetValue(recordType, out var metrics))
                return metrics;

            metrics = new RecordMetricsAccumulator();
            _recordTypeMetrics[recordType] = metrics;
            return metrics;
        }

        private static void IncrementReason(Dictionary<string, int> map, string reason)
        {
            map[reason] = map.TryGetValue(reason, out var count)
                ? count + 1
                : 1;
        }

        private sealed class RecordMetricsAccumulator
        {
            public int TotalRecords { get; set; }
            public int ResolvableRecords { get; set; }
            public int UnresolvedRecords { get; set; }
            public int AmbiguousRecords { get; set; }
            public Dictionary<string, int> UnresolvedByReason { get; } = new(StringComparer.Ordinal);
        }
    }

    private sealed record ProjectRow(int Id, int? OrganizationId, int? OwnerUserId);

    private sealed record BuildingOwnershipRow(int Id, int ProjectId);

    private sealed record WorkflowStateRow(string Id, int ProjectId, int? BuildingId);

    private sealed record ScenarioRow(string Id, int ProjectId, int? BuildingId);

    private sealed record JobRow(string Id, int ProjectId, string ScenarioId);

    private sealed record JobEventRow(string Id, string JobId, string ScenarioId, int ProjectId);

    private sealed record ScenarioHistoryRow(string Id, string ScenarioId, int ProjectId);

    private sealed record SourceResult(bool IsResolved, int? OrganizationId, string? Reason, string? Notes)
    {
        public static SourceResult Resolved(int organizationId) => new(true, organizationId, null, null);

        public static SourceResult Unresolved(string reason, string? notes) => new(false, null, reason, notes);
    }

    private sealed record OwnershipResolution(bool IsResolvable, bool IsAmbiguous, int? OrganizationId, string Reason, string? Notes)
    {
        public static OwnershipResolution Resolved(int organizationId) =>
            new(true, false, organizationId, string.Empty, null);

        public static OwnershipResolution Unresolved(string reason, string? notes) =>
            new(false, false, null, reason, notes);

        public static OwnershipResolution Ambiguous(int candidateOrganizationId, string reason, string? notes) =>
            new(false, true, candidateOrganizationId, reason, notes);
    }
}

