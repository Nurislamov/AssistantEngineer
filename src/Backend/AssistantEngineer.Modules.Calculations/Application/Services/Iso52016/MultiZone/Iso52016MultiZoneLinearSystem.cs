namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

internal static class Iso52016MultiZoneLinearSystem
{
    private const double MinimumPivot = 1e-9;

    internal static double[] SolveLinearSystem(
        double[,] matrix,
        double[] rhs)
    {
        var size = rhs.Length;
        var a = (double[,])matrix.Clone();
        var b = (double[])rhs.Clone();

        for (var pivot = 0; pivot < size; pivot++)
        {
            var bestRow = pivot;
            var bestAbs = Math.Abs(a[pivot, pivot]);

            for (var row = pivot + 1; row < size; row++)
            {
                var candidateAbs = Math.Abs(a[row, pivot]);
                if (candidateAbs > bestAbs)
                {
                    bestAbs = candidateAbs;
                    bestRow = row;
                }
            }

            if (bestAbs <= MinimumPivot)
                throw new InvalidOperationException("Matrix is singular or ill-conditioned for this multi-zone hourly step.");

            if (bestRow != pivot)
            {
                for (var col = pivot; col < size; col++)
                {
                    (a[pivot, col], a[bestRow, col]) = (a[bestRow, col], a[pivot, col]);
                }

                (b[pivot], b[bestRow]) = (b[bestRow], b[pivot]);
            }

            for (var row = pivot + 1; row < size; row++)
            {
                var factor = a[row, pivot] / a[pivot, pivot];
                if (Math.Abs(factor) <= MinimumPivot)
                    continue;

                for (var col = pivot; col < size; col++)
                {
                    a[row, col] -= factor * a[pivot, col];
                }

                b[row] -= factor * b[pivot];
            }
        }

        var solution = new double[size];
        for (var row = size - 1; row >= 0; row--)
        {
            var sum = b[row];
            for (var col = row + 1; col < size; col++)
            {
                sum -= a[row, col] * solution[col];
            }

            solution[row] = sum / a[row, row];
        }

        return solution;
    }

    internal static double[,] BuildResponseMatrix(double[,] aMatrix)
    {
        var size = aMatrix.GetLength(0);
        var response = new double[size, size];

        for (var column = 0; column < size; column++)
        {
            var rhs = new double[size];
            rhs[column] = 1.0;
            var solution = SolveLinearSystem(aMatrix, rhs);
            for (var row = 0; row < size; row++)
            {
                response[row, column] = solution[row];
            }
        }

        return response;
    }

    internal static double[] ApplyResponse(
        IReadOnlyList<double> freeFloatingTemperatures,
        double[,] responseMatrix,
        IReadOnlyList<double> hvacLoads)
    {
        var controlled = new double[freeFloatingTemperatures.Count];
        for (var i = 0; i < freeFloatingTemperatures.Count; i++)
        {
            var value = freeFloatingTemperatures[i];
            for (var j = 0; j < hvacLoads.Count; j++)
            {
                value += responseMatrix[i, j] * hvacLoads[j];
            }

            controlled[i] = value;
        }

        return controlled;
    }
}
