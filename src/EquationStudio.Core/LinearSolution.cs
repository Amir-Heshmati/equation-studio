namespace EquationStudio.Core;

public enum SolutionKind { Unique, None, Infinite }

public enum StepOperation { Simplify, SubtractVariable, SubtractConstant, Divide }

public sealed record SolutionStep(StepOperation Operation, string Before, string After, Rational? Operand = null);

public sealed record LinearSolution(
    SolutionKind Kind,
    Rational? Root,
    IReadOnlyList<SolutionStep> Steps,
    AffineExpression Left,
    AffineExpression Right);

public sealed record AffineExpression(Rational Slope, Rational Intercept)
{
    public Rational At(Rational x) => Slope * x + Intercept;

    public override string ToString()
    {
        if (Slope == Rational.Zero)
        {
            return Intercept.ToString();
        }

        string term = Slope == Rational.One ? "x" : Slope == -Rational.One ? "-x" : $"{Slope}x";
        if (Intercept == Rational.Zero)
        {
            return term;
        }

        return Intercept.Numerator.Sign < 0 ? $"{term} - {-Intercept}" : $"{term} + {Intercept}";
    }
}

public sealed class EquationInputException(string message) : Exception(message);
