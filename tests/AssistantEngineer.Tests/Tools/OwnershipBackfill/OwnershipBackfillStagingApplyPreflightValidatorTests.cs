using AssistantEngineer.Tools.OwnershipBackfill.Staging;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillStagingApplyPreflightValidatorTests
{
    [Fact]
    public void ValidStagingPreflight_Passes()
    {
        var temp = CreateTempFiles();
        try
        {
            var validator = new OwnershipBackfillStagingApplyPreflightValidator();
            var result = validator.Validate(CreateValidOptions(temp));

            Assert.True(result.Passed);
            Assert.Empty(result.Findings);
        }
        finally
        {
            Directory.Delete(temp.Root, true);
        }
    }

    [Fact]
    public void ProductionEnvironment_Fails()
    {
        var temp = CreateTempFiles();
        try
        {
            var validator = new OwnershipBackfillStagingApplyPreflightValidator();
            var options = CreateValidOptions(temp, environmentName: "Production");
            var result = validator.Validate(options);

            Assert.False(result.Passed);
            Assert.Contains(result.Findings, f => f.Code == "STAGING_PREFLIGHT_PRODUCTION_DENIED");
        }
        finally
        {
            Directory.Delete(temp.Root, true);
        }
    }

    [Fact]
    public void UnknownEnvironment_Fails()
    {
        var temp = CreateTempFiles();
        try
        {
            var validator = new OwnershipBackfillStagingApplyPreflightValidator();
            var options = CreateValidOptions(temp, environmentName: "Dev");
            var result = validator.Validate(options);

            Assert.False(result.Passed);
            Assert.Contains(result.Findings, f => f.Code == "STAGING_PREFLIGHT_UNKNOWN_ENVIRONMENT");
        }
        finally
        {
            Directory.Delete(temp.Root, true);
        }
    }

    [Theory]
    [InlineData("hash")]
    [InlineData("")]
    [InlineData("  ")]
    public void MissingApplyInputHash_Fails(string applyInputHash)
    {
        var temp = CreateTempFiles();
        try
        {
            var validator = new OwnershipBackfillStagingApplyPreflightValidator();
            var options = CreateValidOptions(temp, applyInputHash: applyInputHash);
            var result = validator.Validate(options);

            var shouldFail = string.IsNullOrWhiteSpace(applyInputHash);
            Assert.Equal(!shouldFail, result.Passed);
        }
        finally
        {
            Directory.Delete(temp.Root, true);
        }
    }

    [Fact]
    public void MissingReadinessResult_Fails()
    {
        var temp = CreateTempFiles();
        try
        {
            var validator = new OwnershipBackfillStagingApplyPreflightValidator();
            var baseline = CreateValidOptions(temp);
            var options = new OwnershipBackfillStagingApplyPreflightOptions
            {
                EnvironmentName = baseline.EnvironmentName,
                ApplyInputHash = baseline.ApplyInputHash,
                ReadinessResultPath = null,
                PlanPath = baseline.PlanPath,
                SignoffPath = baseline.SignoffPath,
                BackupReference = baseline.BackupReference,
                RollbackReadinessReference = baseline.RollbackReadinessReference,
                OperatorId = baseline.OperatorId,
                SchemaVersion = baseline.SchemaVersion,
                EnableStagingApply = baseline.EnableStagingApply,
                ConfirmNoProductionConnection = baseline.ConfirmNoProductionConnection
            };
            var result = validator.Validate(options);
            Assert.False(result.Passed);
            Assert.Contains(result.Findings, f => f.Code.StartsWith("STAGING_PREFLIGHT_READINESS_REQUIRED", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(temp.Root, true);
        }
    }

    [Fact]
    public void MissingPlan_Fails()
    {
        var temp = CreateTempFiles();
        try
        {
            var validator = new OwnershipBackfillStagingApplyPreflightValidator();
            var baseline = CreateValidOptions(temp);
            var options = new OwnershipBackfillStagingApplyPreflightOptions
            {
                EnvironmentName = baseline.EnvironmentName,
                ApplyInputHash = baseline.ApplyInputHash,
                ReadinessResultPath = baseline.ReadinessResultPath,
                PlanPath = null,
                SignoffPath = baseline.SignoffPath,
                BackupReference = baseline.BackupReference,
                RollbackReadinessReference = baseline.RollbackReadinessReference,
                OperatorId = baseline.OperatorId,
                SchemaVersion = baseline.SchemaVersion,
                EnableStagingApply = baseline.EnableStagingApply,
                ConfirmNoProductionConnection = baseline.ConfirmNoProductionConnection
            };
            var result = validator.Validate(options);
            Assert.False(result.Passed);
            Assert.Contains(result.Findings, f => f.Code.StartsWith("STAGING_PREFLIGHT_PLAN_REQUIRED", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(temp.Root, true);
        }
    }

    [Fact]
    public void MissingSignoff_Fails()
    {
        var temp = CreateTempFiles();
        try
        {
            var validator = new OwnershipBackfillStagingApplyPreflightValidator();
            var baseline = CreateValidOptions(temp);
            var options = new OwnershipBackfillStagingApplyPreflightOptions
            {
                EnvironmentName = baseline.EnvironmentName,
                ApplyInputHash = baseline.ApplyInputHash,
                ReadinessResultPath = baseline.ReadinessResultPath,
                PlanPath = baseline.PlanPath,
                SignoffPath = null,
                BackupReference = baseline.BackupReference,
                RollbackReadinessReference = baseline.RollbackReadinessReference,
                OperatorId = baseline.OperatorId,
                SchemaVersion = baseline.SchemaVersion,
                EnableStagingApply = baseline.EnableStagingApply,
                ConfirmNoProductionConnection = baseline.ConfirmNoProductionConnection
            };
            var result = validator.Validate(options);
            Assert.False(result.Passed);
            Assert.Contains(result.Findings, f => f.Code.StartsWith("STAGING_PREFLIGHT_SIGNOFF_REQUIRED", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(temp.Root, true);
        }
    }

    [Fact]
    public void MissingBackupReference_Fails()
    {
        var temp = CreateTempFiles();
        try
        {
            var validator = new OwnershipBackfillStagingApplyPreflightValidator();
            var options = CreateValidOptions(temp, backupReference: null);
            var result = validator.Validate(options);
            Assert.False(result.Passed);
            Assert.Contains(result.Findings, f => f.Code == "STAGING_PREFLIGHT_BACKUP_REFERENCE_REQUIRED");
        }
        finally
        {
            Directory.Delete(temp.Root, true);
        }
    }

    [Fact]
    public void MissingRollbackReadinessReference_Fails()
    {
        var temp = CreateTempFiles();
        try
        {
            var validator = new OwnershipBackfillStagingApplyPreflightValidator();
            var options = CreateValidOptions(temp, rollbackReadinessReference: null);
            var result = validator.Validate(options);
            Assert.False(result.Passed);
            Assert.Contains(result.Findings, f => f.Code == "STAGING_PREFLIGHT_ROLLBACK_REFERENCE_REQUIRED");
        }
        finally
        {
            Directory.Delete(temp.Root, true);
        }
    }

    [Fact]
    public void MissingOperator_Fails()
    {
        var temp = CreateTempFiles();
        try
        {
            var validator = new OwnershipBackfillStagingApplyPreflightValidator();
            var options = CreateValidOptions(temp, operatorId: null);
            var result = validator.Validate(options);
            Assert.False(result.Passed);
            Assert.Contains(result.Findings, f => f.Code == "STAGING_PREFLIGHT_OPERATOR_REQUIRED");
        }
        finally
        {
            Directory.Delete(temp.Root, true);
        }
    }

    [Fact]
    public void MissingSchemaVersion_Fails()
    {
        var temp = CreateTempFiles();
        try
        {
            var validator = new OwnershipBackfillStagingApplyPreflightValidator();
            var options = CreateValidOptions(temp, schemaVersion: null);
            var result = validator.Validate(options);
            Assert.False(result.Passed);
            Assert.Contains(result.Findings, f => f.Code == "STAGING_PREFLIGHT_SCHEMA_VERSION_REQUIRED");
        }
        finally
        {
            Directory.Delete(temp.Root, true);
        }
    }

    [Fact]
    public void MissingConfirmNoProductionConnection_Fails()
    {
        var temp = CreateTempFiles();
        try
        {
            var validator = new OwnershipBackfillStagingApplyPreflightValidator();
            var options = CreateValidOptions(temp, confirmNoProductionConnection: false);
            var result = validator.Validate(options);
            Assert.False(result.Passed);
            Assert.Contains(result.Findings, f => f.Code == "STAGING_PREFLIGHT_CONFIRMATION_REQUIRED");
        }
        finally
        {
            Directory.Delete(temp.Root, true);
        }
    }

    [Fact]
    public void EnableStagingApplyFalse_ProducesBlockingFinding()
    {
        var temp = CreateTempFiles();
        try
        {
            var validator = new OwnershipBackfillStagingApplyPreflightValidator();
            var options = CreateValidOptions(temp, enableStagingApply: false);
            var result = validator.Validate(options);
            Assert.False(result.Passed);
            Assert.Contains(result.Findings, f => f.Code == "STAGING_PREFLIGHT_ENABLE_FLAG_REQUIRED");
        }
        finally
        {
            Directory.Delete(temp.Root, true);
        }
    }

    private static OwnershipBackfillStagingApplyPreflightOptions CreateValidOptions(
        (string Root, string Readiness, string Plan, string Signoff) temp,
        string? environmentName = "Staging",
        string? applyInputHash = "hash-001",
        string? readinessResultPath = null,
        string? planPath = null,
        string? signoffPath = null,
        string? backupReference = "backup-001",
        string? rollbackReadinessReference = "rollback-001",
        string? operatorId = "operator-001",
        string? schemaVersion = "schema-v1",
        bool enableStagingApply = true,
        bool confirmNoProductionConnection = true)
    {
        return new OwnershipBackfillStagingApplyPreflightOptions
        {
            EnvironmentName = environmentName,
            ApplyInputHash = applyInputHash,
            ReadinessResultPath = readinessResultPath ?? temp.Readiness,
            PlanPath = planPath ?? temp.Plan,
            SignoffPath = signoffPath ?? temp.Signoff,
            BackupReference = backupReference,
            RollbackReadinessReference = rollbackReadinessReference,
            OperatorId = operatorId,
            SchemaVersion = schemaVersion,
            EnableStagingApply = enableStagingApply,
            ConfirmNoProductionConnection = confirmNoProductionConnection
        };
    }

    private static (string Root, string Readiness, string Plan, string Signoff) CreateTempFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ae-staging-preflight-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        var readiness = Path.Combine(root, "readiness.json");
        var plan = Path.Combine(root, "plan.json");
        var signoff = Path.Combine(root, "signoff.json");

        File.WriteAllText(readiness, "{}");
        File.WriteAllText(plan, "{}");
        File.WriteAllText(signoff, "{}");

        return (root, readiness, plan, signoff);
    }
}
