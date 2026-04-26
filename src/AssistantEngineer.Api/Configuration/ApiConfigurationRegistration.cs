namespace AssistantEngineer.Api.Configuration;

internal static class ApiConfigurationRegistration
{
    public static ConfigurationManager AddApiConfiguration(
        this ConfigurationManager configuration)
    {
        configuration.AddJsonFile(
            "Config/building-archetypes.json",
            optional: false,
            reloadOnChange: true);

        return configuration;
    }
}