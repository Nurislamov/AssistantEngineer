using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

public sealed class Iso52016MatrixHourlySolver : IIso52016MatrixHourlySolver
{
    private const double MinimumPositive = 0.000000001;
    private const double UnitHvacLoadW = 1.0;

    public Result<Iso52016MatrixHourlySolverProfile> Solve(
        Iso52016MatrixHourlySolverRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016MatrixHourlySolverProfile>.Failure(validation);

        var options = request.Options ?? new Iso52016MatrixHourlySolverOptions();

        try
        {
            var hours = SolveHours(
                request,
                options);

            return Result<Iso52016MatrixHourlySolverProfile>.Success(
                new Iso52016MatrixHourlySolverProfile(
                    ZoneCode: request.ZoneCode,
                    Options: options,
                    Hours: hours,
                    MonthlySummaries: BuildMonthlySummaries(hours)));
        }
        catch (InvalidOperationException exception)
        {
            return Result<Iso52016MatrixHourlySolverProfile>.Failure(exception.Message);
        }
    }

    private static IReadOnlyList<Iso52016MatrixHourlyResult> SolveHours(
        Iso52016MatrixHourlySolverRequest request,
        Iso52016MatrixHourlySolverOptions options)
    {
        var nodeIndex = request.Nodes
            .Select((node, index) => new { node.NodeId, Index = index })
            .ToDictionary(
                node => node.NodeId,
                node => node.Index,
                StringComparer.OrdinalIgnoreCase);

        var previousTemperatures = request.Nodes
            .Select(node => node.InitialTemperatureC)
            .ToArray();

        var results = new List<Iso52016MatrixHourlyResult>(request.Hours.Count);

        foreach (var hour in request.Hours)
        {
            var result = SolveHour(
                request.Nodes,
                request.InternalConductances,
                request.BoundaryConductances,
                nodeIndex,
                hour,
                previousTemperatures,
                options);

            results.Add(result);

            previousTemperatures = result.NodeStates
                .Select(state => state.TemperatureAfterHvacC)
                .ToArray();
        }

        return results;
    }

    private static Iso52016MatrixHourlyResult SolveHour(
        IReadOnlyList<Iso52016MatrixNodeDefinition> nodes,
        IReadOnlyList<Iso52016MatrixConductanceLink> internalConductances,
        IReadOnlyList<Iso52016MatrixBoundaryConductance> boundaryConductances,
        IReadOnlyDictionary<string, int> nodeIndex,
        Iso52016MatrixHourlyInputRecord hour,
        IReadOnlyList<double> previousTemperaturesC,
        Iso52016MatrixHourlySolverOptions options)
    {
        var heatingSetpointC = hour.HeatingSetpointC ?? options.DefaultHeatingSetpointC;
        var coolingSetpointC = hour.CoolingSetpointC ?? options.DefaultCoolingSetpointC;
        var airNodeIndex = nodeIndex[options.AirNodeId];

        var freeFloatingTemperatures = SolveTemperatures(
            nodes,
            internalConductances,
            boundaryConductances,
            nodeIndex,
            hour,
            previousTemperaturesC,
            options.TimeStepSeconds,
            hvacLoadW: 0.0,
            hvacNodeIndex: airNodeIndex);

        var freeFloatingAirTemperatureC = freeFloatingTemperatures[airNodeIndex];
        var controlledTemperatures = freeFloatingTemperatures;
        var heatingLoadW = 0.0;
        var coolingLoadW = 0.0;

        if (freeFloatingAirTemperatureC < heatingSetpointC)
        {
            var responsePerW = CalculateAirTemperatureResponsePerW(
                nodes,
                internalConductances,
                boundaryConductances,
                nodeIndex,
                hour,
                previousTemperaturesC,
                options.TimeStepSeconds,
                airNodeIndex,
                freeFloatingAirTemperatureC);

            if (responsePerW <= MinimumPositive)
                throw new InvalidOperationException("ISO 52016 Matrix solver cannot control the air node because HVAC response is zero.");

            heatingLoadW = (heatingSetpointC - freeFloatingAirTemperatureC) / responsePerW;

            controlledTemperatures = SolveTemperatures(
                nodes,
                internalConductances,
                boundaryConductances,
                nodeIndex,
                hour,
                previousTemperaturesC,
                options.TimeStepSeconds,
                hvacLoadW: heatingLoadW,
                hvacNodeIndex: airNodeIndex);
        }
        else if (freeFloatingAirTemperatureC > coolingSetpointC)
        {
            var responsePerW = CalculateAirTemperatureResponsePerW(
                nodes,
                internalConductances,
                boundaryConductances,
                nodeIndex,
                hour,
                previousTemperaturesC,
                options.TimeStepSeconds,
                airNodeIndex,
                freeFloatingAirTemperatureC);

            if (responsePerW <= MinimumPositive)
                throw new InvalidOperationException("ISO 52016 Matrix solver cannot control the air node because HVAC response is zero.");

            coolingLoadW = (freeFloatingAirTemperatureC - coolingSetpointC) / responsePerW;

            controlledTemperatures = SolveTemperatures(
                nodes,
                internalConductances,
                boundaryConductances,
                nodeIndex,
                hour,
                previousTemperaturesC,
                options.TimeStepSeconds,
                hvacLoadW: -coolingLoadW,
                hvacNodeIndex: airNodeIndex);
        }

        return CreateHourlyResult(
            nodes,
            hour,
            heatingSetpointC,
            coolingSetpointC,
            freeFloatingTemperatures,
            controlledTemperatures,
            heatingLoadW,
            coolingLoadW,
            options.TimeStepSeconds,
            airNodeIndex);
    }

    private static double CalculateAirTemperatureResponsePerW(
        IReadOnlyList<Iso52016MatrixNodeDefinition> nodes,
        IReadOnlyList<Iso52016MatrixConductanceLink> internalConductances,
        IReadOnlyList<Iso52016MatrixBoundaryConductance> boundaryConductances,
        IReadOnlyDictionary<string, int> nodeIndex,
        Iso52016MatrixHourlyInputRecord hour,
        IReadOnlyList<double> previousTemperaturesC,
        double timeStepSeconds,
        int airNodeIndex,
        double freeFloatingAirTemperatureC)
    {
        var withUnitLoad = SolveTemperatures(
            nodes,
            internalConductances,
            boundaryConductances,
            nodeIndex,
            hour,
            previousTemperaturesC,
            timeStepSeconds,
            hvacLoadW: UnitHvacLoadW,
            hvacNodeIndex: airNodeIndex);

        return withUnitLoad[airNodeIndex] - freeFloatingAirTemperatureC;
    }

    private static double[] SolveTemperatures(
        IReadOnlyList<Iso52016MatrixNodeDefinition> nodes,
        IReadOnlyList<Iso52016MatrixConductanceLink> internalConductances,
        IReadOnlyList<Iso52016MatrixBoundaryConductance> boundaryConductances,
        IReadOnlyDictionary<string, int> nodeIndex,
        Iso52016MatrixHourlyInputRecord hour,
        IReadOnlyList<double> previousTemperaturesC,
        double timeStepSeconds,
        double hvacLoadW,
        int hvacNodeIndex)
    {
        var matrix = BuildCoefficientMatrix(
            nodes,
            internalConductances,
            boundaryConductances,
            nodeIndex,
            hour,
            timeStepSeconds);

        var rhs = BuildRightHandSide(
            nodes,
            boundaryConductances,
            nodeIndex,
            hour,
            previousTemperaturesC,
            timeStepSeconds,
            hvacLoadW,
            hvacNodeIndex);

        return SolveLinearSystem(matrix, rhs);
    }

    private static double[,] BuildCoefficientMatrix(
        IReadOnlyList<Iso52016MatrixNodeDefinition> nodes,
        IReadOnlyList<Iso52016MatrixConductanceLink> internalConductances,
        IReadOnlyList<Iso52016MatrixBoundaryConductance> boundaryConductances,
        IReadOnlyDictionary<string, int> nodeIndex,
        Iso52016MatrixHourlyInputRecord hour,
        double timeStepSeconds)
    {
        var size = nodes.Count;
        var matrix = new double[size, size];

        for (var i = 0; i < size; i++)
        {
            matrix[i, i] = nodes[i].HeatCapacityJPerK / timeStepSeconds;
        }

        foreach (var link in internalConductances)
        {
            var fromIndex = nodeIndex[link.FromNodeId];
            var toIndex = nodeIndex[link.ToNodeId];
            var conductance = link.ConductanceWPerK;

            matrix[fromIndex, fromIndex] += conductance;
            matrix[toIndex, toIndex] += conductance;
            matrix[fromIndex, toIndex] -= conductance;
            matrix[toIndex, fromIndex] -= conductance;
        }

        foreach (var link in boundaryConductances)
        {
            matrix[nodeIndex[link.NodeId], nodeIndex[link.NodeId]] +=
                ResolveBoundaryConductanceWPerK(link, hour);
        }

        return matrix;
    }

    private static double[] BuildRightHandSide(
        IReadOnlyList<Iso52016MatrixNodeDefinition> nodes,
        IReadOnlyList<Iso52016MatrixBoundaryConductance> boundaryConductances,
        IReadOnlyDictionary<string, int> nodeIndex,
        Iso52016MatrixHourlyInputRecord hour,
        IReadOnlyList<double> previousTemperaturesC,
        double timeStepSeconds,
        double hvacLoadW,
        int hvacNodeIndex)
    {
        var rhs = new double[nodes.Count];

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            rhs[i] = node.HeatCapacityJPerK / timeStepSeconds * previousTemperaturesC[i];

            if (hour.NodeHeatGainsW.TryGetValue(node.NodeId, out var gainW))
                rhs[i] += gainW;
        }

        rhs[hvacNodeIndex] += hvacLoadW;

        foreach (var link in boundaryConductances)
        {
            rhs[nodeIndex[link.NodeId]] +=
                ResolveBoundaryConductanceWPerK(link, hour) *
                hour.BoundaryTemperaturesC[link.BoundaryId];
        }

        return rhs;
    }

    private static double ResolveBoundaryConductanceWPerK(
        Iso52016MatrixBoundaryConductance link,
        Iso52016MatrixHourlyInputRecord hour)
    {
        foreach (var overrideRecord in hour.BoundaryConductanceOverrides ?? Array.Empty<Iso52016MatrixHourlyBoundaryConductanceOverride>())
        {
            if (string.Equals(overrideRecord.NodeId, link.NodeId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(overrideRecord.BoundaryId, link.BoundaryId, StringComparison.OrdinalIgnoreCase))
            {
                return overrideRecord.ConductanceWPerK;
            }
        }

        return link.ConductanceWPerK;
    }

    private static double[] SolveLinearSystem(
        double[,] matrix,
        double[] rhs)
    {
        var size = rhs.Length;
        var a = (double[,])matrix.Clone();
        var b = (double[])rhs.Clone();

        for (var pivot = 0; pivot < size; pivot++)
        {
            var bestRow = pivot;
            var bestValue = Math.Abs(a[pivot, pivot]);

            for (var row = pivot + 1; row < size; row++)
            {
                var value = Math.Abs(a[row, pivot]);

                if (value > bestValue)
                {
                    bestValue = value;
                    bestRow = row;
                }
            }

            if (bestValue <= MinimumPositive)
                throw new InvalidOperationException("ISO 52016 Matrix solver matrix is singular or ill-conditioned.");

            if (bestRow != pivot)
            {
                for (var column = pivot; column < size; column++)
                {
                    (a[pivot, column], a[bestRow, column]) = (a[bestRow, column], a[pivot, column]);
                }

                (b[pivot], b[bestRow]) = (b[bestRow], b[pivot]);
            }

            for (var row = pivot + 1; row < size; row++)
            {
                var factor = a[row, pivot] / a[pivot, pivot];
                if (Math.Abs(factor) <= MinimumPositive)
                    continue;

                for (var column = pivot; column < size; column++)
                {
                    a[row, column] -= factor * a[pivot, column];
                }

                b[row] -= factor * b[pivot];
            }
        }

        var solution = new double[size];

        for (var row = size - 1; row >= 0; row--)
        {
            var sum = b[row];

            for (var column = row + 1; column < size; column++)
            {
                sum -= a[row, column] * solution[column];
            }

            solution[row] = sum / a[row, row];
        }

        return solution;
    }

    private static Iso52016MatrixHourlyResult CreateHourlyResult(
        IReadOnlyList<Iso52016MatrixNodeDefinition> nodes,
        Iso52016MatrixHourlyInputRecord hour,
        double heatingSetpointC,
        double coolingSetpointC,
        IReadOnlyList<double> freeFloatingTemperatures,
        IReadOnlyList<double> controlledTemperatures,
        double heatingLoadW,
        double coolingLoadW,
        double timeStepSeconds,
        int airNodeIndex)
    {
        var nodeStates = nodes
            .Select((node, index) => new Iso52016MatrixHourlyNodeState(
                NodeId: node.NodeId,
                TemperatureBeforeHvacC: freeFloatingTemperatures[index],
                TemperatureAfterHvacC: controlledTemperatures[index],
                HeatGainW: hour.NodeHeatGainsW.TryGetValue(node.NodeId, out var gainW) ? gainW : 0.0))
            .ToArray();

        return new Iso52016MatrixHourlyResult(
            HourOfYear: hour.HourOfYear,
            Month: hour.Month,
            Day: hour.Day,
            Hour: hour.Hour,
            HeatingSetpointC: heatingSetpointC,
            CoolingSetpointC: coolingSetpointC,
            AirTemperatureBeforeHvacC: freeFloatingTemperatures[airNodeIndex],
            AirTemperatureAfterHvacC: controlledTemperatures[airNodeIndex],
            HeatingLoadW: heatingLoadW,
            CoolingLoadW: coolingLoadW,
            TimeStepSeconds: timeStepSeconds,
            NodeStates: nodeStates);
    }

    private static IReadOnlyList<Iso52016MatrixMonthlySummary> BuildMonthlySummaries(
        IReadOnlyList<Iso52016MatrixHourlyResult> hours) =>
        hours
            .GroupBy(hour => hour.Month)
            .OrderBy(group => group.Key)
            .Select(group => new Iso52016MatrixMonthlySummary(
                Month: group.Key,
                HeatingEnergyKWh: group.Sum(hour => hour.HeatingEnergyKWh),
                CoolingEnergyKWh: group.Sum(hour => hour.CoolingEnergyKWh),
                TotalNodeHeatGainsKWh: group.Sum(hour => hour.TotalNodeHeatGainsKWh),
                PeakHeatingLoadW: group.Max(hour => hour.HeatingLoadW),
                PeakCoolingLoadW: group.Max(hour => hour.CoolingLoadW),
                AverageAirTemperatureAfterHvacC: group.Average(hour => hour.AirTemperatureAfterHvacC)))
            .ToArray();

    private static Result Validate(
        Iso52016MatrixHourlySolverRequest request)
    {
        if (request is null)
            return Result.Validation("ISO 52016 Matrix solver request is required.");

        if (string.IsNullOrWhiteSpace(request.ZoneCode))
            return Result.Validation("ISO 52016 Matrix zone code is required.");

        if (request.Nodes is null || request.Nodes.Count == 0)
            return Result.Validation("ISO 52016 Matrix solver requires at least one thermal node.");

        if (request.Hours is null || request.Hours.Count == 0)
            return Result.Validation("ISO 52016 Matrix solver requires at least one hourly input record.");

        var options = request.Options ?? new Iso52016MatrixHourlySolverOptions();

        if (options.TimeStepSeconds <= 0)
            return Result.Validation("ISO 52016 Matrix solver time step must be greater than zero.");

        if (options.DefaultCoolingSetpointC <= options.DefaultHeatingSetpointC)
            return Result.Validation("ISO 52016 Matrix default cooling setpoint must be greater than heating setpoint.");

        var nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in request.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.NodeId))
                return Result.Validation("ISO 52016 Matrix node id is required.");

            if (!nodeIds.Add(node.NodeId))
                return Result.Validation($"ISO 52016 Matrix node id '{node.NodeId}' is duplicated.");

            if (node.HeatCapacityJPerK <= 0)
                return Result.Validation($"ISO 52016 Matrix node '{node.NodeId}' heat capacity must be greater than zero.");
        }

        if (!nodeIds.Contains(options.AirNodeId))
            return Result.Validation($"ISO 52016 Matrix air node '{options.AirNodeId}' was not found in node definitions.");

        foreach (var link in request.InternalConductances ?? Array.Empty<Iso52016MatrixConductanceLink>())
        {
            if (!nodeIds.Contains(link.FromNodeId))
                return Result.Validation($"ISO 52016 Matrix internal conductance from-node '{link.FromNodeId}' was not found.");

            if (!nodeIds.Contains(link.ToNodeId))
                return Result.Validation($"ISO 52016 Matrix internal conductance to-node '{link.ToNodeId}' was not found.");

            if (string.Equals(link.FromNodeId, link.ToNodeId, StringComparison.OrdinalIgnoreCase))
                return Result.Validation("ISO 52016 Matrix internal conductance cannot connect a node to itself.");

            if (link.ConductanceWPerK <= 0)
                return Result.Validation("ISO 52016 Matrix internal conductance must be greater than zero.");
        }

        var boundaryLinkKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var link in request.BoundaryConductances ?? Array.Empty<Iso52016MatrixBoundaryConductance>())
        {
            if (!nodeIds.Contains(link.NodeId))
                return Result.Validation($"ISO 52016 Matrix boundary conductance node '{link.NodeId}' was not found.");

            if (string.IsNullOrWhiteSpace(link.BoundaryId))
                return Result.Validation("ISO 52016 Matrix boundary id is required.");

            if (link.ConductanceWPerK <= 0)
                return Result.Validation("ISO 52016 Matrix boundary conductance must be greater than zero.");

            var key = CreateBoundaryConductanceKey(link.NodeId, link.BoundaryId);

            if (!boundaryLinkKeys.Add(key))
                return Result.Validation($"ISO 52016 Matrix boundary conductance link '{link.NodeId}|{link.BoundaryId}' is duplicated.");
        }

        foreach (var hour in request.Hours)
        {
            if (hour.BoundaryTemperaturesC is null)
                return Result.Validation($"ISO 52016 Matrix boundary temperatures are required at hour {hour.HourOfYear}.");

            if (hour.NodeHeatGainsW is null)
                return Result.Validation($"ISO 52016 Matrix node heat gains are required at hour {hour.HourOfYear}.");

            foreach (var boundary in request.BoundaryConductances ?? Array.Empty<Iso52016MatrixBoundaryConductance>())
            {
                if (!hour.BoundaryTemperaturesC.ContainsKey(boundary.BoundaryId))
                    return Result.Validation($"ISO 52016 Matrix boundary temperature '{boundary.BoundaryId}' is missing at hour {hour.HourOfYear}.");
            }

            foreach (var nodeGain in hour.NodeHeatGainsW)
            {
                if (!nodeIds.Contains(nodeGain.Key))
                    return Result.Validation($"ISO 52016 Matrix node heat gain references unknown node '{nodeGain.Key}' at hour {hour.HourOfYear}.");
            }

            var boundaryOverrideValidation = ValidateBoundaryConductanceOverrides(
                hour,
                nodeIds,
                boundaryLinkKeys);

            if (boundaryOverrideValidation.IsFailure)
                return boundaryOverrideValidation;

            var heatingSetpointC = hour.HeatingSetpointC ?? options.DefaultHeatingSetpointC;
            var coolingSetpointC = hour.CoolingSetpointC ?? options.DefaultCoolingSetpointC;

            if (coolingSetpointC <= heatingSetpointC)
                return Result.Validation($"ISO 52016 Matrix cooling setpoint must be greater than heating setpoint at hour {hour.HourOfYear}.");
        }

        return Result.Success();
    }

    private static Result ValidateBoundaryConductanceOverrides(
        Iso52016MatrixHourlyInputRecord hour,
        IReadOnlySet<string> nodeIds,
        IReadOnlySet<string> boundaryLinkKeys)
    {
        var uniqueOverrideKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var overrideRecord in hour.BoundaryConductanceOverrides ?? Array.Empty<Iso52016MatrixHourlyBoundaryConductanceOverride>())
        {
            if (overrideRecord is null)
                return Result.Validation($"ISO 52016 Matrix boundary conductance overrides cannot contain null records at hour {hour.HourOfYear}.");

            if (string.IsNullOrWhiteSpace(overrideRecord.NodeId))
                return Result.Validation($"ISO 52016 Matrix boundary conductance override node id is required at hour {hour.HourOfYear}.");

            if (!nodeIds.Contains(overrideRecord.NodeId.Trim()))
                return Result.Validation($"ISO 52016 Matrix boundary conductance override node '{overrideRecord.NodeId}' was not found at hour {hour.HourOfYear}.");

            if (string.IsNullOrWhiteSpace(overrideRecord.BoundaryId))
                return Result.Validation($"ISO 52016 Matrix boundary conductance override boundary id is required at hour {hour.HourOfYear}.");

            var key = CreateBoundaryConductanceKey(overrideRecord.NodeId, overrideRecord.BoundaryId);

            if (!boundaryLinkKeys.Contains(key))
                return Result.Validation($"ISO 52016 Matrix boundary conductance override '{overrideRecord.NodeId}|{overrideRecord.BoundaryId}' does not match a declared boundary conductance link at hour {hour.HourOfYear}.");

            if (!uniqueOverrideKeys.Add(key))
                return Result.Validation($"ISO 52016 Matrix duplicate boundary conductance override '{overrideRecord.NodeId}|{overrideRecord.BoundaryId}' at hour {hour.HourOfYear}.");

            if (double.IsNaN(overrideRecord.ConductanceWPerK) || double.IsInfinity(overrideRecord.ConductanceWPerK) || overrideRecord.ConductanceWPerK < 0)
                return Result.Validation($"ISO 52016 Matrix boundary conductance override '{overrideRecord.NodeId}|{overrideRecord.BoundaryId}' must be finite and non-negative at hour {hour.HourOfYear}.");
        }

        return Result.Success();
    }

    private static string CreateBoundaryConductanceKey(
        string nodeId,
        string boundaryId) =>
        $"{nodeId.Trim()}|{boundaryId.Trim()}";
}