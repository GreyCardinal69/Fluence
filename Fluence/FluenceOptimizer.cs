using static Fluence.FluenceByteCode;
using static Fluence.FluenceByteCode.InstructionLine;
using static Fluence.FluenceParser;

namespace Fluence
{
    /// <summary>
    /// A class meant for the optimization of bytecode, done incrementally by the parser during parsing.
    /// </summary>
    internal static class FluenceOptimizer
    {
        /// <summary>
        /// Incrementally optimizes a segment of the bytecode list..
        /// It scans for optimizable patterns from a given start index, performs fusions, and then compacts the list while realigning all addresses.
        /// </summary>
        /// <param name="bytecode">The list of bytecode instructions to be modified. It is passed by reference and will be modified in-place.</param>
        /// <param name="parseState">The current state of the parser, containing symbol tables which are required for patching function and method start addresses.</param>
        /// <param name="startIndex">The index in the bytecode list from which to start scanning for optimizations.</param>
        internal static void OptimizeChunk(ref List<InstructionLine> bytecode, ParseState parseState, int startIndex)
        {
            bool byteCodeChanged = false;
            FuseGotoConditionals(ref bytecode, startIndex, ref byteCodeChanged);
            FuseCompoundAssignments(ref bytecode, startIndex, ref byteCodeChanged);
            FuseSimpleAssignments(ref bytecode, startIndex, ref byteCodeChanged);
            FuseTwoPushParams(ref bytecode, startIndex, ref byteCodeChanged);

            if (byteCodeChanged)
            {
                CompactAndRealignFromBottomUp(ref bytecode, parseState);
            }
        }

        /// <summary>
        /// Scans for an arithmetic operation followed by an assignment to a variable, fusing them into a single compound assignment instruction (e.g., AddAssign).
        /// The second, now redundant, instruction is replaced with a null placeholder for later removal.
        /// </summary>
        /// <param name="bytecode">The bytecode list to modify.</param>
        /// <param name="startIndex">The index from which to begin scanning.</param>
        private static void FuseCompoundAssignments(ref List<InstructionLine> bytecode, int startIndex, ref bool byteCodeChanged)
        {
            for (int i = startIndex; i < bytecode.Count - 1; i++)
            {
                InstructionLine line1 = bytecode[i];
                InstructionLine line2 = bytecode[i + 1];
                if (line1 == null || line2 == null) continue;

                InstructionCode opCode = GetFusedOpcode(line1.Instruction);

                // Pattern Match:
                // line1: [Arithmetic] TempN TempN-1 Value
                // line2: [Assign]     Var   TempN
                // =>
                // line: [Arithmetic][Assign] Var TempN-1 Value
                if (opCode != InstructionCode.Skip &&
                    line2.Instruction == InstructionCode.Assign &&
                    line1.Lhs is TempValue l1Lhs &&
                    line2.Lhs is VariableValue &&
                    line2.Rhs is TempValue l2Rhs &&
                    l1Lhs.TempName == l2Rhs.TempName)
                {
                    byteCodeChanged = true;
                    bytecode[i].Instruction = opCode;
                    bytecode[i].Lhs = line2.Lhs;
                    bytecode[i].Rhs = line1.Rhs;
                    bytecode[i].Rhs2 = line1.Rhs2;
                    bytecode[i + 1] = null!;
                    i++;
                }
            }
        }

        /// <summary>
        /// Fuses two PushParam instructions into one.
        /// </summary>
        /// <param name="bytecode">The bytecode list to modify.</param>
        /// <param name="startIndex">The index from which to begin scanning.</param>
        private static void FuseTwoPushParams(ref List<InstructionLine> bytecode, int startIndex, ref bool byteCodeChanged)
        {
            for (int i = startIndex; i < bytecode.Count - 1; i++)
            {
                InstructionLine line1 = bytecode[i];
                InstructionLine line2 = bytecode[i + 1];
                if (line1 == null || line2 == null) continue;

                if (line1.Instruction == InstructionCode.PushParam && line2.Instruction == InstructionCode.PushParam)
                {
                    byteCodeChanged = true;
                    bytecode[i].Instruction = InstructionCode.PushTwoParams;
                    bytecode[i].Rhs = line2.Lhs;
                    bytecode[i + 1] = null!;
                    i++;
                }
            }
        }

        /// <summary>
        /// Combines two simple assignment operations into one.
        /// </summary>
        /// <param name="bytecode">The bytecode list to modify.</param>
        /// <param name="startIndex">The index from which to begin scanning.</param>
        private static void FuseSimpleAssignments(ref List<InstructionLine> bytecode, int startIndex, ref bool byteCodeChanged)
        {
            for (int i = startIndex; i < bytecode.Count - 1; i++)
            {
                InstructionLine line1 = bytecode[i];
                InstructionLine line2 = bytecode[i + 1];
                if (line1 == null || line2 == null) continue;

                if (line1.Instruction == InstructionCode.Assign && line2.Instruction == InstructionCode.Assign && line2.Rhs != line1.Lhs)
                {
                    byteCodeChanged = true;
                    bytecode[i].Instruction = InstructionCode.AssignTwo;
                    bytecode[i].Lhs = line1.Lhs;
                    bytecode[i].Rhs = line1.Rhs;
                    bytecode[i].Rhs2 = line2.Lhs;
                    bytecode[i].Rhs3 = line2.Rhs;
                    bytecode[i + 1] = null!;
                    i++;
                }
            }
        }

        /// <summary>
        /// Scans for a comparison operation followed by a conditional jump that uses its result. Fuses them into a single, more efficient branch instruction (e.g., BranchIfNotEqual).
        /// The second, now redundant, instruction is replaced with a null placeholder for later removal.
        /// </summary>
        /// <param name="bytecode">The bytecode list to modify.</param>
        /// <param name="startIndex">The index from which to begin scanning.</param>
        private static void FuseGotoConditionals(ref List<InstructionLine> bytecode, int startIndex, ref bool byteCodeChanged)
        {
            for (int i = startIndex; i < bytecode.Count - 1; i++)
            {
                InstructionLine line1 = bytecode[i];
                InstructionLine line2 = bytecode[i + 1];
                if (line1 == null || line2 == null) continue;

                InstructionCode op = GetFusedGotoOpCode(line1.Instruction, line2.Instruction);

                // Pattern Match:
                // Not/Equal             TempN    A          B
                // GotoIfTrue/False      JMP      TEMPN      .
                // =>
                // BranchIfEqual/Not     JMP      A          B   
                if (op != InstructionCode.Skip &&
                    line1.Lhs is TempValue cResult &&
                    line2.Rhs is TempValue jCond &&
                    cResult.TempName == jCond.TempName)
                {
                    byteCodeChanged = true;
                    bytecode[i].Instruction = op;
                    bytecode[i].Lhs = line2.Lhs;
                    bytecode[i].Rhs = line1.Rhs;
                    bytecode[i].Rhs2 = line1.Rhs2;
                    bytecode[i + 1] = null!;
                    i++;
                }
            }
        }

        /// <summary>
        /// Gets the corresponding branch instruction for a given comparison and conditional goto pair.
        /// </summary>
        /// <returns>The fused instruction code, or <see cref="InstructionCode.Skip"/> if no pattern matches.</returns>
        private static InstructionCode GetFusedGotoOpCode(InstructionCode op1, InstructionCode op2) => (op1, op2) switch
        {
            (InstructionCode.Equal, InstructionCode.GotoIfTrue) or (InstructionCode.NotEqual, InstructionCode.GotoIfFalse) => InstructionCode.BranchIfEqual,
            (InstructionCode.Equal, InstructionCode.GotoIfFalse) or (InstructionCode.NotEqual, InstructionCode.GotoIfTrue) => InstructionCode.BranchIfNotEqual,
            _ => InstructionCode.Skip,
        };

        /// <summary>
        /// Gets the corresponding compound assignment instruction for a given arithmetic operation.
        /// </summary>
        /// <returns>The fused instruction code, or <see cref="InstructionCode.Skip"/> if no pattern matches.</returns>
        private static InstructionCode GetFusedOpcode(InstructionCode op) => op switch
        {
            InstructionCode.Add => InstructionCode.AddAssign,
            InstructionCode.Subtract => InstructionCode.SubAssign,
            InstructionCode.Multiply => InstructionCode.MulAssign,
            InstructionCode.Divide => InstructionCode.DivAssign,
            InstructionCode.Modulo => InstructionCode.ModAssign,
            _ => InstructionCode.Skip
        };

        /// <summary>
        /// Checks if the given instruction code is a type of jump.
        /// </summary>
        /// <returns>True if the instruction is a jump, otherwise false.</returns>
        private static bool IsJumpInstruction(InstructionCode op) =>
            op is InstructionCode.Goto
            or InstructionCode.GotoIfTrue
            or InstructionCode.GotoIfFalse
            or InstructionCode.BranchIfEqual
            or InstructionCode.BranchIfNotEqual;

        /// <summary>
        /// Compacts the bytecode list by removing all null placeholders and realigns all absolute addresses.
        /// It iterates from the end of the list to the beginning, which provides a stable and correct approach for in-place removal and patching.
        /// </summary>
        private static void CompactAndRealignFromBottomUp(ref List<InstructionLine> bytecode, ParseState state)
        {
            for (int i = bytecode.Count - 1; i >= 0; i--)
            {
                if (bytecode[i] == null)
                {
                    bytecode.RemoveAt(i);
                    PatchAllAddressesAfterRemoval(ref bytecode, state, i);
                }
            }
        }

        /// <summary>
        /// Patches all absolute addresses in the bytecode and symbol tables after a single instruction has been removed.
        /// </summary>
        /// <param name="bytecode">The bytecode list, now one element shorter.</param>
        /// <param name="state">The parse state containing symbols to patch.</param>
        /// <param name="removedIndex">The index of the instruction that was just removed. All addresses greater than this index will be decremented.</param>
        private static void PatchAllAddressesAfterRemoval(ref List<InstructionLine> bytecode, ParseState state, int removedIndex)
        {
            int MapAddr(int oldAddr)
            {
                return oldAddr > removedIndex ? oldAddr - 1 : oldAddr;
            }

            void PatchFunctionValue(FunctionValue f)
            {
                f?.SetStartAddress(MapAddr(f.StartAddress));
            }

            for (int i = 0; i < bytecode.Count; i++)
            {
                InstructionLine insn = bytecode[i];
                if (insn == null) continue;

                if (IsJumpInstruction(insn.Instruction) && insn.Lhs is NumberValue targetAddr)
                {
                    targetAddr.ReAssign(MapAddr((int)targetAddr.Value));
                }
                if (insn.Rhs is FunctionValue fvRhs) PatchFunctionValue(fvRhs);
                if (insn.Rhs2 is FunctionValue fvRhs2) PatchFunctionValue(fvRhs2);
            }

            foreach (Symbol symbol in state.GlobalScope.Symbols.Values)
            {
                if (symbol is FunctionSymbol f) f.SetStartAddress(MapAddr(f.StartAddress));
                else if (symbol is StructSymbol s)
                {
                    foreach (var item in s.Constructors)
                    {
                        PatchFunctionValue(item.Value);
                    }
                    foreach (FunctionValue m in s.Functions.Values) PatchFunctionValue(m);
                }
            }
            foreach (FluenceScope scope in state.NameSpaces.Values)
            {
                foreach (Symbol symbol in scope.Symbols.Values)
                {
                    if (symbol is FunctionSymbol f) f.SetStartAddress(MapAddr(f.StartAddress));
                    else if (symbol is StructSymbol s)
                    {
                        foreach (var item in s.Constructors)
                        {
                            PatchFunctionValue(item.Value);
                        }
                        foreach (FunctionValue m in s.Functions.Values) PatchFunctionValue(m);
                    }
                }
            }
        }
    }
}