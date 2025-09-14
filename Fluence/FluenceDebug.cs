using static Fluence.FluenceByteCode;

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
        /// <param name="startAddress"></param>
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
    }
}