namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public sealed class Iso52016SolverKernelCharacterizationTests
{
    private const double NumericTolerance = 1e-6;
    private const double RepeatTolerance = 1e-9;

    [Fact]
    public void SolveLinearSystem_KnownCaseRemainsPinnedFiniteAndDeterministic()
    {
        var matrix = new[,]
        {
            { 2.0, 1.0 },
            { 1.0, 3.0 }
        };
        var rhs = new[] { 1.0, 2.0 };

        var first = Iso52016MatrixSeamCharacterizationTestHelper.SolveLinearSystem(matrix, rhs);
        var second = Iso52016MatrixSeamCharacterizationTestHelper.SolveLinearSystem(matrix, rhs);

        Assert.Equal(2, first.Length);
        Assert.Equal(2, second.Length);

        Assert.InRange(first[0], 0.2 - NumericTolerance, 0.2 + NumericTolerance);
        Assert.InRange(first[1], 0.6 - NumericTolerance, 0.6 + NumericTolerance);

        for (var i = 0; i < first.Length; i++)
        {
            Assert.False(double.IsNaN(first[i]));
            Assert.False(double.IsInfinity(first[i]));
            Assert.InRange(Math.Abs(first[i] - second[i]), 0.0, RepeatTolerance);
        }
    }

    [Fact]
    public void SolveLinearSystem_SingularMatrixRetainsCurrentFailureBehavior()
    {
        var singular = new[,]
        {
            { 1.0, 2.0 },
            { 2.0, 4.0 }
        };
        var rhs = new[] { 1.0, 2.0 };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            Iso52016MatrixSeamCharacterizationTestHelper.SolveLinearSystem(singular, rhs));

        Assert.Contains("singular", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
