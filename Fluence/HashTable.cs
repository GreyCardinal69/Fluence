namespace Fluence
{
    internal static class HashTable
    {
        private static readonly Dictionary<string, int> _nameToHash = new();
        private static readonly Dictionary<int, string> _hashToName = new();
        private static readonly object _lock = new();

        internal static void Register(string name, int hash)
        {
            lock (_lock)
            {
                if (!_nameToHash.ContainsKey(name))
                    _nameToHash[name] = hash;

                if (!_hashToName.ContainsKey(hash))
                    _hashToName[hash] = name;
            }
        }

        internal static bool TryGetHash(string name, out int hash)
            => _nameToHash.TryGetValue(name, out hash);

        internal static bool TryGetName(int hash, out string name)
            => _hashToName.TryGetValue(hash, out name!);
    }
}