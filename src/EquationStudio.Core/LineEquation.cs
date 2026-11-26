namespace EquationStudio.Core;

public enum LineEquationKind { Line, NoPoints, AllPoints }

public sealed record LineEquation(Rational A, Rational B, Rational C)
{
    // Standard form Ax + By = C also represents vertical lines and degenerate equations.
    public LineEquationKind Kind => A != Rational.Zero || B != Rational.Zero
        ? LineEquationKind.Line
        : C == Rational.Zero ? LineEquationKind.AllPoints : LineEquationKind.NoPoints;

    public bool IsVertical => Kind == LineEquationKind.Line && B == Rational.Zero;
    public bool IsHorizontal => Kind == LineEquationKind.Line && A == Rational.Zero;
    public Rational? Slope => B == Rational.Zero ? null : -A / B;
    public Rational? XIntercept => A == Rational.Zero ? null : C / A;
    public Rational? YIntercept => B == Rational.Zero ? null : C / B;

    public string DisplayEquation => Kind switch
    {
        LineEquationKind.AllPoints or LineEquationKind.NoPoints => $"0 = {C}",
        _ when IsVertical => $"x = {XIntercept}",
        _ => $"y = {new AffineExpression(Slope!, YIntercept!)}"
    };

    public static bool MatchesPlotInput(string input)
    {
        int equals = FindEquals(input);
        return equals < 0 || IsFunctionLabel(input[..equals]) ||
               input.AsSpan().IndexOfAny("yYｙＹ") >= 0;
    }

    public static LineEquation Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length > EquationParser.MaximumLength)
        {
            throw new EquationInputException($"Enter an expression or equation up to {EquationParser.MaximumLength} characters.");
        }

        int equals = FindEquals(input);
        if (equals < 0)
        {
            return FromFunction(LinearFunction.Parse(input));
        }

        if (IsFunctionLabel(input[..equals]))
        {
            return FromFunction(LinearExpression.Normalize(EquationParser.ParseFunction(input[(equals + 1)..], allowLabel: false)));
        }

        var (left, right, _) = EquationParser.Parse(input);
        var leftForm = LinearExpression.Collect(left, allowY: true);
        var rightForm = LinearExpression.Collect(right, allowY: true);
        return new LineEquation(leftForm.X - rightForm.X, leftForm.Y - rightForm.Y, rightForm.Constant - leftForm.Constant);
    }

    public static LineEquation FromFunction(AffineExpression function) =>
        new(-function.Slope, Rational.One, function.Intercept);

    private static int FindEquals(string input) => input.AsSpan().IndexOfAny('=', '＝');

    private static bool IsFunctionLabel(string text) =>
        string.Concat(text.Where(character => !char.IsWhiteSpace(character))).Equals("f(x)", StringComparison.OrdinalIgnoreCase);
}
