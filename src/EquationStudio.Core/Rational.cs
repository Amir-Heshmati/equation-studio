using System.Globalization;
using System.Numerics;

namespace EquationStudio.Core;

public sealed record Rational
{
    public static Rational Zero { get; } = new(0, 1);
    public static Rational One { get; } = new(1, 1);

    public BigInteger Numerator { get; }
    public BigInteger Denominator { get; }

    private Rational(BigInteger numerator, BigInteger denominator)
    {
        if (denominator.IsZero)
        {
            throw new DivideByZeroException();
        }

        // A positive denominator and reduced terms give each value a single representation.
        var divisor = BigInteger.GreatestCommonDivisor(numerator, denominator) * denominator.Sign;
        Numerator = numerator / divisor;
        Denominator = denominator / divisor;
    }

    public static Rational Parse(string text)
    {
        int point = text.IndexOf('.');
        if (point < 0)
        {
            return new Rational(BigInteger.Parse(text, CultureInfo.InvariantCulture), 1);
        }

        var numerator = BigInteger.Parse(text.Remove(point, 1), CultureInfo.InvariantCulture);
        return new Rational(numerator, BigInteger.Pow(10, text.Length - point - 1));
    }

    public double ToDouble()
    {
        // Scale the numerator and denominator independently so large exact terms don't become infinity / infinity.
        int numeratorShift = (int)Math.Max(0, BigInteger.Abs(Numerator).GetBitLength() - 54);
        int denominatorShift = (int)Math.Max(0, Denominator.GetBitLength() - 54);
        double quotient = (double)(BigInteger.Abs(Numerator) >> numeratorShift) / (double)(Denominator >> denominatorShift);
        return Numerator.Sign * Math.ScaleB(quotient, numeratorShift - denominatorShift);
    }

    public override string ToString() => Denominator.IsOne
        ? Numerator.ToString(CultureInfo.InvariantCulture)
        : FormattableString.Invariant($"{Numerator}/{Denominator}");

    public static Rational operator +(Rational left, Rational right) =>
        new(left.Numerator * right.Denominator + right.Numerator * left.Denominator, left.Denominator * right.Denominator);

    public static Rational operator -(Rational left, Rational right) => left + -right;

    public static Rational operator -(Rational value) => new(-value.Numerator, value.Denominator);

    public static Rational operator *(Rational left, Rational right) =>
        new(left.Numerator * right.Numerator, left.Denominator * right.Denominator);

    public static Rational operator /(Rational left, Rational right) =>
        new(left.Numerator * right.Denominator, left.Denominator * right.Numerator);
}
