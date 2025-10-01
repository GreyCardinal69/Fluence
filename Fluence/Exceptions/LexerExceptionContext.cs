using System.Text;

namespace Fluence.Fluence.Exceptions
{
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
        /// The Fluence script file where the error occured, if given code as a string from an application, returns "script".
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
                int lineNumLen = LineNum.ToString().Length;
                stringBuilder
                    .AppendLine($"\nException occured in: {(string.IsNullOrEmpty(FileName) ? "Script" : FileName)}.")
                    .AppendLine($"LEXER ERROR at: line {LineNum}, Column {Column}")
                    .AppendLine($"\n{LineNum}.│ {FaultyLine}")
                    .AppendLine($"{new string(' ', lineNumLen + 1)}│{new string(' ', Column - 1)}^")
                    .AppendLine($"{new string('─', lineNumLen + 1)}┴{new string('─', Column - lineNumLen)}┴{new string('─', FaultyLine.Length)}");
            }

            string tokenText = Token.Text is "\r" or "\n" or "\r\n" or ";\r\n" ? "NewLine" : Token.Text;
            string tokenLiteral = (string)(Token.Literal is (object)"\r" or (object)"\n" or (object)"\r\n" ? "NewLine" : Token.Literal);

            tokenText ??= "Null";
            tokenLiteral ??= "Null";

            stringBuilder.AppendLine($"Last Token scanned <Type, Text, Literal>: <{Token.Type.ToString()}, {tokenText}, {tokenLiteral}>");

            return stringBuilder.ToString();
        }
    }
}