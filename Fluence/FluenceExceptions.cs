using System.Text;

namespace Fluence
{
    /// <summary>
    /// Provides detailed, context-rich information about a compiler error.
    /// </summary>
    internal abstract record ExceptionContext
    {
        /// <summary>
        /// Formats the exception context into a user-friendly, multi-line error message.
        /// </summary>
        /// <returns>A formatted string detailing the error context.</returns>
        internal abstract string Format();
    }

    internal sealed record RuntimeExceptionContext : ExceptionContext
    {
        internal override string Format()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Provides context for an error that occurred during the lexing phase.
    /// </summary>
    internal sealed record LexerExceptionContext : ExceptionContext
    {
        /// <summary>The line number where the error occurred.</summary>
        internal int LineNum { get; init; }

        /// <summary>The column number where the error occurred.</summary>
        internal int Column { get; init; }

        /// <summary>
        /// The Fluence script file where the error occured.
        /// </summary>
        internal required string FileName { get; init; }

        /// <summary>
        /// Last parsed <see cref="Fluence.Token"/>, can be null or <see cref="Token.TokenType.UNKNOWN"/>
        /// </summary>
        internal Token Token { get; init; }

        /// <summary>The source code of the line where the error occurred.</summary>
        internal required string FaultyLine { get; init; }

        internal override string Format()
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (LineNum > 0 && FaultyLine != null && Column > 0)
            {
                stringBuilder
                    .AppendLine($"\nException occured in: {(string.IsNullOrEmpty(FileName) ? "Script" : FileName)}.")
                    .AppendLine($"Exception at line {LineNum}, Column {Column}")
                    .AppendLine($"{LineNum}. {FaultyLine}")
                    .AppendLine($"{new string(' ', Column + 1)}^");
            }

            string tokenText = (Token.Text == "\r" || Token.Text == "\n" || Token.Text == "\r\n" || Token.Text == ";\r\n") ? "NewLine" : Token.Text;
            string tokenLiteral = (string)((Token.Literal is (object)"\r" or (object)"\n" or (object)"\r\n") ? "NewLine" : Token.Literal);

            tokenText ??= "Null";
            tokenLiteral ??= "Null";

            stringBuilder.AppendLine($"Last Token scanned <Type, Text, Literal>: <{Token.Type.ToString()}, {tokenText}, {tokenLiteral}>");

            return stringBuilder.ToString();
        }
    }

    /// <summary>
    /// Provides context for an error that occurred during the parsing phase.
    /// </summary>
    internal sealed record ParserExceptionContext : ExceptionContext
    {
        /// <summary>The line number where the error occurred.</summary>
        internal int LineNum { get; init; }

        /// <summary>The column number where the error occurred.</summary>
        internal int Column { get; init; }

        /// <summary>
        /// The Fluence script file where the error occured.
        /// </summary>
        internal required string FileName { get; init; }

        /// <summary>The source code of the line where the error occurred.</summary>
        internal string FaultyLine { get; init; }

        /// <summary>Gets the token that the parser could not process.</summary>
        internal required Token UnexpectedToken { get; init; }

        internal override string Format()
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (LineNum > 0 && FaultyLine != null && Column > 0)
            {
                string linePrefix = $"{LineNum}.";
                stringBuilder
                    .AppendLine($"\nException occured in: {(string.IsNullOrEmpty(FileName) ? "Script" : FileName)}.")
                    .AppendLine($"Parser Error at line {LineNum}, Column {Column}")
                    .AppendLine(linePrefix + FaultyLine);

                stringBuilder.AppendLine(new string(' ', linePrefix.Length + Column - 1) + "^");
            }

            string tokenText = (UnexpectedToken.Text is "\r" or "\n")
                ? "NewLine"
                : UnexpectedToken.ToDisplayString();

            stringBuilder.AppendLine($"Error: Unexpected token '{tokenText}' (Type: {UnexpectedToken.Type}).");

            return stringBuilder.ToString();
        }
    }

    /// <summary>
    /// The base class for all exceptions thrown by the Fluence Interpreter.
    /// </summary>
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

    /// <summary>
    /// Represents an exception that occurs during the execution of a Fluence script by the VM.
    /// </summary>
    public sealed class FluenceRuntimeException : FluenceException
    {
        public FluenceRuntimeException(string message) : base(message) { }
    }

    /// <summary>
    /// Represents an error that occurs during lexical analysis.
    /// </summary>
    public sealed class FluenceLexerException : FluenceException
    {
        internal FluenceLexerException(string message, LexerExceptionContext context)
            : base(message, context) { }
    }

    /// <summary>
    /// Represents an error that occurs during parsing.
    /// </summary>
    public sealed class FluenceParserException : FluenceException
    {
        internal FluenceParserException(string message, ParserExceptionContext context)
            : base(message, context) { }
    }
}