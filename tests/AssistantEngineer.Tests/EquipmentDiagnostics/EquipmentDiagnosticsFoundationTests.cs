using System.Reflection;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public class EquipmentDiagnosticsFoundationTests
{
    [Fact]
    public async Task ModuleServiceCanSearchByManufacturerAndErrorCode()
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var results = await service.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: "Gree",
                ErrorCode: "H5"),
            CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal("Gree", result.Manufacturer);
        Assert.Equal("GMV", result.SeriesName);
        Assert.Equal("H5", result.Code);
    }

    [Fact]
    public async Task SearchIsCaseInsensitiveAndWhitespaceInsensitive()
    {
        var facade = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsFacade>();

        var results = await facade.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: " g r e e ",
                ErrorCode: " h 5 ",
                Series: " g m v "),
            CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal("H5", result.Code);
        Assert.Equal("GMV", result.SeriesName);
    }

    [Fact]
    public async Task UnknownCodeReturnsEmptyResult()
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var results = await service.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: "Gree",
                ErrorCode: "Unknown"),
            CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task DiagnosticCaseIncludesDiagnosticFoundationData()
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var diagnosticCase = await service.GetDiagnosticCaseAsync(
            manufacturer: "Gree",
            errorCode: "H5",
            series: "GMV",
            modelCode: null,
            CancellationToken.None);

        Assert.NotNull(diagnosticCase);
        Assert.NotEmpty(diagnosticCase.LikelyCauses);
        Assert.NotEmpty(diagnosticCase.DiagnosticSteps);
        Assert.NotEmpty(diagnosticCase.RequiredMeasurements);
        Assert.NotEqual(DiagnosticConfidence.Unknown, diagnosticCase.Confidence);
    }

    [Fact]
    public async Task SeededGreeH5DoesNotClaimManualVerifiedConfidence()
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var diagnosticCase = await service.GetDiagnosticCaseAsync(
            manufacturer: "Gree",
            errorCode: "H5",
            series: "GMV",
            modelCode: null,
            CancellationToken.None);

        Assert.NotNull(diagnosticCase);
        Assert.NotEqual(DiagnosticConfidence.ManualVerified, diagnosticCase.Confidence);
        Assert.NotEqual(DiagnosticConfidence.ManualVerified, diagnosticCase.ErrorCode.Confidence);
    }

    [Fact]
    public void EquipmentDiagnosticsModuleDoesNotReferenceForbiddenBackendProjects()
    {
        var assembly = typeof(IEquipmentDiagnosticsFacade).Assembly;

        var referencedAssemblies = assembly
            .GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.Ordinal);

        var forbiddenReferences = new[]
        {
            "AssistantEngineer.Modules.Calculations",
            "AssistantEngineer.Modules.Buildings",
            "AssistantEngineer.Infrastructure",
            "AssistantEngineer.Api"
        };

        var violations = forbiddenReferences
            .Where(referencedAssemblies.Contains)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"{assembly.GetName().Name} references forbidden projects: {string.Join(", ", violations)}.");
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        return services.BuildServiceProvider();
    }
}
