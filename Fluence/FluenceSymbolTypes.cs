namespace Fluence
{
    /// <summary>
    /// Represents a named entity in the source code that is declared at a scope level.
    /// This is the abstract base class for functions, structs, and enums.
    /// It inherits from <see cref="Value"/> to be storable in the bytecode stream if needed (e.g., for NewInstance).
    /// </summary>
    internal abstract record class Symbol : Value { }

    /// <summary>
    /// Represents a global variable of a scope with a complex expression value.
    /// </summary>
    internal sealed record class VariableSymbol : Symbol
    {
        /// <summary>
        /// The name of the global variable.
        /// </summary>
        internal string Name { get; init; }

        /// <summary>
        /// The dynamic value of the variable.
        /// </summary>
        internal Value Value { get; set; }

        internal VariableSymbol(string name, Value value)
        {
            Name = name;
            Value = value;
        }

        internal override string ToFluenceString() => $"VariableSymbol: {Name}{Value}";

        public override string ToString()
        {
            return $"VariableSymbol: {Name}{Value}";
        }
    }

    /// <summary>
    /// Represents an enum declaration. It contains the enum's name and a collection of its members.
    /// </summary>
    internal sealed record class EnumSymbol : Symbol
    {
        /// <summary>
        /// The name of the enum.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// The dictionary mapping member names to their corresponding <see cref="EnumValue"/>s.
        /// </summary>
        internal Dictionary<string, EnumValue> Members { get; } = new();

        internal EnumSymbol(string name)
        {
            Name = name;
        }

        internal override string ToFluenceString()
        {
            return $"<internal: enum_symbol>";
        }
    }

    /// <summary>
    /// Represents a struct declaration. It contains the struct's name, fields, methods, and constructor information.
    /// </summary>
    internal sealed record class StructSymbol : Symbol
    {
        /// <summary>
        /// The name of the struct.
        /// </summary>
        internal string Name { get; init; }

        /// <summary>
        /// The list of declared field names.
        /// </summary>
        internal List<string> Fields { get; init; } = new();

        /// <summary>
        /// Gets a dictionary of methods defined within the struct, mapping method names to their <see cref="FunctionValue"/>s.
        /// </summary>
        internal Dictionary<string, FunctionValue> Functions { get; init; } = new();

        /// <summary>
        /// Gets a dictionary mapping field names to the sequence of tokens representing their default value expression.
        /// This is populated during the pre-pass and used during the main pass to generate constructor bytecode.
        /// </summary>
        internal Dictionary<string, List<Token>> DefaultFieldValuesAsTokens { get; init; } = new();

        /// <summary>
        /// Gets or sets the constructor function (`init`) for this struct.
        /// This can be null if no explicit constructor is defined.
        /// </summary>
        internal FunctionValue Constructor { get; set; }

        internal StructSymbol(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"StructSymbol<{Name}>";
        }

        internal override string ToFluenceString()
        {
            return $"<internal: struct_symbol>";
        }
    }

    /// <summary>
    /// Defines the delegate signature for a native C# method that can be called from Fluence script.
    /// </summary>
    /// <param name="args">A read-only list of <see cref="Value"/> objects passed as arguments to the function.</param>
    /// <returns>The <see cref="Value"/> that the native method returns.</returns>
    internal delegate Value IntrinsicMethod(IReadOnlyList<Value> args);

    /// <summary>
    /// Represents a function or method declaration. It can be a user-defined Fluence function
    /// with a bytecode address or a native C# intrinsic function with a delegate.
    /// </summary>
    internal sealed record class FunctionSymbol : Symbol
    {
        /// <summary>
        /// The name of the function.
        /// </summary>
        internal string Name { get; init; }

        /// <summary>
        /// The number of parameters the function is declared to accept (excluding the implicit 'self' for methods).
        /// </summary>
        internal int Arity { get; init; }

        /// <summary>
        /// The starting address of the function's bytecode. For intrinsics, this is -1.
        /// </summary>
        internal int StartAddress { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this function is a native C# intrinsic.
        /// </summary>
        internal bool IsIntrinsic { get; init; }

        /// <summary>
        /// If this is an intrinsic function, gets the C# delegate that implements its logic.
        /// </summary>
        internal IntrinsicMethod IntrinsicBody { get; init; }

        internal List<string> Arguments { get; init; }

        /// <summary>
        /// Keeps track in which namespace the function is defined in.
        /// </summary>
        internal FluenceScope DefiningScope { get; init; }

        /// <summary>
        /// Sets the bytecode start address for this function. Called by the parser during the second pass.
        /// </summary>
        internal void SetStartAddress(int addr) => StartAddress = addr;

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionSymbol"/> class for a native C# intrinsic.
        /// </summary>
        /// <param name="name">The name of the intrinsic function.</param>
        /// <param name="arity">The number of arguments the function expects.</param>
        /// <param name="body">The C# delegate that executes the function's logic.</param>
        internal FunctionSymbol(string name, int arity, IntrinsicMethod body, List<string> arguments = null!, FluenceScope definingScope = null!)
        {
            Name = name;
            Arity = arity;
            StartAddress = -1; // Special address for intrinsics
            IsIntrinsic = true;
            IntrinsicBody = body;
            Arguments = arguments;
            DefiningScope = definingScope;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionSymbol"/> class for a user-defined Fluence function.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <param name="arity">The number of arguments the function expects.</param>
        /// <param name="startAddress">The initial start address (usually -1, resolved later).</param>
        internal FunctionSymbol(string name, int arity, int startAddress, List<string> arguments = null!, FluenceScope definingScope = null!)
        {
            Name = name;
            Arity = arity;
            StartAddress = startAddress;
            IsIntrinsic = false;
            Arguments = arguments;
            DefiningScope = definingScope;
        }

        internal override string ToFluenceString()
        {
            return $"<internal: function_symbol>";
        }
    }
}