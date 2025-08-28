namespace Fluence
{
    /// <summary>
    /// A static class containing definitions for Fluence bytecode, including the InstructionLine class and InstructionCode enum.
    /// </summary>
    internal static class FluenceByteCode
    {
        /// <summary>
        /// Dumps a list of bytecode instructions to the console in a formatted table.
        /// </summary>
        /// <param name="instructions">The list of instructions to dump.</param>
        internal static void DumpByteCodeInstructions(List<InstructionLine> instructions)
        {
            Console.WriteLine("--- Compiled Bytecode ---\n");
            Console.WriteLine(string.Format("{0,-5} {1,-20} {2,-50} {3,-45} {4,-40} {5, -25}", "", "TYPE", "LHS", "RHS", "RHS2", "RHS3"));
            Console.WriteLine();
            if (instructions == null || instructions.Count == 0)
            {
                Console.WriteLine("(No instructions generated)");
                return;
            }

            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i] == null) Console.WriteLine($"{i:D4}: NULL");
                else Console.WriteLine($"{i:D4}: {instructions[i].ToString().Replace("\n", "")}");
            }
            Console.WriteLine("\n--- End of Bytecode ---");
        }

        /// <summary>
        /// Represents a single line of executable Fluence bytecode.
        /// An instruction consists of an opcode and up to three operands (LHS, RHS, RHS2).
        /// </summary>
        internal sealed class InstructionLine
        {
            /// <summary>
            /// Defines all possible operation codes (opcodes) for the Fluence VM.
            /// </summary>
            internal enum InstructionCode
            {
                // == Control Flow ==
                Skip,           // No operation. Placeholder.
                Goto,           // Unconditional jump to address in Lhs.
                GotoIfTrue,     // Jump to Lhs if Rhs is true.
                GotoIfFalse,    // Jump to Lhs if Rhs is false.
                Return,         // Return a value (Lhs) from the current function.
                Terminate,      // Halt program execution.

                // == State & Assignment ==
                Assign,         // Assigns the value of Rhs to the variable/location in Lhs.

                // == Arithmetic & Unary Operations ==
                Add,
                Subtract,
                Multiply,
                Divide,
                Modulo,
                Power,
                Negate,         // Unary negation, e.g., -(x).

                // == Logical & Comparison Operations ==
                Equal,
                NotEqual,
                LessThan,
                GreaterThan,
                LessEqual,
                GreaterEqual,
                And,            // Logical &&
                Or,             // Logical ||
                Not,            // Logical !

                // == Bitwise Operations ==
                BitwiseAnd,
                BitwiseOr,
                BitwiseXor,
                BitwiseNot,
                BitwiseLShift,
                BitwiseRShift,

                NewIterator,
                IterNext,

                // == Function & Method Calls ==
                PushParam,      // Pushes a value (Lhs) onto the argument stack for a subsequent call.
                CallFunction,   // Calls a function (Rhs) with a specified number of arguments (Rhs2). Result stored in Lhs.
                CallMethod,     // Calls a method (Rhs2) on an object (Rhs). Result stored in Lhs.

                // == Object & Struct Operations ==
                NewInstance,    // Creates a new instance of a struct (Rhs). Result stored in Lhs.
                GetField,       // Gets the value of a field (Rhs2) from an object (Rhs). Result stored in Lhs.
                SetField,       // Sets the value of a field (Rhs2) on an object (Lhs) to a new value (Rhs).

                // == List & Collection Operations ==
                NewList,        // Creates a new, empty list. Result stored in Lhs.
                NewRange,   // Creates a new list from a range (Rhs to Rhs2). Result stored in Lhs.
                PushElement,    // Pushes an element (Rhs) onto a list (Lhs).
                GetElement,     // Gets an element at an index (Rhs2) from a list (Rhs). Result stored in Lhs.
                SetElement,     // Sets an element at an index (Rhs) on a list (Lhs) to a new value (Rhs2).
                GetLength,      // Gets the length of a collection (Rhs). Result stored in Lhs.

                // == Type Operations ==
                ToString,       // Converts a value (Rhs) to its string representation. Result stored in Lhs.

                //      ==!!==
                //      The following are special bytecode instructions generated solely by the Optimizer class after the parsing phase.

                // Double compound ops, op + assign in one instruction.
                AddAssign,
                SubAssign,
                MulAssign,
                DivAssign,
                ModAssign,

                BranchIfEqual,
                BranchIfNotEqual,
            }

            /// <summary>Gets the operation code for this instruction.</summary>
            internal InstructionCode Instruction;

            /// <summary>Gets the primary operand, often the destination or target of the operation.</summary>
            internal Value Lhs; // Mutable for back-patching jumps.

            /// <summary>Gets the first source operand.</summary>
            internal Value Rhs;

            /// <summary>Gets the second source operand.</summary>
            internal Value Rhs2;

            /// <summary>The third source operand, used only in specialized instructions and generated strictly by the optimizer.</summary>
            internal Value Rhs3;

            // Defines the signature for a specialized, high-performance handler for binary numeric operations.
            internal delegate RuntimeValue BinaryOpHandler(RuntimeValue left, RuntimeValue right);

            /// <summary>
            /// Gets or sets the cached 'fast path' handler for this instruction, used by the inline caching system.
            /// </summary>
            internal BinaryOpHandler? Handler { get; set; } = null!;

            /// <summary>
            /// Gets or sets the cached <see cref="RuntimeNumberType"/> of the left-hand side (Rhs) operand.
            /// </summary>
            internal RuntimeNumberType CachedLhsType { get; set; } = RuntimeNumberType.Unknown;

            /// <summary>
            /// Gets or sets the cached <see cref="RuntimeNumberType"/> of the left-hand side (Rhs) operand.
            /// </summary>
            internal RuntimeNumberType CachedRhsType { get; set; } = RuntimeNumberType.Unknown;

            /// <summary>
            /// Defines the signature for a specialized, high-performance handler for branch instructions.
            /// </summary>
            /// <param name="instruction">The instruction being executed.</param>
            /// <param name="vm">The VM instance, used to access state like the instruction pointer.</param>
            public delegate void BranchHandler(InstructionLine instruction, FluenceVirtualMachine vm);

            /// <summary>
            /// Gets or sets the cached 'fast path' handler for a branch instruction.
            /// </summary>
            public BranchHandler? BranchingHandler { get; set; } = null!;

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