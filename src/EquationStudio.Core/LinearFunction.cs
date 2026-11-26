namespace EquationStudio.Core;

public static class LinearFunction
{
    public static bool MatchesInput(string input)
    {
        int equals = input.IndexOf('=');
        return equals < 0 || input.AsSpan(0, equals).Trim().Equals("y", StringComparison.OrdinalIgnoreCase);
    }

    public static AffineExpression Parse(string input) =>
        LinearExpression.Normalize(EquationParser.ParseFunction(input));
}
