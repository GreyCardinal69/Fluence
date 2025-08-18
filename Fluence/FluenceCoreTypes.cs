namespace Fluence
{
    /// <summary>
    /// Represents a complex object in the Fluence runtime.
    /// This is the abstract base class for mutable reference types like lists, maps, and struct instances.
    /// </summary>
    internal abstract record class ObjectValue : Value;

    /// <summary>
    /// Represents a list instance in the Fluence runtime.
    /// This class wraps a standard C# List to provide list functionality within the VM.
    /// </summary>
    /// <param name="Elements">The mutable list of <see cref="Value"/> objects that this list contains.</param>
    internal sealed record class ListValue(List<Value> Elements) : ObjectValue
    {
        /// <summary>
        /// Provides a user-friendly string representation of the list, suitable for the `print` function.
        /// </summary>
        /// <returns>A string in the format "[element1, element2, ...]".</returns>
        public override string ToString()
        {
            // Limit the number of elements shown for very large lists to avoid flooding the console.
            const int maxElementsToShow = 20;
            var elementsToShow = Elements.Take(maxElementsToShow);
            string formattedElements = string.Join(", ", elementsToShow);

            if (Elements.Count > maxElementsToShow)
            {
                formattedElements += $", ... ({Elements.Count - maxElementsToShow} more)";
            }

            return $"[{formattedElements}]";
        }
    }
}