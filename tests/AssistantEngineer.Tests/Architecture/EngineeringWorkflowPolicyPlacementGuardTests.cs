namespace AssistantEngineer.Tests.Architecture;

public sealed class EngineeringWorkflowPolicyPlacementGuardTests
{
    [Fact]
    public void ApiJobPolicyAndCodecClassesAreExtractedFromApiLayer()
    {
        var apiJobsPath = Path.Combine(
            TestPaths.ApiProjectPath,
            "Services",
            "Calculations",
            "Jobs");

        var forbidden = new[]
        {
            "EngineeringCalculationJobStatusTransitionPolicy.cs",
            "EngineeringCalculationJobPayloadCodec.cs",
            "EngineeringCalculationJobEventRecorder.cs"
        };

        foreach (var fileName in forbidden)
        {
            var path = Path.Combine(apiJobsPath, fileName);
            Assert.False(File.Exists(path), $"Job policy/codec class must live in EngineeringWorkflow module, not API: {path}");
        }
    }

    [Fact]
    public void ApiIdempotencyFolderContainsOnlyPersistenceAdapterAndRegistration()
    {
        var apiIdempotencyPath = Path.Combine(
            TestPaths.ApiProjectPath,
            "Services",
            "Calculations",
            "Idempotency");

        var expected = new HashSet<string>(StringComparer.Ordinal)
        {
            "EfEngineeringIdempotencyService.cs",
            "EngineeringIdempotencyServiceRegistration.cs"
        };

        var actual = Directory.GetFiles(apiIdempotencyPath, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(path => Path.GetFileName(path)!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EngineeringWorkflowModuleHostsJobPoliciesAndIdempotencyPolicies()
    {
        var modulePath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EngineeringWorkflow",
            "Application");

        var requiredPaths = new[]
        {
            Path.Combine(modulePath, "Jobs", "EngineeringCalculationJobStatusTransitionPolicy.cs"),
            Path.Combine(modulePath, "Jobs", "EngineeringCalculationJobPayloadCodec.cs"),
            Path.Combine(modulePath, "Jobs", "EngineeringCalculationJobEventRecorder.cs"),
            Path.Combine(modulePath, "Persistence", "IEngineeringCalculationJobEventRepository.cs"),
            Path.Combine(modulePath, "Idempotency", "IEngineeringIdempotencyService.cs"),
            Path.Combine(modulePath, "Idempotency", "EngineeringIdempotencyModels.cs"),
            Path.Combine(modulePath, "Idempotency", "EngineeringIdempotencyRequestFingerprint.cs"),
            Path.Combine(modulePath, "Idempotency", "EngineeringIdempotencyOptions.cs"),
            Path.Combine(modulePath, "Idempotency", "InMemoryEngineeringIdempotencyService.cs")
        };

        foreach (var requiredPath in requiredPaths)
        {
            Assert.True(File.Exists(requiredPath), $"Missing extracted policy/abstraction: {requiredPath}");
        }
    }
}
