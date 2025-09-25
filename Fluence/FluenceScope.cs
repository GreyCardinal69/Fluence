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
        internal string Name { get; private set; }

        internal readonly List<string> UsedScopes = new List<string>();

        /// <summary>
        /// Gets the parent scope in the hierarchy. This is null for the global scope.
        /// </summary>
        internal FluenceScope ParentScope { get; init; }

        /// <summary>
        /// The runtime storage for this scope's global variables.
        /// This is used by the VM to store the actual RuntimeValues.
        /// </summary>
        internal readonly Dictionary<string, RuntimeValue> RuntimeStorage = new Dictionary<string, RuntimeValue>();

        internal bool Contains(string name) => TryResolve(name, out _);
        internal bool ContainsLocal(string name) => TryGetLocalSymbol(name, out _);

        internal FluenceScope()
        {
            ParentScope = null!;
        }

        internal FluenceScope(FluenceScope parentScope, string name)
        {
            ParentScope = parentScope;
            Name = name;
        }

        /// <summary>
        /// Declares a new symbol directly in this scope.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="symbol">The symbol to declare.</param>
        /// <returns>True if the symbol was declared successfully; false if a symbol with the same name already exists in this scope.</returns>
        internal bool Declare(string name, Symbol symbol)
        {
            if (DeclareInternal(name, symbol))
            {
                UsedScopes.Add(name);
                return true;
            }

            return false;
        }

        private bool DeclareInternal(string name, Symbol symbol)
        {
            return Symbols.TryAdd(name, symbol);
        }

        /// <summary>
        /// Tries to resolve a symbol by searching this scope and, if not found, recursively searching all parent scopes.
        /// </summary>
        /// <param name="name">The name of the symbol to find.</param>
        /// <param name="symbol">When this method returns, contains the found symbol, or null if it was not found.</param>
        /// <returns>True if the symbol was found in this scope or any parent scope; otherwise, false.</returns>
        internal bool TryResolve(string name, out Symbol symbol)
        {
            ref Symbol localSymbol = ref CollectionsMarshal.GetValueRefOrNullRef(Symbols, name);

            if (!Unsafe.IsNullRef(ref localSymbol))
            {
                symbol = localSymbol;
                return true;
            }

            if (ParentScope != null)
            {
                return ParentScope.TryResolve(name, out symbol);
            }

            symbol = null!;
            return false;
        }

        public override string ToString()
        {
            return $"Scope: {Name}";
        }

        // This scope.
        internal bool TryGetLocalSymbol(string name, out Symbol symbol) => Symbols.TryGetValue(name, out symbol!);
    }
}