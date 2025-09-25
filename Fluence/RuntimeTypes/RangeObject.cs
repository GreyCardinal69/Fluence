namespace Fluence.RuntimeTypes
{
    /// <summary>
    /// Represents a heap-allocated range object, typically used in 'for-in' loops.
    /// </summary>
    internal sealed record class RangeObject
    {
        internal RuntimeValue Start { get; private set; }
        internal RuntimeValue End { get; private set; }

        internal RangeObject(RuntimeValue start, RuntimeValue end)
        {
            Start = start;
            End = end;
        }

        public RangeObject() { }

        internal void Reset()
        {
            Start = default;
            End = default;
        }

        internal void Initialize(RuntimeValue start, RuntimeValue end)
        {
            Start = start;
            End = end;
        }

        public override string ToString() => $"{Start}..{End}";
    }
}