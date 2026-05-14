using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Pipeline;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;

public sealed class SystemEnergyHandoffUseCase : ISystemEnergyHandoffUseCase
{
    private readonly ISystemEnergyHandoffUsefulDemandProvider _usefulDemandProvider;
    private readonly SystemEnergyEngine? _systemEnergyEngine;
    private readonly SystemEnergyUsefulEnergyHandoffBuilder? _systemEnergyHandoffBuilder;
    private readonly SystemEnergyOptions _systemEnergyOptions;

    public SystemEnergyHandoffUseCase(
        ISystemEnergyHandoffUsefulDemandProvider usefulDemandProvider,
        SystemEnergyEngine? systemEnergyEngine = null,
        SystemEnergyUsefulEnergyHandoffBuilder? systemEnergyHandoffBuilder = null,
        IOptions<SystemEnergyOptions>? systemEnergyOptions = null)
    {
        _usefulDemandProvider = usefulDemandProvider;
        _systemEnergyEngine = systemEnergyEngine;
        _systemEnergyHandoffBuilder = systemEnergyHandoffBuilder;
        _systemEnergyOptions = systemEnergyOptions?.Value ?? new SystemEnergyOptions();
    }

    public async Task<Result<SystemEnergyHandoffResult>> CalculateBuildingSystemEnergyFromUsefulDemandAsync(
        int buildingId,
        CoolingLoadCalculationMethod coolingMethod = CoolingLoadCalculationMethod.Iso52016,
        HeatingLoadCalculationMethod heatingMethod = HeatingLoadCalculationMethod.En12831,
        DomesticHotWaterEn15316Handoff? dhwHandoff = null,
        CancellationToken cancellationToken = default)
    {
        if (_systemEnergyEngine is null || _systemEnergyHandoffBuilder is null)
        {
            return Result<SystemEnergyHandoffResult>.Validation(
                "System-energy handoff services are not configured.");
        }

        if (!_systemEnergyOptions.UseEn15316InspiredChain ||
            !_systemEnergyOptions.UseEn15316CircuitLevelCalculator)
        {
            return Result<SystemEnergyHandoffResult>.Validation(
                "System-energy circuit-level handoff requires explicit opt-in in Calculations:SystemEnergy options.");
        }

        var usefulResult = await _usefulDemandProvider.CalculateUsefulDemandAsync(
            buildingId,
            coolingMethod,
            heatingMethod,
            cancellationToken);
        if (usefulResult.IsFailure)
            return Result<SystemEnergyHandoffResult>.Failure(usefulResult);

        var handoff = _systemEnergyHandoffBuilder.Build(usefulResult.Value, _systemEnergyOptions, dhwHandoff);
        var systemResult = _systemEnergyEngine.Calculate(handoff.SystemEnergyInput);
        if (systemResult.IsFailure)
            return Result<SystemEnergyHandoffResult>.Failure(systemResult);

        return Result<SystemEnergyHandoffResult>.Success(
            handoff with { SystemEnergyResult = systemResult.Value });
    }
}
