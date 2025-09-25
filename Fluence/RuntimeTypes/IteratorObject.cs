namespace Fluence.RuntimeTypes
{
    /// <summary>
    /// Represents the state of an ongoing iteration over an iterable object.
    /// </summary>
    internal sealed record class IteratorObject
    {
        /// <summary>The object being iterated over.</summary>
        internal object? Iterable { get; private set; }

        /// <summary>The current position within the iteration./summary>
        internal int CurrentIndex { get; set; }

        internal IteratorObject(object iterable)
        {
            Iterable = iterable;
            CurrentIndex = 0;
        }

        public IteratorObject() { }

        internal void Reset()
        {
            Iterable = null;
            CurrentIndex = 0;
        }

        internal void Initialize(object? iterator)
        {
            Iterable = iterator;
            CurrentIndex = 0;
        }
    }
}