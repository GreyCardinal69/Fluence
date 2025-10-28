using System.Text;
using static Fluence.FluenceByteCode;
using static Fluence.FluenceInterpreter;

namespace Fluence
{
    internal static class BytecodeTestGenerator
    {
        /// <summary>
        /// Generates a C# code string that declares and initializes a <see cref="List{InstructionLine}"/>
        /// with the exact content of the provided bytecode list.
        /// </summary>
        /// <param name="bytecode">The list of instructions to convert into C# code.</param>
        /// <param name="variableName">The name for the C# list variable.</param>
        /// <returns>A formatted string of C# code.</returns>
        internal static void GenerateCSharpCodeForInstructionList(List<InstructionLine> bytecode, TextOutputMethod outLine, string variableName = "expectedCode")
        {
            outLine("\n----------Code String For Tests----------\n");

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"List<InstructionLine> {variableName} = new List<InstructionLine>");
            sb.AppendLine("{");

            foreach (InstructionLine instruction in bytecode)
            {
                if (instruction == null)
                {
                    sb.AppendLine("    null,");
                    continue;
                }

                string instructionType = $"InstructionCode.{instruction.Instruction}";
                string lhs = GenerateValueCode(instruction.Lhs);
                string rhs = GenerateValueCode(instruction.Rhs);
                string rhs2 = GenerateValueCode(instruction.Rhs2);
                string rhs3 = GenerateValueCode(instruction.Rhs3);

                sb.AppendLine($"    new({instructionType}, {(lhs == "" ? "null!," : $"{lhs},")} {(rhs == "" ? "null!," : $"{rhs},")} {(rhs2 == "" ? "null!," : $"{rhs2},")} {(rhs3 == "" ? "null!" : $"{rhs3}")}),");
            }

            sb.AppendLine("};");
            outLine(sb.ToString());
            outLine("\n----------Code String For Tests End----------\n\n\n");
        }

        private static string GenerateValueCode(Value value)
        {
            switch (value)
            {
                case null: return "";
                case NumberValue numVal: return $"new NumberValue({numVal.Value})";
                case StringValue strVal:
                    string escapedString = strVal.Value.Replace("\\", "\\\\").Replace("\"", "\\\"");
                    return $"new StringValue(\"{escapedString}\")";
                case NilValue: return "new NilValue()";
                case TempValue tempVal: return $"new TempValue({tempVal.TempName[6..]})";
                case VariableValue varVal: return $"new VariableValue(\"{varVal.Name}\")";
                case FunctionValue funcVal: return $"new FunctionValue(\"{funcVal.Name}\", {funcVal.StartAddress}, {funcVal.Arity}, {funcVal.StartAddressInSource}, [], [], null!)";
                default: return "";
            }
        }
    }
}