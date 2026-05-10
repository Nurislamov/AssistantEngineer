namespace AssistantEngineer.Api.Services.Calculations.Workflow;

public static class EngineeringWorkflowServiceRegistration
{
    public static IServiceCollection AddEngineeringWorkflowServices(this IServiceCollection services)
    {
        services.AddScoped<IEngineeringWorkflowInputSnapshotBuilder, EngineeringWorkflowInputSnapshotBuilder>();
        services.AddScoped<IEngineeringWorkflowStateBuilder, EngineeringWorkflowStateBuilder>();
        services.AddScoped<IEngineeringWorkflowDiagnosticsService, EngineeringWorkflowDiagnosticsService>();
        services.AddScoped<IEngineeringWorkflowTracePreviewService, EngineeringWorkflowTracePreviewService>();
        services.AddScoped<IEngineeringWorkflowReportPreviewService, EngineeringWorkflowReportPreviewService>();
        services.AddScoped<IEngineeringWorkflowSubmissionService, EngineeringWorkflowSubmissionService>();

        return services;
    }
}
