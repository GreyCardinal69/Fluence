namespace Fluence
{
    internal static class FluenceByteCode
    {
        internal static void DumpByteCodeInstructions(List<InstructionLine> instructions)
        {
            Console.WriteLine("--- Compiled Bytecode ---\n");
            Console.WriteLine(string.Format("{0,-5} {1,-15} {2,-40} {3,-45} {4,-40}", "", "TYPE", "LHS", "RHS", "RHS2"));
            Console.WriteLine();
            if (instructions == null || instructions.Count == 0)
            {
                Console.WriteLine("(No instructions generated)");
                return;
            }

            for (int i = 0; i < instructions.Count; i++)
            {
                Console.WriteLine($"{i:D4}: {instructions[i]}");
            }
            Console.WriteLine("\n--- End of Bytecode ---");
        }

        internal class InstructionLine
        {
            internal enum InstructionCode
            {
                Skip,       // Does noting.
                AssignLeftHand,

                Add,
                Subtract,
                Multiply,
                Divide,
                Modulo,
                Power,
                Assign,

                Negate,      // Unary -(x)
                Not,         // !
                BitwiseNot,  // ~
                Increment,   // ++
                Decrement,   // --

                Equal,
                NotEqual,
                LessThan,
                GreaterThan,
                LessEqual,
                GreaterEqual,
                Is,             // is keyword
                IsNot,          // is not combo

                GetLength,      // This is basically list.GetLength(), called once in a for in loop.

                And,    // &&
                Or,     // ||

                BitwiseAnd,      // &
                BitwiseOr,       // |
                BitwiseXor,      // ^
                BitwiseLShift,   // <<
                BitwiseRShift,   // >>

                Goto,            // Unconditional jump to address in Lhs
                GotoIfTrue,      // Jump to Lhs if RhsA is true
                GotoIfFalse,     // Jump to Lhs if RhsA is false

                NewInstance,
                GetField,
                SetField,

                NewList,
                GetElement,
                SetElement,
                PushElement,
                NewRangeList,    // For ranges, aka num1..num2. Better than calling push element bytecode many times.

                Return,
                PushParam,
                CallFunction,

                ToString,

                CallIntrinsic,
                Terminate,       // Ends program.
            }

            internal InstructionCode Instruction;
            internal Value Lhs;
            internal Value Rhs;
            internal Value Rhs2;

            internal readonly Token Token;

            internal InstructionLine(InstructionCode instruction, Value lhs, Value rhs = null, Value rhs2 = null)
            {
                Instruction = instruction;
                Lhs = lhs;
                Rhs = rhs;
                Rhs2 = rhs2;
            }

            internal InstructionLine(InstructionCode instruction, Value lhs, Value rhs, Value rhs2, Token token)
            {
                Instruction = instruction;
                Lhs = lhs;
                Rhs = rhs;
                Rhs2 = rhs2;
                Token = token;
            }

            public override string ToString()
            {
                string instruction = Instruction.ToString();
                string lhs = Lhs != null ? Lhs.ToString() : "Null";
                string rhs = Rhs != null ? Rhs.ToString() : "Null";
                string rhs2 = Rhs2 != null ? Rhs2.ToString() : "Null";
                return string.Format("{0,-15} {1,-40} {2,-45} {3,-40}", instruction, lhs, rhs, rhs2);
            }
        }
    }
}