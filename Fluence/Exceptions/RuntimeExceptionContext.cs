using Fluence.RuntimeTypes;
using System.Text;
using static Fluence.FluenceByteCode;
using static Fluence.FluenceVirtualMachine;

namespace Fluence
{
    internal sealed record RuntimeExceptionContext : ExceptionContext
    {
        internal string ExceptionMessage { get; set; }
        internal required VMDebugContext DebugContext { get; init; }
        internal required List<StackFrameInfo> StackTraces { get; init; }
        internal required InstructionLine InstructionLine { get; init; }
        internal required FluenceParser Parser { get; init; }
        internal required RuntimeExceptionType ExceptionType { get; init; }

        private void ElaborateOnUndefinedFunction(FluenceScope scope, StringBuilder stringBuilder, out bool foundMatch)
        {
            string undefinedVariable = ExceptionMessage.Split('\'')[1];
            Mangler.Demangle(undefinedVariable, out int deMangledArity);
            string deMangledVar = Mangler.Demangle(undefinedVariable);

            int errorlLineNum = InstructionLine.LineInSourceCode;
            int lineNumLen = errorlLineNum.ToString().Length;
            foundMatch = false;
            string leftPad = new string(' ', lineNumLen + 1);

            foreach (Symbol symbol in scope.Symbols.Values)
            {
                if (symbol is FunctionSymbol func)
                {
                    string deMangledFunc = Mangler.Demangle(func.Name);

                    if (string.Equals(deMangledFunc, deMangledVar, StringComparison.Ordinal) && func.Arity != deMangledArity)
                    {
                        if (!foundMatch)
                        {
                            foundMatch = true;
                            stringBuilder.AppendLine($"Runtime Error: Function \"{deMangledFunc}\" does not accept {deMangledArity} argument(s).");
                            stringBuilder.AppendLine($"{leftPad}│\tAvailable signatures are:");
                            stringBuilder.AppendLine($"{leftPad}│\t\t- func {Mangler.Demangle(func.Name)}({(func.Arguments is not null ? string.Join(", ", func.Arguments) : "None")})");
                        }
                        else
                        {
                            stringBuilder.AppendLine($"{leftPad}│\t\t- func {Mangler.Demangle(func.Name)}({(func.Arguments is not null ? string.Join(", ", func.Arguments) : "None")})");
                        }
                    }
                }
            }
        }

        private void ElaborateOnContext(RuntimeExceptionType excType)
        {
            int errorlLineNum = InstructionLine.LineInSourceCode;
            int lineNumLen = errorlLineNum.ToString().Length;
            bool foundMatch = false;
            StringBuilder stringBuilder = new StringBuilder();

            switch (excType)
            {
                case RuntimeExceptionType.UnknownVariable:

                    foreach (FluenceScope scope in Parser.CurrentParseState.NameSpaces.Values)
                    {
                        ElaborateOnUndefinedFunction(scope, stringBuilder, out foundMatch);
                    }

                    if (!foundMatch)
                    {
                        ElaborateOnUndefinedFunction(Parser.CurrentParserStateGlobalScope, stringBuilder, out foundMatch);
                    }

                    if (!foundMatch)
                    {
                        // if there is still no match, then it is just an undefined variable, or maybe struct?
                    }
                    break;
                default:
                    break;
            }

            stringBuilder.Append($"{new string(' ', lineNumLen + 1)}│");
            if (foundMatch)
            {
                ExceptionMessage = stringBuilder.ToString();
            }
        }

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
                faultyLine = lines[InstructionLine.LineInSourceCode == -1 ? 0 : InstructionLine.LineInSourceCode - 1];
            }
            else
            {
                faultyLine = Parser.Lexer.SourceCode.Split(Environment.NewLine)[InstructionLine.LineInSourceCode - 1];
            }

            int errorlLineNum = InstructionLine.LineInSourceCode;
            int errorColumnNum = InstructionLine.ColumnInSourceCode;
            int lineNumLen = errorlLineNum.ToString().Length;

            if (ExceptionType != RuntimeExceptionType.NonSpecific)
            {
                ElaborateOnContext(ExceptionType);
            }

            stringBuilder.AppendLine($"\nException occured in: {(string.IsNullOrEmpty(fileName) ? "Script" : fileName)}.");

            if (!string.IsNullOrEmpty(filepath))
            {
                stringBuilder.AppendLine($"Exact path: {filepath}");
            }

            string leftPad = new string(' ', lineNumLen + 1);

            if (errorlLineNum > 0 && faultyLine != null && errorColumnNum > 0)
            {
                stringBuilder
                    .AppendLine($"RUNTIME ERROR at approximately: line {errorlLineNum}, Column {errorColumnNum}")
                    .AppendLine($"\nMost likely line where the error occured:")
                    .AppendLine($"\n{leftPad}│")
                    .AppendLine($"{leftPad}│")
                    .AppendLine($"{leftPad}│\t{ExceptionMessage}")
                    .AppendLine($"{leftPad}│")
                    .AppendLine($"{errorlLineNum}.│ {faultyLine}")
                    .AppendLine($"{leftPad}│{new string(' ', errorColumnNum - lineNumLen)}^")
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
                        stringBuilder.AppendLine($"                   │ [{item}]");
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
}