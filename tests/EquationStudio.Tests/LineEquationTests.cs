using EquationStudio.Core;

namespace EquationStudio.Tests;

public sealed class LineEquationTests
{
    [Theory]
    [InlineData("2x + 5", "2", "-5/2", "5")]
    [InlineData(" f ( x ) = 2x + 5", "2", "-5/2", "5")]
    [InlineData("٢x − ٣y = ٦", "2/3", "3", "-2")]
    [InlineData("3y = 2(1 + x) - 7", "2/3", "5/2", "-5/3")]
    [InlineData("x + y = 1 + 2x", "1", "-1", "1")]
    [InlineData("y/2 = 3", "0", null, "6")]
    [InlineData("y = 0", "0", null, "0")]
    [InlineData("x = 3", null, "3", null)]
    [InlineData("0 = x + 3", null, "-3", null)]
    [InlineData("x = 0", null, "0", null)]
    [InlineData("[2x - (y + 1)] = 0", "2", "1/2", "-1")]
    [InlineData("2X ＋ ３Y ＝ ６", "-2/3", "3", "2")]
    public void FindsExactFeaturesOfAPlottableLine(string input, string? slope, string? xIntercept, string? yIntercept)
    {
        var line = LineEquation.Parse(input);

        Assert.Equal(LineEquationKind.Line, line.Kind);
        Assert.Equal(slope, line.Slope?.ToString());
        Assert.Equal(xIntercept, line.XIntercept?.ToString());
        Assert.Equal(yIntercept, line.YIntercept?.ToString());
    }

    [Theory]
    [InlineData("x + y = y + x", LineEquationKind.AllPoints)]
    [InlineData("0 = 0", LineEquationKind.AllPoints)]
    [InlineData("x + y = y + x + 1", LineEquationKind.NoPoints)]
    public void ClassifiesDegenerateLines(string input, LineEquationKind kind) =>
        Assert.Equal(kind, LineEquation.Parse(input).Kind);

    [Theory]
    [InlineData("2x + 3y = 6", true)]
    [InlineData("F(x)=x+1", true)]
    [InlineData("2x - 1", true)]
    [InlineData("x = 2", false)]
    [InlineData("2x + 3 = 7", false)]
    public void DefaultsOnlyUnambiguousLineInputsToPlot(string input, bool expected) =>
        Assert.Equal(expected, LineEquation.MatchesPlotInput(input));

    [Theory]
    [InlineData("x*y = 2")]
    [InlineData("x/y = 3")]
    [InlineData("x/0 = 1")]
    [InlineData("z + y = 7")]
    [InlineData("2[x + y) = 4")]
    [InlineData("x^2 + y = 4")]
    [InlineData("f(x) = y + 1")]
    [InlineData("f(x) = y = x")]
    public void RejectsNonlinearOrMalformedLineInputs(string input) =>
        Assert.Throws<EquationInputException>(() => LineEquation.Parse(input));

    [Fact]
    public void AOneVariableSolveStillRejectsYTerms() =>
        Assert.Throws<EquationInputException>(() => LinearEquationSolver.Solve("x + y = 2 + y"));

    [Fact]
    public void AFiniteRatioWithLargeExactTermsCanBePlotted()
    {
        var size = Rational.Parse("1" + new string('0', 400));
        var ratio = (size + Rational.One) / (size + size + Rational.One);

        Assert.Equal(0.5, ratio.ToDouble(), precision: 12);
        Assert.Equal(-0.5, (-ratio).ToDouble(), precision: 12);
    }
}
