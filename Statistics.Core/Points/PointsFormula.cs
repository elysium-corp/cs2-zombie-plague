using System.Diagnostics;
using System.Globalization;
using Statistics.Core.Data;

namespace Statistics.Core.Points;

internal sealed class PointsFormula
{
    private const int MaxFormulaLength = 2_048;

    private readonly FormulaNode _root;

    private PointsFormula(string source, FormulaNode root)
    {
        Source = source;
        _root = root;
    }

    public string Source { get; }

    public static PointsFormula Parse(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new PointsFormulaException("Points formula cannot be empty.");
        }

        if (source.Length > MaxFormulaLength)
        {
            throw new PointsFormulaException(
                $"Points formula cannot be longer than {MaxFormulaLength} characters."
            );
        }

        var parser = new Parser(source);

        return new PointsFormula(source, parser.Parse());
    }

    public decimal Evaluate(RoundPointsContext context)
    {
        try
        {
            return _root.Evaluate(context);
        }
        catch (PointsFormulaException)
        {
            throw;
        }
        catch (OverflowException exception)
        {
            throw new PointsFormulaException(
                $"Points formula result is outside the supported decimal range: {exception.Message}"
            );
        }
    }

    private enum BinaryOperator
    {
        Add,
        Subtract,
        Multiply,
        Divide
    }

    private enum UnaryOperator
    {
        Plus,
        Minus
    }

    private enum PointsVariable
    {
        ZombiesKilled,
        InfectionsMade,
        TimesInfected,
        Deaths,
        HumanWin,
        ZombieWin,
        BestKillStreak,
        BestInfectionStreak
    }

    private abstract class FormulaNode
    {
        public abstract decimal Evaluate(RoundPointsContext context);
    }

    private sealed class NumberNode(decimal value) : FormulaNode
    {
        public override decimal Evaluate(RoundPointsContext context)
        {
            _ = context;

            return value;
        }
    }

    private sealed class VariableNode(PointsVariable variable) : FormulaNode
    {
        public override decimal Evaluate(RoundPointsContext context)
        {
            return variable switch
            {
                PointsVariable.ZombiesKilled => context.ZombiesKilled,
                PointsVariable.InfectionsMade => context.InfectionsMade,
                PointsVariable.TimesInfected => context.TimesInfected,
                PointsVariable.Deaths => context.Deaths,
                PointsVariable.HumanWin => context.HumanWin ? 1 : 0,
                PointsVariable.ZombieWin => context.ZombieWin ? 1 : 0,
                PointsVariable.BestKillStreak => context.BestKillStreak,
                PointsVariable.BestInfectionStreak => context.BestInfectionStreak,
                _ => throw new UnreachableException()
            };
        }
    }

    private sealed class UnaryNode(UnaryOperator @operator, FormulaNode operand) : FormulaNode
    {
        public override decimal Evaluate(RoundPointsContext context)
        {
            var value = operand.Evaluate(context);

            return @operator switch
            {
                UnaryOperator.Plus => value,
                UnaryOperator.Minus => -value,
                _ => throw new UnreachableException()
            };
        }
    }

    private sealed class BinaryNode(
        BinaryOperator @operator,
        FormulaNode left,
        FormulaNode right
    ) : FormulaNode
    {
        public override decimal Evaluate(RoundPointsContext context)
        {
            var leftValue = left.Evaluate(context);
            var rightValue = right.Evaluate(context);

            return @operator switch
            {
                BinaryOperator.Add => leftValue + rightValue,
                BinaryOperator.Subtract => leftValue - rightValue,
                BinaryOperator.Multiply => leftValue * rightValue,
                BinaryOperator.Divide when rightValue != 0 => leftValue / rightValue,
                BinaryOperator.Divide => throw new PointsFormulaException(
                    "Points formula attempted to divide by zero."
                ),
                _ => throw new UnreachableException()
            };
        }
    }

    private sealed class Parser(string source)
    {
        private int _position;

        public FormulaNode Parse()
        {
            var result = ParseExpression();

            SkipWhitespace();

            if (!IsAtEnd)
            {
                throw Error($"Unexpected character '{Current}'.");
            }

            return result;
        }

        private FormulaNode ParseExpression()
        {
            var left = ParseTerm();

            while (true)
            {
                SkipWhitespace();

                if (Match('+'))
                {
                    left = new BinaryNode(BinaryOperator.Add, left, ParseTerm());
                }
                else if (Match('-'))
                {
                    left = new BinaryNode(BinaryOperator.Subtract, left, ParseTerm());
                }
                else
                {
                    return left;
                }
            }
        }

        private FormulaNode ParseTerm()
        {
            var left = ParseUnary();

            while (true)
            {
                SkipWhitespace();

                if (Match('*'))
                {
                    left = new BinaryNode(BinaryOperator.Multiply, left, ParseUnary());
                }
                else if (Match('/'))
                {
                    left = new BinaryNode(BinaryOperator.Divide, left, ParseUnary());
                }
                else
                {
                    return left;
                }
            }
        }

        private FormulaNode ParseUnary()
        {
            SkipWhitespace();

            if (Match('+'))
            {
                return new UnaryNode(UnaryOperator.Plus, ParseUnary());
            }

            if (Match('-'))
            {
                return new UnaryNode(UnaryOperator.Minus, ParseUnary());
            }

            return ParsePrimary();
        }

        private FormulaNode ParsePrimary()
        {
            SkipWhitespace();

            if (Match('('))
            {
                var expression = ParseExpression();

                SkipWhitespace();

                if (!Match(')'))
                {
                    throw Error("Expected closing parenthesis.");
                }

                return expression;
            }

            if (!IsAtEnd && (char.IsAsciiDigit(Current) || Current == '.'))
            {
                return ParseNumber();
            }

            if (!IsAtEnd && IsIdentifierStart(Current))
            {
                return ParseVariable();
            }

            throw Error(IsAtEnd
                ? "Unexpected end of formula."
                : $"Unexpected character '{Current}'."
            );
        }

        private FormulaNode ParseNumber()
        {
            var start = _position;
            var hasDecimalPoint = false;

            while (!IsAtEnd)
            {
                if (char.IsAsciiDigit(Current))
                {
                    _position++;
                    continue;
                }

                if (Current == '.' && !hasDecimalPoint)
                {
                    hasDecimalPoint = true;
                    _position++;
                    continue;
                }

                break;
            }

            var valueSource = source.AsSpan(start, _position - start);

            if (!decimal.TryParse(
                    valueSource,
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var value
                ))
            {
                throw Error($"Invalid number '{valueSource.ToString()}'.", start);
            }

            return new NumberNode(value);
        }

        private FormulaNode ParseVariable()
        {
            var start = _position++;

            while (!IsAtEnd && IsIdentifierPart(Current))
            {
                _position++;
            }

            var name = source[start.._position];
            var variable = name switch
            {
                "zombies_killed" => PointsVariable.ZombiesKilled,
                "infections_made" => PointsVariable.InfectionsMade,
                "times_infected" => PointsVariable.TimesInfected,
                "deaths" => PointsVariable.Deaths,
                "human_win" => PointsVariable.HumanWin,
                "zombie_win" => PointsVariable.ZombieWin,
                "best_kill_streak" => PointsVariable.BestKillStreak,
                "best_infection_streak" => PointsVariable.BestInfectionStreak,
                _ => throw Error($"Unknown points variable '{name}'.", start)
            };

            return new VariableNode(variable);
        }

        private void SkipWhitespace()
        {
            while (!IsAtEnd && char.IsWhiteSpace(Current))
            {
                _position++;
            }
        }

        private bool Match(char value)
        {
            if (IsAtEnd || Current != value)
            {
                return false;
            }

            _position++;

            return true;
        }

        private PointsFormulaException Error(string message, int? position = null)
        {
            return new PointsFormulaException(
                $"{message} Position: {position ?? _position}."
            );
        }

        private bool IsAtEnd => _position >= source.Length;

        private char Current => source[_position];

        private static bool IsIdentifierStart(char value)
        {
            return char.IsAsciiLetter(value) || value == '_';
        }

        private static bool IsIdentifierPart(char value)
        {
            return IsIdentifierStart(value) || char.IsAsciiDigit(value);
        }
    }
}
