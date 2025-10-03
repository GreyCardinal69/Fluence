using Fluence.RuntimeTypes;

namespace Fluence.Global
{
    internal sealed record class ParameterMetadata
    {
        internal string Name { get; init; }
        internal bool ByRef { get; init; }

        // TO DO
        internal bool HasDefaultValue { get; init; }
        internal RuntimeValue DefualtValue { get; init; }
    }
}