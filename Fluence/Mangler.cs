using System.Runtime.CompilerServices;

namespace Fluence.Fluence
{
    internal static class Mangler
    {
        private const string _separator = "__";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Mangle(string baseName, int arity)
        {
            return $"{baseName}{_separator}{arity}";
        }

        public static string Demangle(string mangledName, out int arity)
        {
            int sepIndex = mangledName.LastIndexOf(_separator, StringComparison.Ordinal);
            if (sepIndex > 0 && int.TryParse(mangledName[(sepIndex + _separator.Length)..], out arity))
            {
                return mangledName[..sepIndex];
            }

            arity = -1;
            return mangledName;
        }
    }
}