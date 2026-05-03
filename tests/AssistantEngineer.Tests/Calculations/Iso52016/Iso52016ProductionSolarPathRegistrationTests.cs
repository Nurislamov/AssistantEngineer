using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Solar;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;
using AssistantEngineer.Modules.Calculations.Application.Services.WeatherSolar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016ProductionSolarPathRegistrationTests
{
    [Fact]
    public void AddCalculationsModule_UsesPerezAnisotropicSurfaceIrradianceByDefault()
    {
        var services = CreateServices();

        var descriptor = services.LastOrDefault(service =>
            service.ServiceType == typeof(ISurfaceIrradianceCalculator));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(PerezAnisotropicSurfaceIrradianceCalculator), descriptor.ImplementationType);
    }

    [Fact]
    public void AddCalculationsModule_RegistersWeatherSolarContextBuilderDependencies()
    {
        var services = CreateServices();

        var profileBuilder = services.LastOrDefault(service =>
            service.ServiceType == typeof(IAnnualWeatherSolarProfileBuilder));
        var contextBuilder = services.LastOrDefault(service =>
            service.ServiceType == typeof(IIso52016WeatherSolarContextBuilder));

        Assert.NotNull(profileBuilder);
        Assert.Equal(typeof(AnnualWeatherSolarProfileBuilder), profileBuilder.ImplementationType);

        Assert.NotNull(contextBuilder);
        Assert.Equal(ServiceLifetime.Scoped, contextBuilder.Lifetime);
        Assert.Equal(typeof(Iso52016WeatherSolarContextBuilder), contextBuilder.ImplementationType);
    }

    [Fact]
    public void ProductionRegistrationDocumentsAnnualLoopWeatherSolarInjection()
    {
        var repoRoot = FindRepositoryRoot();

        var steadyStateCalculator = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Iso52016",
            "Iso52016HourlySteadyStateCalculator.cs"));

        var weatherProvider = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Iso52016",
            "Iso52016HourlyWeatherProvider.cs"));

        Assert.Contains("IIso52016WeatherSolarContextBuilder? weatherSolarContextBuilder", steadyStateCalculator, StringComparison.Ordinal);
        Assert.Contains("weatherSolarContextBuilder", steadyStateCalculator, StringComparison.Ordinal);
        Assert.Contains("WeatherSolarContext?.GetHour(weather.HourOfYear)", steadyStateCalculator, StringComparison.Ordinal);

        Assert.Contains("IIso52016WeatherSolarContextBuilder? weatherSolarContextBuilder", weatherProvider, StringComparison.Ordinal);
        Assert.Contains("BuildWeatherSolarContext", weatherProvider, StringComparison.Ordinal);
        Assert.Contains("new Iso52016WeatherSolarContextRequest", weatherProvider, StringComparison.Ordinal);
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddCalculationsModule(configuration);

        return services;
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AssistantEngineer.sln")))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root with AssistantEngineer.sln was not found.");
    }
}
