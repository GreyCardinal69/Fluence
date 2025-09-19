namespace Fluence
{
    /// <summary>
    /// Represents a named entity in the source code that is declared at a scope level.
    /// This is the abstract base class for functions, structs, and enums.
    /// It inherits from <see cref="Value"/> to be storable in the bytecode stream.
    /// </summary>
    internal abstract record class Symbol : Value { }

    /// <summary>
    /// Represents a variable of a scope.
    /// </summary>
    internal sealed record class VariableSymbol : Symbol
    {
        /// <summary>
        /// The name of the variable.
        /// </summary>
        internal string Name { get; init; }

        /// <summary>
        /// The value of the variable.
        /// </summary>
        internal Value Value { get; set; }

        /// <summary>
        /// Indicates that the variable is marked as 'solid' readonly variable.
        /// </summary>
        internal bool IsReadonly { get; init; }

        internal VariableSymbol(string name, Value value, bool readOnly = false)
        {
            Name = name;
            Value = value;
            IsReadonly = readOnly;
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
        internal string Name { get; init; }

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
        internal List<string> Fields { get; } = new();

        /// <summary>
        /// The static and solid fields of a struct.
        /// </summary>
        internal Dictionary<string, RuntimeValue> StaticFields { get; } = new();

        /// <summary>
        /// Stores natively implemented static intrinsic methods.
        /// </summary>
        public Dictionary<string, FunctionSymbol> StaticIntrinsics { get; } = new();

        /// <summary>
        /// Gets a dictionary of methods defined within the struct, mapping method names to their <see cref="FunctionValue"/>s.
        /// </summary>
        internal Dictionary<string, FunctionValue> Functions { get; } = new();

        /// <summary>
        /// Gets a dictionary mapping field names to the sequence of tokens representing their default value expression.
        /// This is populated during the pre-pass and used during the main pass to generate constructor bytecode.
        /// </summary>
        internal Dictionary<string, List<Token>> DefaultFieldValuesAsTokens { get; } = new();

        /// <summary>
        /// Gets or sets the constructor function (`init`) for this struct.
        /// This can be null if no explicit constructor is defined.
        /// </summary>
        internal Dictionary<string, FunctionValue> Constructors { get; } = new();

        internal StructSymbol()
        {
        }

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
    internal delegate RuntimeValue IntrinsicMethod(FluenceVirtualMachine vm, int argCount);

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
        /// The line in the source file where the function is defined, pointing to the line where the function name is declared.
        /// </summary>
        internal int StartAddressInSource { get; init; }

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
        internal FunctionSymbol(string name, int arity, int startAddress, int lineInSource, List<string> arguments = null!, FluenceScope definingScope = null!)
        {
            StartAddressInSource = lineInSource;
            Name = name;
            Arity = arity;
            StartAddress = startAddress;
            IsIntrinsic = false;
            Arguments = arguments;
            DefiningScope = definingScope;
        }

        public override string ToString()
        {
            string args = (Arguments == null || Arguments.Count == 0) ? "None" : string.Join(",", Arguments);
            return $"FunctionSymbol: {Name}, Intrinsic:{IsIntrinsic}, {FluenceDebug.FormatByteCodeAddress(StartAddress)}, #{Arity} args: {args}, LocationInSource: {StartAddressInSource}.";
        }

        internal override string ToFluenceString()
        {
            return $"<internal: function_symbol>";
        }
    }
}