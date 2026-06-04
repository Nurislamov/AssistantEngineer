using System.Reflection;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

internal static class Iso52016MatrixSeamCharacterizationTestHelper
{
    private const BindingFlags NonPublicStatic = BindingFlags.NonPublic | BindingFlags.Static;

    private static readonly MethodInfo BuildCoefficientMatrixMethod = typeof(Iso52016MatrixHourlySolver)
        .GetMethod("BuildCoefficientMatrix", NonPublicStatic)
        ?? throw new InvalidOperationException("BuildCoefficientMatrix method was not found.");

    private static readonly MethodInfo BuildRightHandSideMethod = typeof(Iso52016MatrixHourlySolver)
        .GetMethod("BuildRightHandSide", NonPublicStatic)
        ?? throw new InvalidOperationException("BuildRightHandSide method was not found.");

    private static readonly MethodInfo SolveLinearSystemMethod = typeof(Iso52016MatrixHourlySolver)
        .GetMethod("SolveLinearSystem", NonPublicStatic)
        ?? throw new InvalidOperationException("SolveLinearSystem method was not found.");

    public static Iso52016MatrixHourlySolverRequest CreateTwoNodeRequest(
        double outdoorTemperatureC = 8.0,
        double airNodeGainW = 150.0,
        double massNodeGainW = 0.0,
        double initialAirTemperatureC = 21.0,
        double initialMassTemperatureC = 21.0,
        int hourCount = 1)
    {
        var hours = Enumerable
            .Range(0, hourCount)
            .Select(hour => new Iso52016MatrixHourlyInputRecord(
                HourOfYear: hour,
                Month: 1,
                Day: 1,
                Hour: hour % 24,
                BoundaryTemperaturesC: new Dictionary<string, double>
                {
                    ["outdoor"] = outdoorTemperatureC
                },
                NodeHeatGainsW: new Dictionary<string, double>
                {
                    ["air"] = airNodeGainW,
                    ["mass"] = massNodeGainW
                },
                HeatingSetpointC: 20.0,
                CoolingSetpointC: 26.0))
            .ToArray();

        return new Iso52016MatrixHourlySolverRequest(
            ZoneCode: "matrix-seam-characterization",
            Nodes:
            [
                new Iso52016MatrixNodeDefinition("air", 1_200_000.0, initialAirTemperatureC, IsAirNode: true),
                new Iso52016MatrixNodeDefinition("mass", 8_000_000.0, initialMassTemperatureC)
            ],
            InternalConductances:
            [
                new Iso52016MatrixConductanceLink("air", "mass", 40.0)
            ],
            BoundaryConductances:
            [
                new Iso52016MatrixBoundaryConductance("air", "outdoor", 90.0),
                new Iso52016MatrixBoundaryConductance("mass", "outdoor", 15.0)
            ],
            Hours: hours,
            Options: new Iso52016MatrixHourlySolverOptions(
                TimeStepSeconds: 3600.0,
                AirNodeId: "air",
                DefaultHeatingSetpointC: 20.0,
                DefaultCoolingSetpointC: 26.0));
    }

    public static IReadOnlyDictionary<string, int> BuildNodeIndex(Iso52016MatrixHourlySolverRequest request)
    {
        return request.Nodes
            .Select((node, index) => new { node.NodeId, Index = index })
            .ToDictionary(item => item.NodeId, item => item.Index, StringComparer.OrdinalIgnoreCase);
    }

    public static double[,] BuildCoefficientMatrix(
        Iso52016MatrixHourlySolverRequest request,
        int hourIndex = 0)
    {
        var options = request.Options ?? new Iso52016MatrixHourlySolverOptions();

        return (double[,])Invoke(
            BuildCoefficientMatrixMethod,
            request.Nodes,
            request.InternalConductances,
            request.BoundaryConductances,
            BuildNodeIndex(request),
            request.Hours[hourIndex],
            options.TimeStepSeconds);
    }

    public static double[] BuildRightHandSide(
        Iso52016MatrixHourlySolverRequest request,
        IReadOnlyList<double> previousTemperaturesC,
        double hvacLoadW,
        int hourIndex = 0)
    {
        var options = request.Options ?? new Iso52016MatrixHourlySolverOptions();
        var nodeIndex = BuildNodeIndex(request);
        var airNodeIndex = nodeIndex[options.AirNodeId];

        return (double[])Invoke(
            BuildRightHandSideMethod,
            request.Nodes,
            request.BoundaryConductances,
            nodeIndex,
            request.Hours[hourIndex],
            previousTemperaturesC,
            options.TimeStepSeconds,
            hvacLoadW,
            airNodeIndex);
    }

    public static double[] SolveLinearSystem(double[,] matrix, double[] rhs)
    {
        return (double[])Invoke(
            SolveLinearSystemMethod,
            matrix,
            rhs);
    }

    private static object Invoke(MethodInfo method, params object[] args)
    {
        try
        {
            return method.Invoke(null, args)
                   ?? throw new InvalidOperationException($"Method '{method.Name}' returned null.");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
