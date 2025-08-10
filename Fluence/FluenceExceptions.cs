using System.Text;

namespace Fluence
{
    internal abstract record ExceptionContext
    {
        internal abstract string Format();
    }

    internal record LexerExceptionContext : ExceptionContext
    {
        internal int LineNum { get; init; }
        internal int Column { get; init; }
        internal Token Token { get; init; }
        // Line number where error occured and the string of that line is better than just 
        // the number.
        internal required string FaultyLine { get; init; }

        internal override string Format()
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (LineNum > 0 && FaultyLine != null && Column > 0)
            {
                stringBuilder
                    .AppendLine($"Exception at line {LineNum}, Column {Column}")
                    .AppendLine($"{LineNum}. {FaultyLine}")
                    .AppendLine($"{new string(' ', Column + 1)}^");
            }

            if (Token != null)
            {
                string tokenText = (Token.Text == "\r" || Token.Text == "\n" || Token.Text == "\r\n")
                    ? "NewLine" : Token.Text;
                string tokenLiteral = (string)((Token.Literal == "\r" || Token.Literal == "\n" || Token.Literal == "\r\n")
                    ? "NewLine" : Token.Literal);

                tokenLiteral ??= "Null";

                stringBuilder.AppendLine($"Last Token scanned <Type, Text, Literal>: <{Token.Type.ToString()}, {tokenText}, {tokenLiteral}>");
            }

            return stringBuilder.ToString();
        }
    }

    internal record ParserExceptionContext : ExceptionContext
    {
        // Parser and Lexer sort of work at the same time, so these values can be obtained from lexer,
        // With some degree of accuracy.
        internal int LineNum { get; init; }
        internal int Column { get; init; }
        internal string FaultyLine { get; init; }

        // Parser-specific details
        internal required Token UnexpectedToken { get; init; }
        internal required string ExpectedDescription { get; init; }

        internal override string Format()
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (LineNum > 0 && FaultyLine != null && Column > 0)
            {
                string linePrefix = $"{LineNum}. \t";
                stringBuilder
                    .AppendLine($"Parser Error at line {LineNum}, Column {Column}")
                    .AppendLine(linePrefix + FaultyLine);

                stringBuilder.AppendLine(new string(' ', linePrefix.Length + Column - 1) + "^");
            }

            if (UnexpectedToken != null)
            {
                string tokenText = (UnexpectedToken.Text == "\r" || UnexpectedToken.Text == "\n")
                    ? "NewLine"
                    : UnexpectedToken.Text;

                stringBuilder.AppendLine($"Error: Unexpected token '{tokenText}' (Type: {UnexpectedToken.Type}).");
            }

            if (!string.IsNullOrEmpty(ExpectedDescription))
            {
                stringBuilder.AppendLine($"Expected: {ExpectedDescription}");
            }

            return stringBuilder.ToString();
        }
    }

    public class FluenceException : Exception
    {
        internal readonly ExceptionContext? _context;

        public FluenceException(string message) : base(message)
        {
            _context = null;
        }

        internal FluenceException(string message, ExceptionContext context) : base(message)
        {
            _context = context;
        }

        public override string Message
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(_context?.Format());
                stringBuilder.AppendLine($"Exception: {base.Message}");
                return stringBuilder.ToString();
            }
        }
    }

    public class FluenceLexerException : FluenceException
    {
        internal FluenceLexerException(string message, LexerExceptionContext context)
            : base(message, context) { }
    }

    public class FluenceParserException : FluenceException
    {
        internal FluenceParserException(string message, ParserExceptionContext context)
            : base(message, context) { }
    }
}