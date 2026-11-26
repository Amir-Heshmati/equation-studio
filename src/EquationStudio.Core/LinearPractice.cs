using System.Globalization;

namespace EquationStudio.Core;

public enum PracticeLevel { BothSides, Parentheses, Fractions }

public sealed record PracticeExercise(string Equation, int ExpectedRoot);

public static class LinearPractice
{
    public static PracticeExercise Create(PracticeLevel level, Random random)
    {
        int root = random.Next(-9, 10);
        int leftSlope = random.Next(2, 8);
        int rightSlope = random.Next(1, leftSlope);
        int offset = random.Next(1, 7);
        int divisor = random.Next(2, 5);

        // Choosing a root first lets each exercise have a unique answer without filtering random equations.
        string equation = level switch
        {
            PracticeLevel.BothSides => $"{Number(leftSlope)}x + {Number(offset)} = {Number(rightSlope)}x{Signed((leftSlope - rightSlope) * root + offset)}",
            PracticeLevel.Parentheses => $"{Number(leftSlope)}(x + {Number(offset)}) = {Number(rightSlope)}x{Signed((leftSlope - rightSlope) * root + leftSlope * offset)}",
            PracticeLevel.Fractions => $"({Number(leftSlope * divisor)}x + {Number(offset)})/{Number(divisor)} = {Number(rightSlope)}x + ({Number((leftSlope - rightSlope) * root * divisor + offset)}/{Number(divisor)})",
            _ => throw new ArgumentOutOfRangeException(nameof(level))
        };

        return new PracticeExercise(equation, root);
    }

    private static string Signed(int value) => value < 0 ? $" - {Number(-value)}" : $" + {Number(value)}";

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
