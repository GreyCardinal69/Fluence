using Fluence.RuntimeTypes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fluence
{
    /// <summary>
    /// Manages a lexical scope, holding a table of symbols and a reference to its parent scope.
    /// </summary>
    internal sealed class FluenceScope
    {
        /// <summary>
        /// Gets the collection of symbols declared directly within this scope.
        /// </summary>
        internal readonly Dictionary<string, Symbol> Symbols = new Dictionary<string, Symbol>();

        /// <summary>
        /// Gets the name of this scope, primarily used for debugging and error messages.
        /// </summary>
        internal readonly string Name;

        /// <summary>Indicates whether this scope is the global scope.</summary>
        internal readonly bool IsTheGlobalScope;

        /// <summary>
        /// Keeps track of declared symbol names for name conflict detection.
        /// </summary>
        internal readonly HashSet<string> DeclaredSymbolNames = new HashSet<string>();

        /// <summary>
        /// Gets the parent scope in the hierarchy. This is null for the global scope.
        /// </summary>
        internal FluenceScope ParentScope { get; init; }

        /// <summary>
        /// The runtime storage for this scope's global variables.
        /// This is used by the VM to store the actual RuntimeValues.
        /// </summary>
        internal readonly Dictionary<int, RuntimeValue> RuntimeStorage = new Dictionary<int, RuntimeValue>();

        // Used in Tests. Might also be useful for other purposes.
        internal bool Contains(string name) => TryResolve(name, out _);
        internal bool ContainsLocal(string name) => TryGetLocalSymbol(name, out _);
        internal bool TryGetLocalSymbol(string name, out Symbol symbol) => Symbols.TryGetValue(name, out symbol!);

        internal FluenceScope(FluenceScope parentScope, string name)
        {
            ParentScope = parentScope;
            Name = name;
            IsTheGlobalScope = string.Equals(name, "Global", StringComparison.Ordinal);
        }

        /// <summary>
        /// Declares a new symbol directly in this scope.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="symbol">The symbol to declare.</param>
        /// <returns>True if the symbol was declared successfully; false if a symbol with the same name already exists in this scope.</returns>
        internal bool Declare(string name, Symbol symbol)
        {
            if (Symbols.TryAdd(name, symbol))
            {
                DeclaredSymbolNames.Add(name);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to resolve a symbol by searching this scope and, if not found, recursively searching all parent scopes.
        /// </summary>
        /// <param name="name">The name of the symbol to find.</param>
        /// <param name="symbol">When this method returns, contains the found symbol, or null if it was not found.</param>
        /// <returns>True if the symbol was found in this scope or any parent scope; otherwise, false.</returns>
        internal bool TryResolve(string name, out Symbol symbol)
        {
            FluenceScope current = this;
            while (current != null)
            {
                ref Symbol localSymbol = ref CollectionsMarshal.GetValueRefOrNullRef(current.Symbols, name);
                if (!Unsafe.IsNullRef(ref localSymbol))
                {
                    symbol = localSymbol;
                    return true;
                }
                current = current.ParentScope;
            }
            symbol = null!;
            return false;
        }

        public override string ToString() => $"Scope: {Name}";
    }
}