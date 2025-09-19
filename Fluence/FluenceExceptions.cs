using System.Text;
using static Fluence.FluenceByteCode;
using static Fluence.FluenceVirtualMachine;

namespace Fluence
{
    /// <summary>
    /// Provides detailed, context-rich information about an error.
    /// </summary>
    internal abstract record ExceptionContext
    {
        /// <summary>
        /// Formats the exception context into a user-friendly, multi-line error message.
        /// </summary>
        /// <returns>A formatted string detailing the error context.</returns>
        internal abstract string Format();
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

            string tokenText = (Token.Text is "\r" or "\n" or "\r\n" or ";\r\n") ? "NewLine" : Token.Text;
            string tokenLiteral = (string)((Token.Literal is (object)"\r" or (object)"\n" or (object)"\r\n") ? "NewLine" : Token.Literal);

            tokenText ??= "Null";
            tokenLiteral ??= "Null";

            stringBuilder.AppendLine($"Last Token scanned <Type, Text, Literal>: <{Token.Type.ToString()}, {tokenText}, {tokenLiteral}>");

            return stringBuilder.ToString();
        }
    }

    internal sealed record RuntimeExceptionContext : ExceptionContext
    {
        internal required string ExceptionMessage { get; init; }
        internal required VMDebugContext DebugContext { get; init; }
        internal required List<StackFrameInfo> StackTraces { get; init; }
        internal required InstructionLine InstructionLine { get; init; }
        internal required FluenceParser Parser { get; init; }

        internal override string Format()
        {
            StringBuilder stringBuilder = new StringBuilder();

            string fileName = null;
            string filepath = null;
            string faultyLine;

            if (Parser.IsMultiFileProject)
            {
                filepath = Parser.CurrentParseState.ProjectFilePaths[InstructionLine.ProjectFileIndex]; ;
                fileName = Path.GetFileName(filepath);
                string[] lines = File.ReadAllLines(filepath);
                faultyLine = lines[InstructionLine.LineInSourceCode - 1];
            }
            else
            {
                faultyLine = Parser.Lexer.SourceCode.Split(Environment.NewLine)[InstructionLine.LineInSourceCode - 1];
            }

            int errorlLineNum = InstructionLine.LineInSourceCode;
            int errorColumnNum = InstructionLine.ColumnInSourceCode;
            int lineNumLen = errorlLineNum.ToString().Length;

            if (errorlLineNum > 0 && faultyLine != null && errorColumnNum > 0)
            {
                stringBuilder.AppendLine($"\nException occured in: {(string.IsNullOrEmpty(fileName) ? "Script" : fileName)}.");

                if (!string.IsNullOrEmpty(filepath))
                {
                    stringBuilder.AppendLine($"Exact path: {filepath}");
                }

                stringBuilder
                    .AppendLine($"RUNTIME ERROR at approximately: line {errorlLineNum}, Column {errorColumnNum}")
                    .AppendLine($"\nMost likely line where the error occured:")
                    .AppendLine($"\n{new string(' ', lineNumLen + 1)}│")
                    .AppendLine($"{new string(' ', lineNumLen + 1)}│")
                    .AppendLine($"{new string(' ', lineNumLen + 1)}│\t {ExceptionMessage}")
                    .AppendLine($"{new string(' ', lineNumLen + 1)}│")
                    .AppendLine($"{errorlLineNum}.│ {faultyLine}")
                    .AppendLine($"{new string(' ', lineNumLen + 1)}│{new string(' ', errorColumnNum - 2)}^")
                    .AppendLine($"{new string('─', lineNumLen + 1)}┴{new string('─', errorColumnNum - lineNumLen)}┴{new string('─', faultyLine.Length)}");
            }

            stringBuilder.AppendLine($"\nState at the moment of the exception:\n")
                .AppendLine($"In Function       :│ \"{Mangler.Demangle(DebugContext.CurrentFunctionName, out _)}\"")
                .Append($"                   │ \n");

            stringBuilder.Append("Local Variables   :│ ");

            if (DebugContext.CurrentLocals.Count > 0)
            {
                int maxKeyLength = DebugContext.CurrentLocals.Keys.Max(key => key.Length);

                bool isFirstLocal = true;
                foreach (KeyValuePair<string, RuntimeValue> local in DebugContext.CurrentLocals)
                {
                    string value = local.Value.ToString();
                    string end = value.Length > 150 ? "...\"" : "\"";
                    string formattedValue = $"\"{value[..Math.Min(150, value.Length)]}{end}";

                    string paddedKey = local.Key.PadRight(maxKeyLength);

                    if (isFirstLocal)
                    {
                        stringBuilder.AppendLine($"{paddedKey} = {formattedValue}");
                        isFirstLocal = false;
                    }
                    else
                    {
                        stringBuilder.AppendLine($"                   │ {paddedKey} = {formattedValue}");
                    }
                }
            }
            else
            {
                stringBuilder.AppendLine("EMPTY");
            }

            stringBuilder.AppendLine("                   │");

            stringBuilder.Append("Operand Stack     :│");

            if (DebugContext.OperandStackSnapshot.Count > 0)
            {
                bool isFirstOperand = true;
                foreach (RuntimeValue item in DebugContext.OperandStackSnapshot)
                {
                    if (isFirstOperand)
                    {
                        stringBuilder.AppendLine($" [{item}]");
                        isFirstOperand = false;
                    }
                    else
                    {
                        stringBuilder.AppendLine($"                  │ [{item}]");
                    }
                }
            }
            else
            {
                stringBuilder.AppendLine(" EMPTY");
            }

            string separator = new string('─', 50);
            stringBuilder.AppendLine(separator);
            stringBuilder.AppendLine("\nLast Virtual Machine Instruction and Function:\n");
            stringBuilder.AppendLine($"IP: {DebugContext.InstructionPointer:D4}   Function: {Mangler.Demangle(DebugContext.CurrentFunctionName, out _)}   Call Stack Depth: {DebugContext.CallStackDepth}");
            stringBuilder.AppendLine($"\nThe Error occured at the following bytecode instruction:\n");
            stringBuilder.AppendLine($"{string.Format("{0,-20} {1,-50} {2,-45} {3,-40} {4, -25}", "TYPE", "LHS", "RHS", "RHS2", "RHS3")}");
            stringBuilder.AppendLine($"{DebugContext.CurrentInstruction}");

            stringBuilder.AppendLine($"\nStack Trace (most recent call last):");

            foreach (StackFrameInfo trace in StackTraces)
            {
                stringBuilder.AppendLine($"\tat {Mangler.Demangle(trace.FunctionName, out _)} ({trace.FileName} : {trace.LineNumber})");
            }

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
                int lineNumLen = LineNum.ToString().Length;
                stringBuilder
                    .AppendLine($"\nException occured in: {(string.IsNullOrEmpty(FileName) ? "Script" : FileName)}.")
                    .AppendLine($"PARSER ERROR at: line {LineNum}, Column {Column}")
                    .AppendLine($"\n{LineNum}.│ {FaultyLine}")
                    .AppendLine($"{new string(' ', lineNumLen + 1)}│{new string(' ', Column - 1)}^")
                    .AppendLine($"{new string('─', lineNumLen + 1)}┴{new string('─', Column - lineNumLen)}┴{new string('─', FaultyLine.Length)}");
            }

            string tokenText = (UnexpectedToken.Text is "\r" or "\n" or "\r\n" or ";\r\n")
                ? "NewLine"
                : UnexpectedToken.ToDisplayString();

            stringBuilder.AppendLine($"Error: Unexpected token '{tokenText}' (Type: {UnexpectedToken.Type}).");

            return stringBuilder.ToString();
        }
    }

    /// <summary>
    /// The base class for all exceptions thrown by the Fluence VM.
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
                if (_context is not RuntimeExceptionContext)
                {
                    stringBuilder.AppendLine($"Exception: {base.Message}");
                }
                return stringBuilder.ToString();
            }
        }
    }

    /// <summary>
    /// Represents an exception that occurs during the execution of a Fluence script by the VM.
    /// </summary>
    public sealed class FluenceRuntimeException : FluenceException
    {
        internal FluenceRuntimeException(string message, RuntimeExceptionContext context)
            : base(message, context) { }
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