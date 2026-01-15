namespace Fluence.Global
{
    /// <summary>
    /// An immutable record holding metadata about a single function or method.
    /// </summary>
    internal sealed record class MethodMetadata(string Name, int Arity, bool IsCtor, IReadOnlyList<string> Parameters, int RefMask)
    {
        /// <summary>
        /// The user-facing, unmangled name of the method.
        /// </summary>
        internal string BaseName => Name;

        /// <summary>
        /// The mangled BaseName of the method used internally.
        /// </summary>
        internal string MangledName => Mangler.Mangle(Name, Arity);

        /// <summary>
        /// Generates a user-friendly signature string.
        /// </summary>
        internal string GetSignature()
        {
            List<string> parts = new List<string>();
            for (int i = 0; i < Parameters.Count; i++)
            {
                bool isRef = (RefMask & (1 << i)) != 0;
                string p = Parameters[i];
                parts.Add(isRef ? $"ref {p}" : p);
            }
            return $"func {BaseName}({string.Join(", ", parts)})";
        }

        public bool Equals(MethodMetadata? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return MangledName == other.MangledName &&
                   Arity == other.Arity &&
                   RefMask == other.RefMask &&
                   Parameters.SequenceEqual(other.Parameters);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(MangledName);
            hash.Add(Arity);
            hash.Add(RefMask);
            foreach (string p in Parameters) hash.Add(p);
            return hash.ToHashCode();
        }
    }
}