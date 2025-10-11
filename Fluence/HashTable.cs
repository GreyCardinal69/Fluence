namespace Fluence
{
    internal static class HashTable
    {
        private static readonly Dictionary<int, string> _hashToName = new();
        private static readonly object _lock = new();

        internal static void Register(string name, int hash)
        {
            lock (_lock)
            {
                if (!_hashToName.ContainsKey(hash))
                    _hashToName[hash] = name;
            }
        }

        internal static bool TryGetName(int hash, out string name)
            => _hashToName.TryGetValue(hash, out name!);
    }
}