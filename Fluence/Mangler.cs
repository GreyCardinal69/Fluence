using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fluence
{
    /// <summary> Provides static helper methods for name mangling and demangling. </summary>
    internal static class Mangler
    {
        /// <summary> The constant separator used to distinguish the base name from the mangled arity suffix.</summary>
        private const string _separator = "__";

        private static readonly Dictionary<(string, int), string> _cache = new();

        /// <summary> Mangles a base function name by appending a special separator at the end and appending its arity.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Mangle(string name, int arity)
        {
            ref string? cached = ref CollectionsMarshal.GetValueRefOrAddDefault(_cache, (name, arity), out bool exists);

            if (exists)
            {
                return cached;
            }

            string mangled = $"{name}__{arity}";
            cached = mangled;

            return mangled;
        }

        /// <summary> Demangles a name, separating it back into its base name and arity.</summary>
        internal static string Demangle(string mangledName, out int arity)
        {
            int sepIndex = mangledName.LastIndexOf(_separator, StringComparison.Ordinal);
            if (sepIndex > 0 && int.TryParse(mangledName[(sepIndex + _separator.Length)..], out arity))
            {
                return mangledName[..sepIndex];
            }

            arity = -1;
            return mangledName;
        }

        /// <summary> Demangles a name, separating it back into only its base name.</summary>
        internal static string Demangle(string mangledName)
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