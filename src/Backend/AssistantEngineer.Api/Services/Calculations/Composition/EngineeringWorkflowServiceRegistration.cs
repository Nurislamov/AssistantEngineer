using AssistantEngineer.Api.Services.Calculations.Composition;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;
using WorkflowPersistence = AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence.IEngineeringWorkflowPersistenceService;

namespace AssistantEngineer.Api.Services.Calculations.Composition;

public static class EngineeringWorkflowServiceRegistration
{
    public static IServiceCollection AddEngineeringWorkflowServices(this IServiceCollection services)
    {
        services.AddScoped<IEngineeringWorkflowScenarioRunner, EngineeringWorkflowScenarioRunnerAdapter>();
        services.AddScoped<IEngineeringWorkflowJobService, EngineeringWorkflowJobServiceAdapter>();
        services.AddScoped<WorkflowPersistence, EngineeringWorkflowPersistenceServiceAdapter>();
        services.AddScoped<IEngineeringWorkflowInputSnapshotBuilder, EngineeringWorkflowInputSnapshotBuilder>();
        services.AddScoped<IEngineeringWorkflowStateBuilder, EngineeringWorkflowStateBuilder>();
        services.AddScoped<IEngineeringWorkflowDiagnosticsService, EngineeringWorkflowDiagnosticsService>();
        services.AddScoped<IEngineeringWorkflowTracePreviewService, EngineeringWorkflowTracePreviewService>();
        services.AddScoped<IEngineeringWorkflowReportPreviewService, EngineeringWorkflowReportPreviewService>();
        services.AddScoped<IEngineeringWorkflowSubmissionService, EngineeringWorkflowSubmissionService>();

        return services;
    }
}
