using EquationStudio.Core;

namespace EquationStudio.Tests;

public sealed class LinearFunctionTests
{
    [Theory]
    [InlineData("2x + 5", "2", "5", "7")]
    [InlineData("Y = 2x + 5", "2", "5", "7")]
    [InlineData("-3(x - 2)", "-3", "6", "3")]
    [InlineData("5", "0", "5", "5")]
    [InlineData("x / 2", "1/2", "0", "1/2")]
    public void ParsesAFunctionAndEvaluatesAPoint(string input, string slope, string intercept, string atOne)
    {
        Assert.True(LinearFunction.MatchesInput(input));
        var function = LinearFunction.Parse(input);

        Assert.Equal(slope, function.Slope.ToString());
        Assert.Equal(intercept, function.Intercept.ToString());
        Assert.Equal(atOne, function.At(Rational.One).ToString());
    }

    [Theory]
    [InlineData("2x + 5 = 9")]
    [InlineData("x = 3")]
    public void KeepsEquationsOnTheSolvePath(string input) =>
        Assert.False(LinearFunction.MatchesInput(input));

    [Theory]
    [InlineData("x*x")]
    [InlineData("y = 1 / x")]
    [InlineData("y = 2x = 4")]
    [InlineData("y")]
    [InlineData("y =")]
    public void RejectsUnsupportedFunctions(string input) =>
        Assert.Throws<EquationInputException>(() => LinearFunction.Parse(input));
}
