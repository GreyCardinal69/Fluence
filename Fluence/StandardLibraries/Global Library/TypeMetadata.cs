namespace Fluence.Global
{
    internal sealed class TypeMetadata
    {
        /// <summary> The user-facing name of the type. </summary>
        internal string Name { get; }

        /// <summary>
        /// A list of the names of the fields for a struct type.
        /// </summary>
        internal IReadOnlyList<string> Fields { get; }

        /// <summary>
        /// A list of the mangled names of the methods for a struct type.
        /// </summary>
        internal IReadOnlyList<string> Methods { get; }

        internal TypeMetadata(string name, IReadOnlyList<string>? fields = null, IReadOnlyList<string>? methods = null)
        {
            Name = name;
            Fields = fields ?? [];
            Methods = methods ?? [];
        }

        public override string ToString() => $"<internal: Type_Metadata__{Name}>";
    }
}