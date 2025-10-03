namespace Fluence.Global
{
    /// <summary>
    /// An immutable record holding metadata about a single function or method.
    /// </summary>
    internal sealed record class MethodMetadata(string Name, int Arity, bool IsCtor, IReadOnlyList<string> Parameters, IReadOnlySet<string> RefParameters)
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
            IEnumerable<string> paramStrings = Parameters.Select(p => RefParameters.Contains(p) ? $"ref {p}" : p);
            return $"func {BaseName}({string.Join(", ", paramStrings)})";
        }

        public bool Equals(MethodMetadata? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return MangledName == other.MangledName &&
                   Arity == other.Arity &&
                   Parameters.SequenceEqual(other.Parameters) &&
                   RefParameters.SetEquals(other.RefParameters);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(MangledName);
            hash.Add(Arity);
            foreach (string p in Parameters) hash.Add(p);
            foreach (string r in RefParameters) hash.Add(r);
            return hash.ToHashCode();
        }
    }
}