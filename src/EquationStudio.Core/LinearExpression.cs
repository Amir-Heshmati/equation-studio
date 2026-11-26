using static EquationStudio.Core.EquationParser;

namespace EquationStudio.Core;

internal static class LinearExpression
{
    public static AffineExpression Normalize(Expression expression)
    {
        var form = Collect(expression, allowY: false);
        return new AffineExpression(form.X, form.Constant);
    }

    public static LinearForm Collect(Expression expression, bool allowY) => expression switch
    {
        Constant constant => new(Rational.Zero, Rational.Zero, constant.Value),
        Variable { Name: 'x' } => new(Rational.One, Rational.Zero, Rational.Zero),
        Variable when allowY => new(Rational.Zero, Rational.One, Rational.Zero),
        Variable => throw new EquationInputException("Use x as the only variable for an equation or function of x."),
        Negation negation => Collect(negation.Inner, allowY).Scale(-Rational.One),
        Binary binary => Combine(binary.Operator, Collect(binary.Left, allowY), Collect(binary.Right, allowY)),
        _ => throw new InvalidOperationException("Unknown expression node.")
    };

    private static LinearForm Combine(char operation, LinearForm left, LinearForm right) => operation switch
    {
        '+' => new(left.X + right.X, left.Y + right.Y, left.Constant + right.Constant),
        '-' => new(left.X - right.X, left.Y - right.Y, left.Constant - right.Constant),
        '*' when left.IsConstant => right.Scale(left.Constant),
        '*' when right.IsConstant => left.Scale(right.Constant),
        '/' when !right.IsConstant => throw new EquationInputException("The divisor must be a constant."),
        '/' when right.Constant == Rational.Zero => throw new EquationInputException("Division by zero is undefined."),
        '/' => left.Scale(Rational.One / right.Constant),
        _ => throw new EquationInputException("For a linear expression, multiply only by a constant.")
    };

    internal sealed record LinearForm(Rational X, Rational Y, Rational Constant)
    {
        public bool IsConstant => X == Rational.Zero && Y == Rational.Zero;

        public LinearForm Scale(Rational factor) => new(X * factor, Y * factor, Constant * factor);
    }
}
