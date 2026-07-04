using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class ProgramCompositionRootTests
{
    [Fact]
    public void ProgramRemainsThinCompositionRoot()
    {
        var programText = ReadProgramFile();

        Assert.Contains(
            "return await AssistantEngineerApiHost.RunAsync(args);",
            programText,
            StringComparison.Ordinal);

        Assert.Contains(
            "public partial class Program",
            programText,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ApiHostOwnsEstablishedCompositionSequence()
    {
        var hostText = ReadApiHostFile();

        var expectedFragments = new[]
        {
            "PostgresMigrationCommand.IsMigrationCommand(args)",
            "return await PostgresMigrationCommand.RunAsync(args);",
            "var builder = WebApplication.CreateBuilder(args);",
            "builder.Configuration.AddApiConfiguration();",
            "builder.ConfigureRequestLimits();",
            "builder.ConfigureApiHardening();",
            "builder.ConfigureDataProtection();",
            "builder.Services.AddApiPresentation();",
            "builder.Services.AddApiAuthentication(builder.Configuration, builder.Environment);",
            "builder.Services.AddApiVersioningSupport();",
            "builder.Services.AddApiDocumentation();",
            "builder.Services.AddAssistantEngineerModules(",
            "var app = builder.Build();",
            "app.UseApiPipeline();",
            "app.Run();",
            "return 0;"
        };

        Assert.All(
            expectedFragments,
            fragment => Assert.Contains(fragment, hostText, StringComparison.Ordinal));

        Assert.True(
            hostText.IndexOf("PostgresMigrationCommand.IsMigrationCommand(args)", StringComparison.Ordinal) <
            hostText.IndexOf("WebApplication.CreateBuilder(args)", StringComparison.Ordinal));
    }

    [Fact]
    public void ProgramDoesNotContainLowLevelServiceRegistration()
    {
        var programText = ReadProgramFile();

        var forbiddenFragments = new[]
        {
            "AddControllers(",
            "AddOpenApi(",
            "AddApiVersioning(",
            "Configure<ApiBehaviorOptions>",
            "ConfigureOptions<ApiBehaviorOptionsSetup>",
            "ConfigureOptions<ApiMvcOptionsSetup>",
            "AddRequestTimeouts(",
            "AddRateLimiter(",
            "AddCors(",
            "AddDataProtection(",
            "AddHealthChecks(",
            "ConfigureKestrel(",
            "AddBuildingsModule(",
            "AddCalculationsModule(",
            "AddEquipmentModule(",
            "AddReportingModule(",
            "AddBenchmarksModule(",
            "AddInfrastructure("
        };

        AssertDoesNotContainAny(
            programText,
            forbiddenFragments,
            "Program.cs must not contain low-level service registration.");
    }

    [Fact]
    public void ProgramDoesNotContainLowLevelHttpPipeline()
    {
        var programText = ReadProgramFile();

        var forbiddenFragments = new[]
        {
            "MapOpenApi(",
            "UseHttpsRedirection(",
            "UseRouting(",
            "UseCors(",
            "UseRequestTimeouts(",
            "UseAuthentication(",
            "UseAuthorization(",
            "UseRateLimiter(",
            "MapHealthChecks(",
            "MapControllers("
        };

        AssertDoesNotContainAny(
            programText,
            forbiddenFragments,
            "Program.cs must not contain low-level HTTP pipeline calls.");
    }

    [Fact]
    public void ProgramDoesNotContainInlineConfigurationDetails()
    {
        var programText = ReadProgramFile();

        var forbiddenFragments = new[]
        {
            "Config/building-archetypes.json",
            "RequestLimits:MaxRequestBodyBytes",
            "RequestLimits:DefaultTimeoutSeconds",
            "RequestLimits:LongRunningTimeoutSeconds",
            "DefaultApiVersion",
            "AssumeDefaultVersionWhenUnspecified",
            "ReportApiVersions",
            "UrlSegmentApiVersionReader",
            "ValidationFilter",
            "GlobalExceptionFilter",
            "RequestPolicies.LongRunning"
        };

        AssertDoesNotContainAny(
            programText,
            forbiddenFragments,
            "Program.cs must not contain inline configuration details.");
    }

    [Fact]
    public void ProgramContainsOnlyHighLevelStartupCalls()
    {
        var programText = ReadProgramFile();

        var meaningfulLines = programText
            .Split(Environment.NewLine)
            .Select(line => line.Trim())
            .Where(line =>
                !string.IsNullOrWhiteSpace(line) &&
                !line.StartsWith("using ", StringComparison.Ordinal) &&
                !line.StartsWith("//", StringComparison.Ordinal))
            .ToArray();

        var allowedFragments = new[]
        {
            "return await AssistantEngineerApiHost.RunAsync(args);",
            "public partial class Program;"
        };

        var violations = meaningfulLines
            .Where(line =>
                !allowedFragments.Any(fragment =>
                    line.Contains(fragment, StringComparison.Ordinal)))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Program.cs contains unexpected startup details: {string.Join("; ", violations)}.");
    }

    private static string ReadProgramFile()
    {
        var apiProjectPath = global::AssistantEngineer.Tests.TestPaths.ApiProjectPath;

        var programPath = Path.Combine(
            apiProjectPath,
            "Program.cs");

        return File.ReadAllText(programPath);
    }

    private static string ReadApiHostFile()
    {
        var apiProjectPath = global::AssistantEngineer.Tests.TestPaths.ApiProjectPath;

        var hostPath = Path.Combine(
            apiProjectPath,
            "AssistantEngineerApiHost.cs");

        return File.ReadAllText(hostPath);
    }

    private static void AssertDoesNotContainAny(
        string text,
        IReadOnlyCollection<string> forbiddenFragments,
        string message)
    {
        var violations = forbiddenFragments
            .Where(fragment =>
                text.Contains(fragment, StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"{message} Forbidden fragments: {string.Join(", ", violations)}.");
    }
}
