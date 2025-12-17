using System.Runtime.InteropServices;

namespace Fluence
{
    internal sealed class StringPool
    {
        private static readonly Dictionary<int, string> _pool = new Dictionary<int, string>();

        internal static string Intern(ReadOnlySpan<char> span)
        {
            // TO DO, potential hash collission, might need better hash.
            int hash = 5381;
            for (int i = 0; i < span.Length; i++)
            {
                hash = (hash << 5) + hash + span[i]; // hash * 33 + c.
            }

            ref string? interned = ref CollectionsMarshal.GetValueRefOrAddDefault(_pool, hash, out bool exists);

            if (exists)
            {
                if (interned!.Length == span.Length && span.SequenceEqual(interned))
                {
                    return interned!;
                }
            }

            string newString = span.ToString();
            interned = newString;
            return newString;
        }

        /// <summary>
        /// Clears the string pool.
        /// </summary>
        public static void Clear()
        {
            _pool.Clear();
        }
    }
}