using System.Runtime.InteropServices;
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
        private static readonly Dictionary<int, RegisterInfo> _registerInfoMap = new();
        private static readonly Dictionary<int, Value> _constantsMap = new();
        private static readonly List<int> _instructionsToRemove = new();
        private static readonly HashSet<string> _uniqueSymbols = new HashSet<string>();

        /// <summary>
        /// A private struct to hold information about a temporary register's assignments.
        /// </summary>
        private struct RegisterInfo
        {
            public int AssignmentCount;
            public int AssignmentIndex;
            public Value? ConstantValue;
        }

        /// <summary>
        /// Incrementally optimizes a segment of the bytecode list..
        /// It scans for optimizable patterns from a given start index, performs fusions, and then compacts the list while realigning all addresses.
        /// </summary>
        /// <param name="bytecode">The list of bytecode instructions to be modified. It is passed by reference and will be modified in-place.</param>
        /// <param name="parseState">The current state of the parser, containing symbol tables which are required for patching function and method start addresses.</param>
        /// <param name="startIndex">The index in the bytecode list from which to start scanning for optimizations.</param>
        internal static void OptimizeChunk(ref List<InstructionLine> bytecode, ParseState parseState, int startIndex, VirtualMachineConfiguration config)
        {
            bool byteCodeChanged = false;
            bool constantFoldingDidWork = false;

            FuseGotoConditionals(ref bytecode, startIndex, ref byteCodeChanged);
            RemoveConstTempRegisters(ref bytecode, startIndex, ref byteCodeChanged, ref constantFoldingDidWork);
            FuseCompoundAssignments(ref bytecode, startIndex, ref byteCodeChanged);
            FuseSimpleAssignments(ref bytecode, startIndex, ref byteCodeChanged);
            FusePushParams(ref bytecode, startIndex, ref byteCodeChanged);
            ConvertToIncrementsDecrements(ref bytecode, startIndex);
            FuseComparisonBranches(ref bytecode, startIndex, ref byteCodeChanged);

            if (byteCodeChanged)
            {
                CompactAndRealignFromBottomUp(ref bytecode, parseState);
                _uniqueSymbols.Clear();
            }

            if (constantFoldingDidWork)
            {
                _registerInfoMap.Clear();
                _constantsMap.Clear();
                _instructionsToRemove.Clear();
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
            Span<InstructionLine> byteCodeSpan = CollectionsMarshal.AsSpan(bytecode);
            Span<InstructionLine> relevantSpan = byteCodeSpan[startIndex..];

            int i = 0;
            while (i < relevantSpan.Length - 1)
            {
                ref InstructionLine line1 = ref relevantSpan[i];
                ref InstructionLine line2 = ref relevantSpan[i + 1];

                if (line1 == null || line2 == null)
                {
                    i++;
                    continue;
                }

                InstructionCode opCode = GetFusedOpcode(line1.Instruction);

                // Pattern Match:
                // line1: [Arithmetic] TempN TempN-1 Value
                // line2: [Assign]     Var   TempN
                // =>
                // line: [Arithmetic][Assign] Var TempN-1 Value
                if (opCode != InstructionCode.Skip &&
                    line2.Instruction == InstructionCode.Assign &&
                    line1.Lhs is TempValue l1Lhs &&
                    line2.Rhs is TempValue l2Rhs &&
                    line2.Lhs is VariableValue &&
                    l1Lhs.Hash == l2Rhs.Hash)
                {
                    line1.Instruction = opCode;
                    line1.Lhs = line2.Lhs;
                    relevantSpan[i + 1] = null!;
                    byteCodeChanged = true;
                    i += 2;
                }
                else
                {
                    i++;
                }
            }
        }

        /// <summary>
        /// Fuses two PushParam instructions into one.
        /// </summary>
        /// <param name="bytecode">The bytecode list to modify.</param>
        /// <param name="startIndex">The index from which to begin scanning.</param>
        private static void FusePushParams(ref List<InstructionLine> bytecode, int startIndex, ref bool byteCodeChanged)
        {
            Span<InstructionLine> byteCodeSpan = CollectionsMarshal.AsSpan(bytecode);
            Span<InstructionLine> relevantSpan = byteCodeSpan[startIndex..];

            int i = 0;
            while (i < relevantSpan.Length - 1)
            {
                ref InstructionLine insn1 = ref relevantSpan[i];

                if (insn1?.Instruction != InstructionCode.PushParam)
                {
                    i++;
                    continue;
                }

                if (i + 3 < relevantSpan.Length)
                {
                    ref InstructionLine insn2 = ref relevantSpan[i + 1];
                    ref InstructionLine insn3 = ref relevantSpan[i + 2];
                    ref InstructionLine insn4 = ref relevantSpan[i + 3];

                    if (insn2?.Instruction == InstructionCode.PushParam &&
                        insn3?.Instruction == InstructionCode.PushParam &&
                        insn4?.Instruction == InstructionCode.PushParam)
                    {
                        insn1.Instruction = InstructionCode.PushFourParams;
                        insn1.Rhs = insn2.Lhs;
                        insn1.Rhs2 = insn3.Lhs;
                        insn1.Rhs3 = insn4.Lhs;

                        relevantSpan[i + 1] = null!;
                        relevantSpan[i + 2] = null!;
                        relevantSpan[i + 3] = null!;

                        byteCodeChanged = true;
                        i += 4;
                        continue;
                    }
                }

                if (i + 2 < relevantSpan.Length)
                {
                    ref InstructionLine insn2 = ref relevantSpan[i + 1];
                    ref InstructionLine insn3 = ref relevantSpan[i + 2];

                    if (insn2?.Instruction == InstructionCode.PushParam &&
                        insn3?.Instruction == InstructionCode.PushParam)
                    {
                        insn1.Instruction = InstructionCode.PushThreeParams;
                        insn1.Rhs = insn2.Lhs;
                        insn1.Rhs2 = insn3.Lhs;

                        relevantSpan[i + 1] = null!;
                        relevantSpan[i + 2] = null!;

                        byteCodeChanged = true;
                        i += 3;
                        continue;
                    }
                }

                ref InstructionLine insn2_two = ref relevantSpan[i + 1];
                if (insn2_two?.Instruction == InstructionCode.PushParam)
                {
                    insn1.Instruction = InstructionCode.PushTwoParams;
                    insn1.Rhs = insn2_two.Lhs;

                    relevantSpan[i + 1] = null!;

                    byteCodeChanged = true;
                    i += 2;
                    continue;
                }

                i++;
            }
        }

        /// <summary>
        /// Converts an Add or a Subtract instruction that simply increments or decrements a variable into a slightly more faster Increment
        /// or Decrement instruction.
        /// </summary>
        /// <param name="bytecode">The bytecode list to modify.</param>
        /// <param name="startIndex">The index from which to begin scanning.</param>
        private static void ConvertToIncrementsDecrements(ref List<InstructionLine> bytecode, int startIndex)
        {
            for (int i = startIndex; i < bytecode.Count - 1; i++)
            {
                InstructionLine line1 = bytecode[i];
                if (line1 == null) continue;

                // Pattern Match:
                // Add/Sub      Var     Var     1
                // =>
                // ++/--        Var     1
                if ((line1.Instruction == InstructionCode.Add || line1.Instruction == InstructionCode.Subtract) &&
                     line1.Lhs is VariableValue var &&
                     line1.Rhs is VariableValue var2 &&
                     var.Hash == var2.Hash &&
                     line1.Rhs2 is NumberValue num &&
                     num.Type == NumberValue.NumberType.Integer &&
                     (int)num.Value == 1)
                {
                    InstructionCode instruction = line1.Instruction == InstructionCode.Add ? InstructionCode.Increment : InstructionCode.Decrement;

                    // This optimization does not change bytecode instructions to a considerable degree, no need to parch addresses.
                    bytecode[i].Instruction = instruction;
                    bytecode[i].Rhs = null!;
                    bytecode[i].Rhs2 = null!;
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
        /// If the bytecode contains any assignments to Temporary Registers, where the values assigned are const, we can remove those
        /// and place them directly in instructions where that Temporary Register is used, reducing instruction count.
        /// </summary>
        /// <param name="bytecode">The bytecode list to modify.</param>
        /// <param name="startIndex">The index from which to begin scanning.</param>
        private static void RemoveConstTempRegisters(ref List<InstructionLine> bytecode, int startIndex, ref bool byteCodeChanged, ref bool constantFoldingDidWork)
        {
            _registerInfoMap.Clear();
            _constantsMap.Clear();
            _instructionsToRemove.Clear();

            Span<InstructionLine> byteCodeSpan = CollectionsMarshal.AsSpan(bytecode);
            Span<InstructionLine> relevantSpan = byteCodeSpan[startIndex..];

            for (int i = 0; i < relevantSpan.Length; i++)
            {
                ref InstructionLine? insn = ref relevantSpan[i];

                if (insn is null) continue;

                if (insn.Instruction == InstructionCode.Assign && insn.Lhs is TempValue temp)
                {
                    ref RegisterInfo info = ref CollectionsMarshal.GetValueRefOrAddDefault(_registerInfoMap, temp.Hash, out bool exists);

                    info.AssignmentCount++;

                    if (!exists)
                    {
                        info.AssignmentIndex = startIndex + i;
                        if (IsAConstantValue(insn.Rhs))
                        {
                            info.ConstantValue = insn.Rhs;
                        }
                    }
                    else
                    {
                        info.ConstantValue = null;
                    }
                }
            }

            foreach (KeyValuePair<int, RegisterInfo> kvp in _registerInfoMap)
            {
                if (kvp.Value.AssignmentCount == 1 && kvp.Value.ConstantValue is not null)
                {
                    _constantsMap.Add(kvp.Key, kvp.Value.ConstantValue);
                    _instructionsToRemove.Add(kvp.Value.AssignmentIndex);
                }
            }

            if (_constantsMap.Count == 0)
            {
                return;
            }

            for (int i = 0; i < relevantSpan.Length; i++)
            {
                ref InstructionLine insn = ref relevantSpan[i];
                if (insn == null) continue;

                bool changed = false;

                if (insn.Rhs is TempValue tempRhs && _constantsMap.TryGetValue(tempRhs.Hash, out Value? constValRhs))
                {
                    insn.Rhs = constValRhs;
                    changed = true;
                }
                if (insn.Rhs2 is TempValue tempRhs2 && _constantsMap.TryGetValue(tempRhs2.Hash, out Value? constValRhs2))
                {
                    insn.Rhs2 = constValRhs2;
                    changed = true;
                }
                if (insn.Rhs3 is TempValue tempRhs3 && _constantsMap.TryGetValue(tempRhs3.Hash, out Value? constValRhs3))
                {
                    insn.Rhs3 = constValRhs3;
                    changed = true;
                }

                if (changed)
                {
                    constantFoldingDidWork = true;
                    byteCodeChanged = true;
                }
            }

            foreach (int index in _instructionsToRemove)
            {
                byteCodeSpan[index] = null!;
            }
        }

        /// <summary>
        /// Scans for a comparison operation followed by a conditional jump that uses its result. Fuses them into a single, more efficient branch instruction.
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
                    cResult.Hash == jCond.Hash)
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
        /// Scans for a comparison operation (<, <=, >, >=) followed by a conditional jump
        /// that uses its result, and fuses them into a single, efficient branch instruction.
        /// </summary>
        /// <param name="bytecode">The bytecode list to modify.</param>
        /// <param name="startIndex">The index from which to begin scanning.</param>
        /// <param name="byteCodeChanged">Flag to indicate if the bytecode was modified.</param>
        private static void FuseComparisonBranches(ref List<InstructionLine> bytecode, int startIndex, ref bool byteCodeChanged)
        {
            Span<InstructionLine> byteCodeSpan = CollectionsMarshal.AsSpan(bytecode);
            Span<InstructionLine> relevantSpan = byteCodeSpan[startIndex..];

            int i = 0;
            while (i < relevantSpan.Length - 1)
            {
                ref InstructionLine? line1 = ref relevantSpan[i]!;
                InstructionLine? line2 = relevantSpan[i + 1];

                if (line1 is null || line2 is null)
                {
                    i++;
                    continue;
                }

                InstructionCode fusedOp = GetFusedBranchOpCode(line1.Instruction, line2.Instruction);

                // Pattern Match:
                // [Comparison] TempN    A          B
                // [GotoIfTrue/False]    JMP        TempN      .
                // =>
                // [BranchIf...] JMP     A          B
                if (fusedOp != InstructionCode.Skip &&
                    line1.Lhs is TempValue comparisonResult &&
                    line2.Rhs is TempValue jumpCondition &&
                    comparisonResult.Hash == jumpCondition.Hash)
                {
                    line1.Instruction = fusedOp;
                    line1.Lhs = line2.Lhs;
                    relevantSpan[i + 1] = null!;
                    byteCodeChanged = true;
                    i += 2;
                }
                else
                {
                    i++;
                }
            }
        }

        /// <summary>
        /// Gets the corresponding branch instruction for a given comparison and conditional goto pair.
        /// </summary>
        /// <returns>The fused instruction code, or <see cref="InstructionCode.Skip"/> if no pattern matches.</returns>
        private static InstructionCode GetFusedBranchOpCode(InstructionCode comparisonOp, InstructionCode jumpOp) => (comparisonOp, jumpOp) switch
        {
            (InstructionCode.GreaterThan, InstructionCode.GotoIfTrue) => InstructionCode.BranchIfGreaterThan,
            (InstructionCode.GreaterThan, InstructionCode.GotoIfFalse) => InstructionCode.BranchIfLessOrEqual,

            (InstructionCode.LessThan, InstructionCode.GotoIfTrue) => InstructionCode.BranchIfLessThan,
            (InstructionCode.LessThan, InstructionCode.GotoIfFalse) => InstructionCode.BranchIfGreaterOrEqual,

            (InstructionCode.GreaterEqual, InstructionCode.GotoIfTrue) => InstructionCode.BranchIfGreaterOrEqual,
            (InstructionCode.GreaterEqual, InstructionCode.GotoIfFalse) => InstructionCode.BranchIfLessThan,

            (InstructionCode.LessEqual, InstructionCode.GotoIfTrue) => InstructionCode.BranchIfLessOrEqual,
            (InstructionCode.LessEqual, InstructionCode.GotoIfFalse) => InstructionCode.BranchIfGreaterThan,

            _ => InstructionCode.Skip,
        };

        /// <summary>
        /// Checks whether the given <see cref="Value"/> represents a constant value such as strings, chars, nil, bool or numeric.
        /// </summary>
        /// <param name="val">The Value to check.</param>
        /// <returns>True if the <see cref="Value"/> is considered constant.</returns>
        private static bool IsAConstantValue(Value val) => val is
            NumberValue or
            StringValue or
            CharValue or
            BooleanValue or
            NilValue;

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
            or InstructionCode.BranchIfNotEqual
            or InstructionCode.BranchIfGreaterThan
            or InstructionCode.BranchIfGreaterOrEqual
            or InstructionCode.BranchIfLessThan
            or InstructionCode.BranchIfLessOrEqual;

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

            for (int i = 0; i < bytecode.Count; i++)
            {
                InstructionLine insn = bytecode[i];
                if (insn == null) continue;

                if (IsJumpInstruction(insn.Instruction) && insn.Lhs is NumberValue targetAddr)
                {
                    targetAddr.ReAssign(MapAddr((int)targetAddr.Value));
                }
                if (insn.Lhs is TryCatchValue tryCatch)
                {
                    tryCatch.TryGoToIndex = MapAddr(tryCatch.TryGoToIndex);
                    tryCatch.CatchGoToIndex = MapAddr(tryCatch.CatchGoToIndex);
                }
                if (insn.Rhs is FunctionValue fvRhs)
                {
                    fvRhs.SetStartAddress(MapAddr(fvRhs.StartAddress));
                    fvRhs.SetEndAddress(MapAddr(fvRhs.EndAddress));
                }
                if (insn.Rhs is LambdaValue lambda)
                {
                    lambda.Function.SetStartAddress(MapAddr(lambda.Function.StartAddress));
                    lambda.Function.SetEndAddress(MapAddr(lambda.Function.EndAddress));
                }
                if (insn.Rhs2 is FunctionValue fvRhs2)
                {
                    fvRhs2.SetStartAddress(MapAddr(fvRhs2.StartAddress));
                    fvRhs2.SetEndAddress(MapAddr(fvRhs2.EndAddress));
                }
            }

            foreach (Symbol symbol in state.GlobalScope.Symbols.Values)
            {
                _uniqueSymbols.Add(symbol.Name);
                if (symbol is FunctionSymbol f)
                {
                    f.SetStartAddress(MapAddr(f.StartAddress));
                    f.SetEndAddress(MapAddr(f.EndAddress));
                }
                else if (symbol is StructSymbol s)
                {
                    foreach (KeyValuePair<string, FunctionValue> item in s.Constructors)
                    {
                        _uniqueSymbols.Add(item.Key);
                        item.Value.SetStartAddress(MapAddr(item.Value.StartAddress));
                        item.Value.SetEndAddress(MapAddr(item.Value.EndAddress));
                    }
                    foreach (FunctionValue m in s.Functions.Values)
                    {
                        _uniqueSymbols.Add(m.Name);
                        m.SetStartAddress(MapAddr(m.StartAddress));
                        m.SetEndAddress(MapAddr(m.EndAddress));
                    }
                }
            }

            foreach (FluenceScope scope in state.NameSpaces.Values)
            {
                foreach (Symbol symbol in scope.Symbols.Values)
                {
                    if (_uniqueSymbols.Contains(symbol.Name))
                    {
                        continue;
                    }

                    if (symbol is FunctionSymbol f) f.SetStartAddress(MapAddr(f.StartAddress));
                    else if (symbol is StructSymbol s)
                    {
                        foreach (KeyValuePair<string, FunctionValue> item in s.Constructors)
                        {
                            item.Value.SetStartAddress(MapAddr(item.Value.StartAddress));
                            item.Value.SetEndAddress(MapAddr(item.Value.EndAddress));
                        }
                        foreach (FunctionValue m in s.Functions.Values)
                        {
                            m.SetStartAddress(MapAddr(m.StartAddress));
                            m.SetEndAddress(MapAddr(m.EndAddress));
                        }
                    }
                }
            }
        }
    }
}