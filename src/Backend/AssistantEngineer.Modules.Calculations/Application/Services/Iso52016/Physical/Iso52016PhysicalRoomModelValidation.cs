using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

internal static class Iso52016PhysicalRoomModelValidation
{
    private const double FractionTolerance = 1e-9;

    internal static Result Validate(
        Iso52016PhysicalRoomModelRequest request)
    {
        if (request is null)
            return Result.Validation("ISO 52016 physical room model request is required.");

        if (request.HourlyInputProfile is null)
            return Result.Validation("ISO 52016 physical room model requires an hourly input profile.");

        if (request.HourlyInputProfile.Hours is null || request.HourlyInputProfile.Hours.Count == 0)
            return Result.Validation("ISO 52016 physical room model hourly input profile must provide at least one hour.");

        var heatBalanceOptions = request.HeatBalanceOptions ?? new Iso52016RoomHeatBalanceOptions();
        var modelOptions = request.ModelOptions ?? new Iso52016PhysicalNodeModelOptions();
        var surfaces = (request.Surfaces ?? Array.Empty<Iso52016PhysicalSurface>()).ToArray();
        var surfaceBoundaryConditions = (request.SurfaceBoundaryConditions ?? Array.Empty<Iso52016PhysicalSurfaceHourlyBoundaryCondition>()).ToArray();
        var operationConditions = (request.OperationConditions ?? Array.Empty<Iso52016PhysicalHourlyOperationCondition>()).ToArray();

        var idsValidation = ValidateIds(modelOptions);

        if (idsValidation.IsFailure)
            return idsValidation;

        var fractionsValidation = ValidateFractions(modelOptions);

        if (fractionsValidation.IsFailure)
            return fractionsValidation;

        var conductanceValidation = ValidateDerivedConductances(
            request.HourlyInputProfile,
            modelOptions);

        if (conductanceValidation.IsFailure)
            return conductanceValidation;

        var surfacesValidation = ValidateSurfaces(surfaces, modelOptions);

        if (surfacesValidation.IsFailure)
            return surfacesValidation;

        var boundaryValidation = ValidateSurfaceBoundaryConditions(
            surfaceBoundaryConditions,
            surfaces,
            request.HourlyInputProfile);

        if (boundaryValidation.IsFailure)
            return boundaryValidation;

        var operationValidation = ValidateOperationConditions(
            operationConditions,
            request.HourlyInputProfile);

        if (operationValidation.IsFailure)
            return operationValidation;

        return Result.Success();
    }

    private static Result ValidateIds(
        Iso52016PhysicalNodeModelOptions modelOptions)
    {
        var requiredIds = new[]
        {
            modelOptions.AirNodeId,
            modelOptions.InternalSurfaceNodeId,
            modelOptions.ThermalMassNodeId,
            modelOptions.OutdoorBoundaryId,
            modelOptions.GroundBoundaryId,
            modelOptions.AdjacentBoundaryId,
            modelOptions.VentilationBoundaryId,
            modelOptions.AdjacentConditionedBoundaryId,
            modelOptions.AdjacentUnconditionedBoundaryId
        };

        if (requiredIds.Any(string.IsNullOrWhiteSpace))
            return Result.Validation("ISO 52016 physical room model node and boundary ids are required.");

        var nodeIds = new[]
        {
            modelOptions.AirNodeId.Trim(),
            modelOptions.InternalSurfaceNodeId.Trim(),
            modelOptions.ThermalMassNodeId.Trim()
        };

        if (nodeIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != nodeIds.Length)
            return Result.Validation("ISO 52016 physical room model node ids must be unique.");

        return Result.Success();
    }

    private static Result ValidateFractions(
        Iso52016PhysicalNodeModelOptions modelOptions)
    {
        var heatCapacityFractionSum =
            modelOptions.AirHeatCapacityFraction +
            modelOptions.InternalSurfaceHeatCapacityFraction +
            modelOptions.ThermalMassHeatCapacityFraction;

        if (Math.Abs(heatCapacityFractionSum - 1.0) > FractionTolerance)
            return Result.Validation("ISO 52016 physical room model heat capacity fractions must sum to 1.0.");

        var boundaryFractionSum =
            modelOptions.OutdoorTransmissionConductanceFraction +
            modelOptions.GroundTransmissionConductanceFraction +
            modelOptions.AdjacentTransmissionConductanceFraction;

        if (Math.Abs(boundaryFractionSum - 1.0) > FractionTolerance)
            return Result.Validation("ISO 52016 physical room model boundary conductance fractions must sum to 1.0.");

        if (modelOptions.SolarGainsToInternalSurfaceFraction < 0 || modelOptions.SolarGainsToInternalSurfaceFraction > 1)
            return Result.Validation("ISO 52016 physical room model solar gains to internal surface fraction must be between 0 and 1.");

        if (modelOptions.InternalRadiativeGainsToInternalSurfaceFraction < 0 || modelOptions.InternalRadiativeGainsToInternalSurfaceFraction > 1)
            return Result.Validation("ISO 52016 physical room model internal radiative gains to internal surface fraction must be between 0 and 1.");

        if (modelOptions.InternalGainsConvectiveFraction < 0 || modelOptions.InternalGainsConvectiveFraction > 1)
            return Result.Validation("ISO 52016 physical room model internal gains convective fraction must be between 0 and 1.");

        return Result.Success();
    }

    private static Result ValidateDerivedConductances(
        Iso52016RoomHourlyInputProfile hourlyInputProfile,
        Iso52016PhysicalNodeModelOptions modelOptions)
    {
        var airToSurfaceConductanceWPerK =
            modelOptions.AirToInternalSurfaceConductanceWPerK ??
            hourlyInputProfile.TotalHeatTransferCoefficientWPerK * modelOptions.AirToInternalSurfaceConductanceMultiplier;

        var surfaceToMassConductanceWPerK =
            modelOptions.InternalSurfaceToThermalMassConductanceWPerK ??
            hourlyInputProfile.TransmissionHeatTransferCoefficientWPerK * modelOptions.InternalSurfaceToThermalMassConductanceMultiplier;

        if (airToSurfaceConductanceWPerK <= 0)
            return Result.Validation("ISO 52016 physical room model air-to-surface conductance must be greater than zero.");

        if (surfaceToMassConductanceWPerK <= 0)
            return Result.Validation("ISO 52016 physical room model surface-to-mass conductance must be greater than zero.");

        return Result.Success();
    }

    private static Result ValidateSurfaces(
        IReadOnlyList<Iso52016PhysicalSurface> surfaces,
        Iso52016PhysicalNodeModelOptions modelOptions)
    {
        if (surfaces.Any(surface => surface is null))
            return Result.Validation("ISO 52016 physical room model surfaces cannot contain null records.");

        if (surfaces.Any(surface => string.IsNullOrWhiteSpace(surface.SurfaceId)))
            return Result.Validation("ISO 52016 physical room model surface ids are required.");

        if (surfaces.Select(surface => surface.SurfaceId.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != surfaces.Count)
            return Result.Validation("ISO 52016 physical room model surface ids must be unique.");

        var nodeIds = new List<string> { modelOptions.AirNodeId.Trim() };

        foreach (var surface in surfaces)
        {
            if (surface.AreaM2 <= 0)
                return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' area must be greater than zero.");

            if (surface.ConstructionLayers is null || surface.ConstructionLayers.Count == 0)
                return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' requires at least one construction layer.");

            foreach (var layer in surface.ConstructionLayers)
            {
                if (layer is null)
                    return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' construction layers cannot contain null records.");

                if (string.IsNullOrWhiteSpace(layer.LayerId))
                    return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' construction layer id is required.");

                if (layer.ThicknessM <= 0)
                    return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' construction layer '{layer.LayerId}' thickness must be greater than zero.");

                if (layer.ConductivityWPerMK <= 0)
                    return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' construction layer '{layer.LayerId}' conductivity must be greater than zero.");

                if (layer.DensityKgPerM3 <= 0)
                    return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' construction layer '{layer.LayerId}' density must be greater than zero.");

                if (layer.SpecificHeatCapacityJPerKgK <= 0)
                    return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' construction layer '{layer.LayerId}' specific heat must be greater than zero.");
            }

            foreach (var (name, value) in new[]
            {
                ("boundary conductance", surface.BoundaryConductanceWPerK),
                ("surface-to-air conductance", surface.SurfaceToAirConductanceWPerK),
                ("surface-to-mass conductance", surface.SurfaceToMassConductanceWPerK),
                ("surface heat capacity", surface.HeatCapacityJPerK),
                ("mass heat capacity", surface.MassHeatCapacityJPerK)
            })
            {
                if (value.HasValue && value.Value <= 0)
                    return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' {name} must be greater than zero.");
            }

            nodeIds.Add(Iso52016PhysicalRoomModelMapping.ResolveSurfaceNodeId(surface));
            nodeIds.Add(Iso52016PhysicalRoomModelMapping.ResolveMassNodeId(surface));
        }

        if (nodeIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != nodeIds.Count)
            return Result.Validation("ISO 52016 physical room model expanded surface node ids must be unique.");

        var solarValidation = ValidateDistributionFractions(
            surfaces,
            static surface => surface.SolarGainsDistributionFraction,
            "solar gains distribution fractions");

        if (solarValidation.IsFailure)
            return solarValidation;

        var radiativeValidation = ValidateDistributionFractions(
            surfaces,
            static surface => surface.InternalRadiativeGainsDistributionFraction,
            "internal radiative gains distribution fractions");

        if (radiativeValidation.IsFailure)
            return radiativeValidation;

        return Result.Success();
    }

    private static Result ValidateSurfaceBoundaryConditions(
        IReadOnlyList<Iso52016PhysicalSurfaceHourlyBoundaryCondition> surfaceBoundaryConditions,
        IReadOnlyList<Iso52016PhysicalSurface> surfaces,
        Iso52016RoomHourlyInputProfile hourlyInputProfile)
    {
        if (surfaceBoundaryConditions.Count == 0)
            return Result.Success();

        if (surfaces.Count == 0)
            return Result.Validation("ISO 52016 physical room model surface boundary conditions require explicit surfaces.");

        if (surfaceBoundaryConditions.Any(condition => condition is null))
            return Result.Validation("ISO 52016 physical room model surface boundary conditions cannot contain null records.");

        var surfaceIds = surfaces
            .Select(surface => surface.SurfaceId.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var hourIds = hourlyInputProfile.Hours
            .Select(hour => hour.HourOfYear)
            .ToHashSet();

        var uniqueKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var condition in surfaceBoundaryConditions)
        {
            if (string.IsNullOrWhiteSpace(condition.SurfaceId))
                return Result.Validation("ISO 52016 physical room model surface boundary condition surface id is required.");

            var surfaceId = condition.SurfaceId.Trim();

            if (!surfaceIds.Contains(surfaceId))
                return Result.Validation($"ISO 52016 physical room model boundary condition references unknown surface id '{surfaceId}'.");

            if (!hourIds.Contains(condition.HourOfYear))
                return Result.Validation($"ISO 52016 physical room model boundary condition for surface '{surfaceId}' references hour {condition.HourOfYear} that is not in the hourly profile.");

            if (double.IsNaN(condition.BoundaryTemperatureC) || double.IsInfinity(condition.BoundaryTemperatureC))
                return Result.Validation($"ISO 52016 physical room model boundary condition for surface '{surfaceId}' at hour {condition.HourOfYear} must be finite.");

            var key = $"{surfaceId}|{condition.HourOfYear}";

            if (!uniqueKeys.Add(key))
                return Result.Validation($"ISO 52016 physical room model duplicate boundary condition for surface '{surfaceId}' and hour {condition.HourOfYear}.");
        }

        return Result.Success();
    }

    private static Result ValidateOperationConditions(
        IReadOnlyList<Iso52016PhysicalHourlyOperationCondition> operationConditions,
        Iso52016RoomHourlyInputProfile hourlyInputProfile)
    {
        if (operationConditions.Count == 0)
            return Result.Success();

        if (operationConditions.Any(condition => condition is null))
            return Result.Validation("ISO 52016 physical room model operation conditions cannot contain null records.");

        var hourIds = hourlyInputProfile.Hours
            .Select(hour => hour.HourOfYear)
            .ToHashSet();

        var uniqueHours = new HashSet<int>();

        foreach (var condition in operationConditions)
        {
            if (!hourIds.Contains(condition.HourOfYear))
                return Result.Validation($"ISO 52016 physical room model operation condition references hour {condition.HourOfYear} that is not in the hourly profile.");

            if (!uniqueHours.Add(condition.HourOfYear))
                return Result.Validation($"ISO 52016 physical room model duplicate operation condition for hour {condition.HourOfYear}.");

            if (condition.VentilationHeatTransferCoefficientWPerK.HasValue &&
                (double.IsNaN(condition.VentilationHeatTransferCoefficientWPerK.Value) ||
                    double.IsInfinity(condition.VentilationHeatTransferCoefficientWPerK.Value) ||
                    condition.VentilationHeatTransferCoefficientWPerK.Value < 0))
            {
                return Result.Validation($"ISO 52016 physical room model operation condition ventilation heat transfer coefficient must be finite and non-negative at hour {condition.HourOfYear}.");
            }

            if (condition.VentilationBoundaryTemperatureC.HasValue &&
                (double.IsNaN(condition.VentilationBoundaryTemperatureC.Value) ||
                    double.IsInfinity(condition.VentilationBoundaryTemperatureC.Value)))
            {
                return Result.Validation($"ISO 52016 physical room model operation condition ventilation boundary temperature must be finite at hour {condition.HourOfYear}.");
            }

            if (condition.InternalGainsConvectiveFraction.HasValue &&
                (condition.InternalGainsConvectiveFraction.Value < 0 || condition.InternalGainsConvectiveFraction.Value > 1))
            {
                return Result.Validation($"ISO 52016 physical room model operation condition internal gains convective fraction must be between 0 and 1 at hour {condition.HourOfYear}.");
            }

            if (condition.SolarGainsToAirFraction.HasValue &&
                (condition.SolarGainsToAirFraction.Value < 0 || condition.SolarGainsToAirFraction.Value > 1))
            {
                return Result.Validation($"ISO 52016 physical room model operation condition solar gains to air fraction must be between 0 and 1 at hour {condition.HourOfYear}.");
            }
        }

        return Result.Success();
    }

    private static Result ValidateDistributionFractions(
        IReadOnlyList<Iso52016PhysicalSurface> surfaces,
        Func<Iso52016PhysicalSurface, double?> selector,
        string label)
    {
        var provided = surfaces
            .Select(selector)
            .ToArray();

        if (provided.All(value => !value.HasValue))
            return Result.Success();

        if (provided.Any(value => !value.HasValue))
            return Result.Validation($"ISO 52016 physical room model {label} must be provided for all surfaces when any surface specifies them.");

        foreach (var value in provided.Select(value => value!.Value))
        {
            if (value < 0 || value > 1)
                return Result.Validation($"ISO 52016 physical room model {label} must be between 0 and 1.");
        }

        if (Math.Abs(provided.Sum(value => value!.Value) - 1.0) > FractionTolerance)
            return Result.Validation($"ISO 52016 physical room model {label} must sum to 1.0.");

        return Result.Success();
    }
}
