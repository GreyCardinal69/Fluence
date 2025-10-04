namespace Fluence
{
    /// <summary>
    /// A static class containing definitions for Fluence bytecode.
    /// </summary>
    internal static class FluenceByteCode
    {
        /// <summary>
        /// Represents a single line of executable Fluence bytecode.
        /// An instruction consists of an opcode and up to four operands (LHS, RHS, RHS2, RHS3).
        /// </summary>
        internal sealed class InstructionLine
        {
            /// <summary>
            /// Defines all possible operation codes (opcodes) for the Fluence VM.
            /// </summary>
            internal enum InstructionCode
            {
                Skip,           // No operation. Placeholder.
                Goto,
                GotoIfTrue,
                GotoIfFalse,
                Return,
                Terminate,      // Halt program execution.

                Assign,

                Add,
                Subtract,
                Multiply,
                Divide,
                Modulo,
                Power,
                Negate,         // Unary negation, '-(x)'.

                Equal,
                NotEqual,
                LessThan,
                GreaterThan,
                LessEqual,
                GreaterEqual,
                And,            // Logical &&
                Or,             // Logical ||
                Not,            // Logical !

                BitwiseAnd,
                BitwiseOr,
                BitwiseXor,
                BitwiseNot,
                BitwiseLShift,
                BitwiseRShift,

                NewIterator,
                IterNext,

                GetType,

                // Function & Method Calls.
                PushParam,      // Pushes a value (Lhs) onto the argument stack.
                CallFunction,
                CallMethod,

                // Object & Struct Operations.
                NewInstance,
                GetField,
                SetField,

                // List & Collection Operations.
                NewList,
                NewRange,
                PushElement,
                GetElement,
                SetElement,
                GetLength,

                CallStatic,
                GetStatic,
                SetStatic,

                // Type Operations.
                ToString,

                NewLambda,
                LoadAddress,    // REF

                //      ==!!==
                //      The following are special bytecode instructions generated solely by the Optimizer class after the parsing phase.

                // Double compound ops, op + assign in one instruction.
                AddAssign,
                SubAssign,
                MulAssign,
                DivAssign,
                ModAssign,

                /// <summary> Increments an integer variable, even if it is readonly. </summary>
                IncrementIntUnrestricted,

                AssignTwo,

                BranchIfEqual,
                BranchIfNotEqual,

                PushTwoParams,
                PushThreeParams,
                PushFourParams,
            }

            /// <summary>The operation code for this instruction.</summary>
            internal InstructionCode Instruction;

            /// <summary>The primary operand, often the destination or target of the operation.</summary>
            internal Value Lhs;

            /// <summary>The first source operand.</summary>
            internal Value Rhs;

            /// <summary>The second source operand.</summary>
            internal Value Rhs2;

            /// <summary>The third source operand, used only in specialized instructions and generated strictly by the optimizer.</summary>
            internal Value Rhs3;

            /// <summary>
            /// Defines the signature for a specialized opcode handler that bypasses
            /// the generic logic for improved performance on subsequent calls.
            /// </summary>
            internal delegate void SpecializedOpcodeHandler(InstructionLine instruction, FluenceVirtualMachine vm);

            /// <summary>The approximate line location the instruction points to in the source file.</summary>
            internal int LineInSourceCode { get; private set; }

            /// <summary>The approximate column location the instruction points to in the source file.</summary>
            internal int ColumnInSourceCode { get; private set; }

            /// <summary>
            /// In a multi-file project, this is the index into the project's file path table
            /// that identifies the source file for this instruction.
            /// </summary>
            internal int ProjectFileIndex { get; private set; }

            internal void SetDebugInfo(int column, int line, int fileIndex)
            {
                ColumnInSourceCode = column;
                LineInSourceCode = line;
                ProjectFileIndex = fileIndex;
            }

            /// <summary>
            /// The cached, optimized "fast path" for this instruction.
            /// If this is not null, it is executed by the generic opcode handler.
            /// </summary>
            internal SpecializedOpcodeHandler? SpecializedHandler { get; set; }

            internal InstructionLine(InstructionCode instruction, Value lhs, Value rhs = null!, Value rhs2 = null!, Value rhs3 = null!)
            {
                Instruction = instruction;
                Lhs = lhs;
                Rhs = rhs;
                Rhs2 = rhs2;
                Rhs3 = rhs3;
            }

            public override string ToString()
            {
                string instruction = Instruction.ToString();
                string lhs = Lhs != null ? Lhs.ToString() : "Null";
                string rhs = Rhs != null ? Rhs.ToString() : "Null";
                string rhs2 = Rhs2 != null ? Rhs2.ToString() : "Null";
                string rhs3 = Rhs3 != null ? Rhs3.ToString() : "Null";
                return string.Format("{0,-20} {1,-50} {2,-45} {3,-40} {4, -25}", instruction, lhs, rhs, rhs2, rhs3);
            }
        }
    }
}