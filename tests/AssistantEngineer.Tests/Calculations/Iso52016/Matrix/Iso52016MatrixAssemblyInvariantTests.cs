namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public sealed class Iso52016MatrixAssemblyInvariantTests
{
    private const double RepeatTolerance = 1e-9;

    [Fact]
    public void Assembly_BuildsExpectedDimensionsFiniteValuesAndDeterministicRepeat()
    {
        var request = Iso52016MatrixSeamCharacterizationTestHelper.CreateTwoNodeRequest();

        var firstMatrix = Iso52016MatrixSeamCharacterizationTestHelper.BuildCoefficientMatrix(request);
        var secondMatrix = Iso52016MatrixSeamCharacterizationTestHelper.BuildCoefficientMatrix(request);

        var size = request.Nodes.Count;
        Assert.Equal(size, firstMatrix.GetLength(0));
        Assert.Equal(size, firstMatrix.GetLength(1));
        Assert.Equal(size, secondMatrix.GetLength(0));
        Assert.Equal(size, secondMatrix.GetLength(1));

        for (var row = 0; row < size; row++)
        {
            for (var column = 0; column < size; column++)
            {
                var first = firstMatrix[row, column];
                var second = secondMatrix[row, column];

                Assert.False(double.IsNaN(first));
                Assert.False(double.IsInfinity(first));
                Assert.InRange(Math.Abs(first - second), 0.0, RepeatTolerance);
            }
        }

        var nodeIndex = Iso52016MatrixSeamCharacterizationTestHelper.BuildNodeIndex(request);
        var airIndex = nodeIndex["air"];
        var massIndex = nodeIndex["mass"];

        Assert.True(firstMatrix[airIndex, airIndex] > 0.0);
        Assert.True(firstMatrix[massIndex, massIndex] > 0.0);
        Assert.True(firstMatrix[airIndex, massIndex] < 0.0);
        Assert.True(firstMatrix[massIndex, airIndex] < 0.0);

        var rhs = Iso52016MatrixSeamCharacterizationTestHelper.BuildRightHandSide(
            request,
            previousTemperaturesC: request.Nodes.Select(node => node.InitialTemperatureC).ToArray(),
            hvacLoadW: 0.0);

        Assert.Equal(size, rhs.Length);
        Assert.All(rhs, value =>
        {
            Assert.False(double.IsNaN(value));
            Assert.False(double.IsInfinity(value));
        });
    }
}
