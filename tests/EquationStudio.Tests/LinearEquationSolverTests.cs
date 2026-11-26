using EquationStudio.Core;

namespace EquationStudio.Tests;

public sealed class LinearEquationSolverTests
{
    [Theory]
    [InlineData("2x + 3 = 11", "4")]
    [InlineData("3(x - 2) = x + 4", "5")]
    [InlineData("x / 3 = 1 / 2", "3/2")]
    [InlineData("−(x + 1) = ٢", "-3")]
    [InlineData("0.1x = 0.3", "3")]
    [InlineData("-2x + 4 = 0", "2")]
    [InlineData("2x = 0", "0")]
    [InlineData("۰٫۵x = ۱٫۲۵", "5/2")]
    public void SolvesAndChecksTheOriginalEquation(string equation, string expected)
    {
        var result = LinearEquationSolver.Solve(equation);

        Assert.Equal(SolutionKind.Unique, result.Kind);
        Assert.Equal(expected, result.Root?.ToString());
        Assert.Equal(result.Left.At(result.Root!), result.Right.At(result.Root!));
        Assert.Equal($"x = {expected}", result.Steps[^1].After);
    }

    [Theory]
    [InlineData("2x + 1 = 2x + 2", SolutionKind.None, "0 = 1")]
    [InlineData("x + 1 = x + 1.000000000000000001", SolutionKind.None, "0 = 1/1000000000000000000")]
    [InlineData("2(x + 1) = 2x + 2", SolutionKind.Infinite, "0 = 0")]
    [InlineData("2 = 2", SolutionKind.Infinite, "0 = 0")]
    public void ClassifiesEquationsWithoutAnIsolatedRoot(string equation, SolutionKind expected, string lastStep)
    {
        var result = LinearEquationSolver.Solve(equation);

        Assert.Equal(expected, result.Kind);
        Assert.Null(result.Root);
        Assert.Equal(lastStep, result.Steps[^1].After);
    }

    [Theory]
    [InlineData("x*x = 2")]
    [InlineData("2/x = 4")]
    [InlineData("x/0 = 3")]
    [InlineData("x = 3 = 4")]
    [InlineData("x 2 = 4")]
    [InlineData("2 3x = 4")]
    [InlineData("(x = 4")]
    [InlineData("x^2 = 4")]
    [InlineData("x = 4\0x")]
    public void RejectsUnsupportedOrMalformedEquations(string equation) =>
        Assert.Throws<EquationInputException>(() => LinearEquationSolver.Solve(equation));

    [Fact]
    public void LimitsNestingAndInputLength()
    {
        Assert.Throws<EquationInputException>(() => LinearEquationSolver.Solve(new string('(', 40) + "x" + new string(')', 40) + "=0"));
        Assert.Throws<EquationInputException>(() => LinearEquationSolver.Solve(new string('1', 300) + "x=0"));
    }

    [Fact]
    public void StepsJoinWithoutSkippingBetweenTheirBeforeAndAfterStates()
    {
        var result = LinearEquationSolver.Solve("3(x - 2) = x + 4");

        Assert.Equal(StepOperation.Simplify, result.Steps[0].Operation);
        Assert.Equal(StepOperation.SubtractVariable, result.Steps[1].Operation);
        Assert.Equal(Rational.Parse("1"), result.Steps[1].Operand);
        Assert.Equal("2x - 6 = 4", result.Steps[1].After);
        Assert.Equal(StepOperation.Divide, result.Steps[^1].Operation);
        for (int index = 1; index < result.Steps.Count; index++)
        {
            Assert.Equal(result.Steps[index - 1].After, result.Steps[index].Before);
        }
    }

    [Fact]
    public void HarderPracticeHasAUniqueAnswerAndMoreComplexInput()
    {
        foreach (var level in new[] { PracticeLevel.BothSides, PracticeLevel.Parentheses, PracticeLevel.Fractions })
        {
            var exercise = LinearPractice.Create(level, new Random(42));
            var result = LinearEquationSolver.Solve(exercise.Equation);

            Assert.Equal(Rational.Parse(exercise.ExpectedRoot.ToString()), result.Root);
            Assert.NotEqual(Rational.Zero, result.Left.Slope);
            Assert.NotEqual(Rational.Zero, result.Right.Slope);
        }
    }
}
