using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.InternalGains;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.InternalGains;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016RoomInternalGainProfileBuilder : ISo52016RoomInternalGainProfileBuilder
{
    private readonly InternalGainEngine _internalGainEngine;

    public Iso52016RoomInternalGainProfileBuilder()
        : this(new InternalGainEngine())
    {
    }

    public Iso52016RoomInternalGainProfileBuilder(
        InternalGainEngine internalGainEngine)
    {
        _internalGainEngine = internalGainEngine ??
                              throw new ArgumentNullException(nameof(internalGainEngine));
    }

    public Result<Iso52016RoomInternalGainProfile> Build(
        Iso52016RoomInternalGainProfileRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016RoomInternalGainProfile>.Failure(validation);

        var hours = Enumerable
            .Range(0, request.HourCount)
            .Select(hourOfYear => BuildHour(
                request,
                hourOfYear))
            .ToArray();

        return Result<Iso52016RoomInternalGainProfile>.Success(
            new Iso52016RoomInternalGainProfile(
                RoomCode: request.RoomCode.Trim(),
                PeopleCount: request.PeopleCount,
                SensibleHeatGainPerPersonW: request.SensibleHeatGainPerPersonW,
                EquipmentLoadW: request.EquipmentLoadW,
                LightingLoadW: request.LightingLoadW,
                Hours: hours));
    }

    private Iso52016HourlyRoomInternalGainRecord BuildHour(
        Iso52016RoomInternalGainProfileRequest request,
        int hourOfYear)
    {
        var occupancyFactor = request.OccupancyFactors[hourOfYear];
        var equipmentFactor = request.EquipmentFactors[hourOfYear];
        var lightingFactor = request.LightingFactors[hourOfYear];

        var result = _internalGainEngine.Calculate(
            new InternalGainInput(
                RoomId: 0,
                OccupancyPeople: request.PeopleCount,
                SensibleGainPerPersonW: request.SensibleHeatGainPerPersonW,
                EquipmentLoadW: request.EquipmentLoadW,
                LightingLoadW: request.LightingLoadW,
                OccupancyScheduleFactor: occupancyFactor,
                EquipmentScheduleFactor: equipmentFactor,
                LightingScheduleFactor: lightingFactor,
                DiagnosticsContext: request.RoomCode));

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Internal gain calculation failed for hour {hourOfYear}: {result.Error}");
        }

        if (result.Value.HasErrors)
        {
            var firstError = result.Value.Diagnostics.First(diagnostic =>
                diagnostic.Severity == CalculationDiagnosticSeverity.Error);

            throw new InvalidOperationException(
                $"Internal gain calculation has diagnostics error at hour {hourOfYear}: {firstError.Code} - {firstError.Message}");
        }

        return new Iso52016HourlyRoomInternalGainRecord(
            HourOfYear: hourOfYear,
            OccupancyFactor: occupancyFactor,
            EquipmentFactor: equipmentFactor,
            LightingFactor: lightingFactor,
            PeopleGainW: result.Value.OccupancySensibleGainW,
            EquipmentGainW: result.Value.EquipmentGainW,
            LightingGainW: result.Value.LightingGainW,
            TotalInternalGainW: result.Value.TotalSensibleGainW);
    }

    private static Result Validate(
        Iso52016RoomInternalGainProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoomCode))
            return Result.Validation("Room code is required.");

        if (request.HourCount <= 0)
            return Result.Validation("Hour count must be greater than zero.");

        if (request.PeopleCount < 0)
            return Result.Validation("People count must not be negative.");

        if (request.SensibleHeatGainPerPersonW < 0)
            return Result.Validation("Sensible heat gain per person must not be negative.");

        if (request.EquipmentLoadW < 0)
            return Result.Validation("Equipment load must not be negative.");

        if (request.LightingLoadW < 0)
            return Result.Validation("Lighting load must not be negative.");

        var factorValidation = ValidateFactors(
            request.OccupancyFactors,
            request.HourCount,
            "Occupancy factors");

        if (factorValidation.IsFailure)
            return factorValidation;

        factorValidation = ValidateFactors(
            request.EquipmentFactors,
            request.HourCount,
            "Equipment factors");

        if (factorValidation.IsFailure)
            return factorValidation;

        factorValidation = ValidateFactors(
            request.LightingFactors,
            request.HourCount,
            "Lighting factors");

        if (factorValidation.IsFailure)
            return factorValidation;

        return Result.Success();
    }

    private static Result ValidateFactors(
        IReadOnlyList<double>? factors,
        int expectedCount,
        string name)
    {
        if (factors is null)
            return Result.Validation($"{name} are required.");

        if (factors.Count != expectedCount)
        {
            return Result.Validation(
                $"{name} must contain exactly {expectedCount} values.");
        }

        var invalidIndex = factors
            .Select((factor, index) => new
            {
                Factor = factor,
                Index = index
            })
            .FirstOrDefault(item =>
                item.Factor is < 0.0 or > 1.0);

        if (invalidIndex is not null)
        {
            return Result.Validation(
                $"{name} value at hour {invalidIndex.Index} must be between 0 and 1.");
        }

        return Result.Success();
    }
}