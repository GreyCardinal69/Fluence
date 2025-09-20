using System.Runtime.CompilerServices;

namespace Fluence
{
    /// <summary> Provides static helper methods for name mangling and demangling. </summary>
    internal static class Mangler
    {
        /// <summary> The constant separator used to distinguish the base name from the mangled arity suffix.</summary>
        private const string _separator = "__";

        /// <summary> Mangles a base function name by appending a special separator at the end and appending its arity.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Mangle(string baseName, int arity)
        {
            return $"{baseName}{_separator}{arity}";
        }

        /// <summary> Demangles a name, separating it back into its base name and arity.</summary>
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

        /// <summary> Demangles a name, separating it back into its base name only.</summary>
        public static string Demangle(string mangledName)
        {
            int sepIndex = mangledName.LastIndexOf(_separator, StringComparison.Ordinal);
            if (sepIndex > 0 && int.TryParse(mangledName[(sepIndex + _separator.Length)..], out _))
            {
                return mangledName[..sepIndex];
            }

            return mangledName;
        }
    }
}