using System.Text;

namespace Fluence
{
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
                int lineNumLen = LineNum.ToString().Length;
                stringBuilder
                    .AppendLine($"\nException occured in: {(string.IsNullOrEmpty(FileName) ? "Script" : FileName)}.")
                    .AppendLine($"PARSER ERROR at: line {LineNum}, Column {Column}")
                    .AppendLine($"\n{LineNum}.│ {FaultyLine}")
                    .AppendLine($"{new string(' ', lineNumLen + 1)}│{new string(' ', Column - lineNumLen)}^")
                    .AppendLine($"{new string('─', lineNumLen + 1)}┴{new string('─', Column - lineNumLen)}┴{new string('─', FaultyLine.Length)}");
            }

            string tokenText = (UnexpectedToken.Text is "\r" or "\n" or "\r\n" or ";\r\n")
                ? "NewLine"
                : UnexpectedToken.ToDisplayString();

            stringBuilder.AppendLine($"Error: Unexpected token '{tokenText}' (Type: {UnexpectedToken.Type}).");

            return stringBuilder.ToString();
        }
    }
}