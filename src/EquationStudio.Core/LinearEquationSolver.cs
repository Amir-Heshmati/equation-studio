using static EquationStudio.Core.EquationParser;

namespace EquationStudio.Core;

public static class LinearEquationSolver
{
    public const int MaximumInputLength = EquationParser.MaximumLength;

    public static LinearSolution Solve(string equation)
    {
        var (leftExpression, rightExpression, text) = Parse(equation);
        AffineExpression left = LinearExpression.Normalize(leftExpression);
        AffineExpression right = LinearExpression.Normalize(rightExpression);
        var coefficient = left.Slope - right.Slope;
        var constant = right.Intercept - left.Intercept;
        var steps = new List<SolutionStep>();
        string current = text;

        AddStep(StepOperation.Simplify, Format(left, right));
        AddStep(StepOperation.SubtractVariable, Format(new(coefficient, left.Intercept), new(Rational.Zero, right.Intercept)), right.Slope);
        AddStep(StepOperation.SubtractConstant, Format(new(coefficient, Rational.Zero), new(Rational.Zero, constant)), left.Intercept);

        if (coefficient == Rational.Zero)
        {
            var kind = constant == Rational.Zero ? SolutionKind.Infinite : SolutionKind.None;
            return new LinearSolution(kind, null, steps.AsReadOnly(), left, right);
        }

        var root = constant / coefficient;
        AddStep(StepOperation.Divide, $"x = {root}", coefficient);
        return new LinearSolution(SolutionKind.Unique, root, steps.AsReadOnly(), left, right);

        void AddStep(StepOperation operation, string after, Rational? operand = null)
        {
            if (operation == StepOperation.Simplify
                ? string.Concat(current.Where(character => !char.IsWhiteSpace(character))) ==
                  string.Concat(after.Where(character => !char.IsWhiteSpace(character)))
                : current == after)
            {
                current = steps.Count == 0 ? after : current;
                return;
            }

            steps.Add(new SolutionStep(operation, current, after, operand));
            current = after;
        }
    }

    private static string Format(AffineExpression left, AffineExpression right) => $"{left} = {right}";

}
