namespace Fluence
{
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
    }
}