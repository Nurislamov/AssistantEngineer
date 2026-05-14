using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Pipeline;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;

internal sealed class PipelineSystemEnergyHandoffUsefulDemandProvider : ISystemEnergyHandoffUsefulDemandProvider
{
    private readonly IServiceProvider _serviceProvider;

    public PipelineSystemEnergyHandoffUsefulDemandProvider(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<Result<BuildingEnergyBalanceResult>> CalculateUsefulDemandAsync(
        int buildingId,
        CoolingLoadCalculationMethod coolingMethod,
        HeatingLoadCalculationMethod heatingMethod,
        CancellationToken cancellationToken)
    {
        var pipeline = _serviceProvider.GetRequiredService<IEnergyCalculationPipeline>();
        return pipeline.CalculateBuildingEnergyBalanceAsync(
            buildingId,
            coolingMethod,
            heatingMethod,
            cancellationToken);
    }
}
