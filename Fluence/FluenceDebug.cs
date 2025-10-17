using Fluence.RuntimeTypes;
using System.Text;
using static Fluence.FluenceByteCode;
using static Fluence.FluenceInterpreter;

namespace Fluence
{
    /// <summary>
    /// Provides various debug functions.
    /// </summary>
    internal static class FluenceDebug
    {
        /// <summary>
        /// Formats a function's integer start address into a convenient string format. Limited to 1000 as of now.
        /// </summary>
        /// <param name="startAddress">The start address.</param>
        /// <returns>The formatted start address string.</returns>
        internal static string FormatByteCodeAddress(int startAddress)
        {
            if (startAddress < 10) return $"000{startAddress}";
            if (startAddress < 100) return $"00{startAddress}";
            if (startAddress < 1000) return $"0{startAddress}";
            if (startAddress < 1000) return $"{startAddress}";
            return "-1";
        }

        internal static string TruncateLine(string line, int maxLength = 75)
        {
            if (string.IsNullOrEmpty(line) || line.Length <= maxLength)
            {
                return line;
            }
            return string.Concat(line.AsSpan(0, maxLength - 3), "...");
        }

        /// <summary>
        /// Dumps a list of bytecode instructions to the console in a formatted table.
        /// </summary>
        /// <param name="instructions">The list of instructions to dump.</param>
        internal static void DumpByteCodeInstructions(List<InstructionLine> instructions, TextOutputMethod outMethod)
        {
            outMethod("--- Compiled Bytecode ---\n");
            outMethod("Value types with unique print format:");
            outMethod("VariableValue: Var_{Name}_{Register Index}_{Is Global?}");
            outMethod("TempValue: {Name}_{Register Index}");
            outMethod("FunctionValue: Func_{Name}_{Arity}_{TotalRegisters}_{Scope}_{StartAddress}/{EndAddress}\n");
            outMethod(string.Format("{0,-5} {1,-20} {2,-40} {3,-45} {4,-40} {5, -25}", "", "TYPE", "LHS", "RHS", "RHS2", "RHS3"));
            outMethod("");

            if (instructions == null || instructions.Count == 0)
            {
                outMethod("(No instructions generated)");
                return;
            }

            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i] == null) outMethod($"{i:D4}: NULL");
                else outMethod($"{i:D4}: {instructions[i].ToString().Replace("\n", "")}");
            }

            outMethod("\n--- End of Bytecode ---");
        }

        /// <summary>
        /// A helper debug function to print all tokens starting from the first token at the start index up to the token at the end index.
        /// </summary>
        /// <param name="start">The index of the first token.</param>
        /// <param name="end">The index of the last token.</param>
        internal static void DumpTokensFromTo(int start, int end, TextOutputMethod outMethod, FluenceLexer lexer)
        {
            for (int i = start; i < end; i++)
            {
                outMethod(lexer.PeekAheadByN(i).ToString());
            }
        }

        internal static void DumpSymbolTables(FluenceParser.ParseState parseState, TextOutputMethod outMethod)
        {
            StringBuilder sb = new StringBuilder("------------------------------------\n\nGenerated Symbol Hierarchy:\n\n");

            DumpScope(sb, parseState.GlobalScope, "Global Scope", 0);

            // If there are any namespaces, dump them as separate top-level scopes.
            if (parseState.NameSpaces.Count != 0)
            {
                sb.AppendLine();
                foreach (KeyValuePair<string, FluenceScope> ns in parseState.NameSpaces)
                {
                    DumpScope(sb, ns.Value, $"Namespace: {ns.Key}", 0);
                    outMethod("\n");
                }
            }

            sb.AppendLine("------------------------------------");
            outMethod(sb.ToString());
        }

        /// <summary>
        /// A recursive helper to dump the contents of a single scope and its children.
        /// </summary>
        /// <param name="sb">The StringBuilder to append to.</param>
        /// <param name="scope">The scope to dump.</param>
        /// <param name="scopeName">The display name for this scope.</param>
        /// <param name="indentationLevel">The current level of indentation.</param>
        private static void DumpScope(StringBuilder sb, FluenceScope scope, string scopeName, int indentationLevel)
        {
            string indent = new string(' ', indentationLevel * 4);

            sb.Append(indent).Append(scopeName).AppendLine(" {");

            if (scope.Symbols.Count == 0)
            {
                sb.Append(indent).AppendLine("    (empty)");
            }
            else
            {
                // Dump all symbols within the current scope.
                foreach (KeyValuePair<int, Symbol> item in scope.Symbols)
                {
                    DumpSymbol(sb, item.Value.Name, item.Value, indentationLevel + 1);
                }
            }

            sb.Append(indent).AppendLine("}").AppendLine();
        }

        /// <summary>
        /// Helper to dump a single symbol's details with proper indentation.
        /// </summary>
        private static void DumpSymbol(StringBuilder sb, string symbolName, Symbol symbol, int indentationLevel)
        {
            string indent = new string(' ', indentationLevel * 4);
            string innerIndent = new string(' ', (indentationLevel + 1) * 4);

            string args, argsRef;

            switch (symbol)
            {
                case EnumSymbol enumSymbol:
                    sb.Append(indent).Append($"Symbol: {symbolName}, type Enum {{").AppendLine();
                    foreach (KeyValuePair<string, EnumValue> member in enumSymbol.Members)
                    {
                        sb.Append(innerIndent).Append(member.Value.MemberName).Append(", ").Append(member.Value.Value).AppendLine();
                    }
                    sb.Append(indent).AppendLine("}");
                    break;

                case FunctionSymbol functionSymbol:
                    string scope = functionSymbol.DefiningScope == null || functionSymbol.Arguments == null ? $"None {(functionSymbol.IsIntrinsic ? "(Intrinsic)" : "Global?")}" : functionSymbol.DefiningScope.Name;
                    args = functionSymbol.Arguments == null ? "None" : string.Join(",", functionSymbol.Arguments);
                    argsRef = (functionSymbol.ArgumentsByRef == null || functionSymbol.ArgumentsByRef.Count == 0) ? "None" : string.Join(",", functionSymbol.ArgumentsByRef);

                    if (string.IsNullOrEmpty(args)) args = "None";

                    sb.Append(indent).Append($"Symbol: {symbolName}, type: Function Header {{");
                    sb.Append($" Arity: {functionSymbol.Arity}, Scope: {scope}, StartAddress: {FluenceDebug.FormatByteCodeAddress(functionSymbol.StartAddress)},");
                    sb.Append($" Args: {args}, ").Append($" RefArgs: {argsRef}, ").Append($" LocationInSource: {functionSymbol.StartAddressInSource}").AppendLine();
                    break;
                case VariableSymbol variableSymbol:
                    sb.Append(indent).Append($"Symbol: {symbolName}, type VariableSymbol: {variableSymbol}.").AppendLine();
                    break;
                case StructSymbol structSymbol:
                    sb.Append(indent).Append($"Symbol: {symbolName}, type Struct {{").AppendLine();
                    sb.Append(innerIndent).Append("Fields: ").Append(structSymbol.Fields.Count != 0 ? string.Join(", ", structSymbol.Fields) : "None").AppendLine(".");

                    if (structSymbol.Functions.Count != 0)
                    {
                        sb.Append(innerIndent).AppendLine("Functions: {");
                        foreach (KeyValuePair<string, FunctionValue> function in structSymbol.Functions)
                        {
                            args = function.Value.Arguments == null ? "None" : string.Join(",", function.Value.Arguments);
                            argsRef = function.Value.ArgumentsByRef == null ? "None" : string.Join(",", function.Value.ArgumentsByRef);

                            sb.Append(innerIndent).Append($"    Name: {function.Key}, Arity: {function.Value.Arity}, Start Address: {FormatByteCodeAddress(function.Value.StartAddress)}")
                              .Append($" Args: {args}, ").Append($" RefArgs: {argsRef}, ").Append($"Scope: {function.Value.DefiningScope}, ").Append($"Registers Size: {function.Value.TotalRegisterSlots}").AppendLine();
                        }
                        sb.Append(innerIndent).AppendLine("}");
                    }

                    sb.Append("\tDefault Values of Fields:");
                    if (structSymbol.DefaultFieldValuesAsTokens.Count != 0) sb.Append('\n');

                    foreach (KeyValuePair<string, List<Token>> item in structSymbol.DefaultFieldValuesAsTokens)
                    {
                        sb.Append($"\t\t{item.Key} : {(item.Value.Count == 0 ? "None (Nil)." : string.Join(", ", item.Value))}\n");
                    }

                    if (structSymbol.DefaultFieldValuesAsTokens.Count == 0) sb.Append(" None.\n");

                    sb.Append(indent).Append(indent).Append($"Constructors: {(structSymbol.Functions.Count == 0 ? "None.\n" : "\n")}");
                    foreach (KeyValuePair<string, FunctionValue> item in structSymbol.Constructors)
                    {
                        sb.Append(indent).Append(indent).Append(indent).Append(item).AppendLine();
                    }

                    sb.Append(indent).Append(indent).Append($"Functions: {(structSymbol.Functions.Count == 0 ? "None.\n" : "\n")}");
                    foreach (KeyValuePair<string, FunctionValue> item in structSymbol.Functions)
                    {
                        sb.Append(indent).Append(indent).Append(indent).Append(item).AppendLine();
                    }

                    sb.Append(indent).Append($"Static Intrinsics: {(structSymbol.StaticIntrinsics.Count == 0 ? "None.\n" : "\n")}");
                    foreach (KeyValuePair<string, FunctionSymbol> item in structSymbol.StaticIntrinsics)
                    {
                        sb.Append(indent).Append(indent).Append(item).AppendLine();
                    }

                    sb.Append(indent).Append($"Static Fields: {(structSymbol.StaticFields.Count == 0 ? "None.\n" : "\n")}");
                    foreach (KeyValuePair<string, RuntimeValue> item in structSymbol.StaticFields)
                    {
                        sb.Append(indent).Append(indent).Append(item).AppendLine();
                    }

                    sb.Append(indent).AppendLine("}");
                    break;
            }
        }
    }
}