using System;

namespace Fluence
{
    internal readonly struct SymbolKey : IEquatable<SymbolKey>
    {
        internal readonly int NameHash;
        internal readonly int Arity;

        internal SymbolKey(string name, int arity)
        {
            NameHash = name.GetHashCode();
            Arity = arity;
        }

        internal SymbolKey(int nameHash, int arity)
        {
            NameHash = nameHash;
            Arity = arity;
        }

        public bool Equals(SymbolKey other)
        {
            return NameHash == other.NameHash && Arity == other.Arity;
        }

        public override bool Equals(object? obj)
        {
            return obj is SymbolKey key && Equals(key);
        }

        public override int GetHashCode()
        {
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
            return HashCode.Combine(NameHash, Arity);
#else
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + NameHash;
                hash = hash * 23 + Arity;
                return hash;
            }
#endif
        }

        public static bool operator ==(SymbolKey left, SymbolKey right) => left.Equals(right);
        public static bool operator !=(SymbolKey left, SymbolKey right) => !left.Equals(right);

        public override string ToString() => $"Key({NameHash}, {Arity})";
    }
}