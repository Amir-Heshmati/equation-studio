namespace EquationStudio.Core;

internal sealed class EquationParser(string input)
{
    internal const int MaximumLength = 256;
    private const int MaximumDepth = 24;
    private int position;

    public static (Expression Left, Expression Right, string Text) Parse(string input)
    {
        var parser = Create(input);
        Expression left = parser.ParseSum(0);
        if (!parser.Take('='))
        {
            throw new EquationInputException("Use one equals sign to separate the two sides.");
        }

        Expression right = parser.ParseSum(0);
        parser.RequireEnd("Use only numbers, x, parentheses, and +, −, ×, or ÷.");
        return (left, right, parser.InputText);
    }

    public static Expression ParseFunction(string input, bool allowLabel = true)
    {
        var parser = Create(input);
        if (allowLabel && parser.Take('y') && !parser.Take('='))
        {
            throw new EquationInputException("Write y = followed by an expression in x.");
        }

        Expression expression = parser.ParseSum(0);
        parser.RequireEnd("Enter one expression in x, optionally starting with y =.");
        return expression;
    }

    private string InputText => input;

    private static EquationParser Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length > MaximumLength)
        {
            throw new EquationInputException($"Enter an equation between 1 and {MaximumLength} characters.");
        }

        return new EquationParser(new string(input.Trim().Select(NormalizeCharacter).ToArray()));
    }

    private void RequireEnd(string error)
    {
        Peek();
        if (position != input.Length)
        {
            throw new EquationInputException(error);
        }
    }

    private static char NormalizeCharacter(char character) => character switch
    {
        >= '۰' and <= '۹' => (char)('0' + character - '۰'),
        >= '٠' and <= '٩' => (char)('0' + character - '٠'),
        >= '０' and <= '９' => (char)('0' + character - '０'),
        '−' or '－' => '-',
        '＋' => '+',
        '×' or '·' or '⋅' or '＊' => '*',
        '÷' or '⁄' or '／' => '/',
        '٫' or '．' => '.',
        '＝' => '=',
        'X' or 'Ｘ' or 'ｘ' => 'x',
        'Y' or 'Ｙ' or 'ｙ' => 'y',
        _ => character
    };

    private Expression ParseSum(int depth)
    {
        Expression term = ParseProduct(depth);
        while (Peek() is '+' or '-')
        {
            char operation = input[position++];
            term = new Binary(operation, term, ParseProduct(depth));
        }

        return term;
    }

    private Expression ParseProduct(int depth)
    {
        Expression factor = ParseFactor(depth);
        while (Peek() is '*' or '/' or 'x' or 'y' or '(' or '[' or '{')
        {
            char next = Peek();
            // Implicit products such as 2x and 3(x+1) share the precedence of explicit products.
            char operation = next is '*' or '/' ? input[position++] : '*';
            factor = new Binary(operation, factor, ParseFactor(depth));
        }

        return factor;
    }

    private Expression ParseFactor(int depth)
    {
        if (depth > MaximumDepth)
        {
            throw new EquationInputException("This expression has too many nested operations.");
        }

        if (Take('+'))
        {
            return ParseFactor(depth + 1);
        }

        if (Take('-'))
        {
            return new Negation(ParseFactor(depth + 1));
        }

        char next = Peek();
        if (next is '(' or '[' or '{')
        {
            position++;
            Expression inner = ParseSum(depth + 1);
            char closing = next switch { '(' => ')', '[' => ']', _ => '}' };
            if (!Take(closing))
            {
                throw new EquationInputException($"Close each {next} with a matching {closing}.");
            }

            return inner;
        }

        if (next is 'x' or 'y')
        {
            position++;
            return new Variable(next);
        }

        Peek();
        int start = position;
        while (position < input.Length && char.IsAsciiDigit(input[position]))
        {
            position++;
        }

        int digitCount = position - start;
        if (position < input.Length && input[position] == '.')
        {
            position++;
            int fractionStart = position;
            while (position < input.Length && char.IsAsciiDigit(input[position]))
            {
                position++;
            }

            digitCount += position - fractionStart;
        }

        if (digitCount is < 1 or > 24)
        {
            throw new EquationInputException("Enter x, a parenthesized expression, or a number with up to 24 digits.");
        }

        return new Constant(Rational.Parse(input[start..position]));
    }

    private char Peek()
    {
        while (position < input.Length && char.IsWhiteSpace(input[position]))
        {
            position++;
        }

        return position == input.Length ? '\0' : input[position];
    }

    private bool Take(char expected)
    {
        if (Peek() != expected)
        {
            return false;
        }

        position++;
        return true;
    }

    internal abstract record Expression;
    internal sealed record Constant(Rational Value) : Expression;
    internal sealed record Variable(char Name) : Expression;
    internal sealed record Negation(Expression Inner) : Expression;
    internal sealed record Binary(char Operator, Expression Left, Expression Right) : Expression;
}
