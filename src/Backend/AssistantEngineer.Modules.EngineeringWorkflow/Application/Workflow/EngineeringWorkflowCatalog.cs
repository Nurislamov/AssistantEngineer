namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;

public static class EngineeringWorkflowCatalog
{
    public const int DefaultWeatherYear = 2020;

    public static readonly string[] WorkflowSteps =
    [
        "Project",
        "Building",
        "Zones",
        "Envelope",
        "WeatherSolar",
        "Ventilation",
        "Ground",
        "DomesticHotWater",
        "SystemEnergy",
        "Validation",
        "CalculationTrace",
        "Reports",
        "Review"
    ];

    public static readonly string[] AvailableModules =
    [
        "Weather",
        "Solar",
        "ThermalTopology",
        "Iso52016",
        "MultiZone",
        "Ventilation",
        "Ground",
        "DomesticHotWater",
        "SystemEnergy",
        "Validation",
        "Reporting"
    ];
}
