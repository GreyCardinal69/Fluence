using Fluence.RuntimeTypes;
using Fluence.VirtualMachine;
using System.Runtime.CompilerServices;

namespace Fluence
{
    /// <summary>
    /// Represents a named entity in the source code that is declared at a scope level.
    /// This is the abstract base class for functions, structs, and enums.
    /// It inherits from <see cref="Value"/> to be storable in the bytecode stream.
    /// </summary>
    internal abstract record class Symbol : Value
    {
        /// <summary>
        /// The declared name of this symbol.
        /// Always non-null for any valid symbol.
        /// </summary>
        internal string Name { get; init; }

        protected Symbol(string name)
        {
            Name = name;
            Hash = Name.GetHashCode();
        }
    }

    /// <summary>
    /// Represents a declaration of an intrinsic struct symbol with a set of traits it implements.
    /// </summary>
    internal sealed record class IntrinsicStructSymbol : Symbol
    {
        /// <summary>
        /// A set of pre-calculated hash codes representing the traits this struct implements, populated by the parser at compile-time after verifying that
        /// the struct correctly fulfills all trait contracts.
        /// </summary>
        internal HashSet<int> ImplementedTraits { get; } = new();

        internal IntrinsicStructSymbol(string name) : base(name)
        {

        }

        internal override string ToFluenceString() => "<internal: intrinsic__struct__symbol>";

        public override string ToString() => "IntrinsicStructSymbol";
    }

    /// <summary>
    /// Represents a variable of a scope.
    /// </summary>
    internal sealed record class VariableSymbol : Symbol
    {
        /// <summary>The value of the variable.</summary>
        internal Value Value { get; init; }

        /// <summary>
        /// Indicates that the variable is marked as 'solid' as in, a readonly variable.
        /// </summary>
        internal bool IsReadonly { get; init; }

        internal VariableSymbol(string name, Value value, bool readOnly = false) : base(name)
        {
            Value = value;
            IsReadonly = readOnly;
        }

        internal override string ToFluenceString() => $"VariableSymbol: {Name}---{Value}";

        public override string ToString() => $"VariableSymbol: {Name}---{Value}";
    }

    /// <summary>
    /// Represents an enum declaration. It contains the enum's name and a collection of its members.
    /// </summary>
    internal sealed record class EnumSymbol : Symbol
    {
        /// <summary>
        /// The dictionary mapping member names to their corresponding <see cref="EnumValue"/>s.
        /// </summary>
        internal Dictionary<string, EnumValue> Members { get; } = new();

        internal EnumSymbol(string name) : base(name) { }

        internal override string ToFluenceString() => $"<internal: enum_symbol>";

        public override string ToString() => $"EnumSymbol: {Name}-{Members}";
    }

    /// <summary>
    /// Represents a trait encapsulating field and function signatures that classes inherit,
    /// along with default field values and static fields.
    /// </summary>
    internal sealed record class TraitSymbol : Symbol
    {
        /// <summary>
        /// Defines the signature of a function within the trait, including its name, hash, and arity.
        /// </summary>
        internal readonly record struct FunctionSignature
        {
            /// <summary>The name of the function.</summary>
            internal string Name { get; init; }

            /// <summary>The hash code associated with the function signature.</summary>
            internal int Hash { get; init; }

            /// <summary>The arity of the function.</summary>
            internal int Arity { get; init; }

            /// <summary>Indicates whether the function signature is that of a constructor.</summary>
            internal bool IsAConstructor { get; init; }
        }

        /// <summary>Contains the trait's fields' names.</summary>
        internal Dictionary<int, string> FieldSignatures { get; init; }

        /// <summary>Contains the trait's functions' signatures.</summary>
        internal Dictionary<int, FunctionSignature> FunctionSignatures { get; init; }

        /// <summary>
        /// Gets a dictionary mapping field names to the sequence of tokens representing their default value expression.
        /// This is populated during the pre-pass and used during the main pass to generate constructor bytecode.
        /// </summary>
        internal Dictionary<string, List<Token>> DefaultFieldValuesAsTokens { get; } = new();

        /// <summary>The static solid fields of the trait.</summary>
        internal Dictionary<string, RuntimeValue> StaticFields { get; } = new();

        internal TraitSymbol(string name) : base(name)
        {
            FieldSignatures = new Dictionary<int, string>();
            FunctionSignatures = new Dictionary<int, FunctionSignature>();
        }

        internal override string ToFluenceString() => "<internal: trait_symbol>";

        public override string ToString() => $"TraitSymbol<{Name}>";
    }

    /// <summary>
    /// Represents a struct declaration. It contains the struct's name, fields, methods, and constructor information.
    /// </summary>
    internal sealed record class StructSymbol : Symbol
    {
        /// <summary>The scope the struct belongs to.</summary>
        internal FluenceScope Scope { get; init; }

        /// <summary>The list of declared field names.</summary>
        internal List<string> Fields { get; } = new();

        /// <summary>The static solid fields of a struct.</summary>
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

        /// <summary>
        /// A set of pre-calculated hash codes representing the traits this struct implements, populated by the parser at compile-time after verifying that
        /// the struct correctly fulfills all trait contracts.
        /// </summary>
        internal HashSet<int> ImplementedTraits { get; } = new();

        internal StructSymbol(string name, FluenceScope scope) : base(name)
        {
            Scope = scope;
        }

        internal override string ToFluenceString() => $"<internal: struct_symbol>";

        public override string ToString() => $"StructSymbol<{Name}>";
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
        /// The number of parameters the function is declared to accept (excluding the implicit 'self' for methods).
        /// </summary>
        internal int Arity { get; init; }

        /// <summary>
        /// The starting address of the function's bytecode. For intrinsics, this is -1.
        /// </summary>
        internal int StartAddress { get; private set; }

        /// <summary>
        /// The address of the last instruction of the function's body in the bytecode.
        /// </summary>
        internal int EndAddress { get; private set; }

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
        internal IntrinsicMethod? IntrinsicBody { get; init; }

        /// <summary>
        /// The arguments of the function by name.
        /// </summary>
        internal List<string> Arguments { get; init; }

        /// <summary>The hash codes of the function's arguments.</summary>
        internal List<int> ArgumentHashCodes { get; private set; }

        /// <summary>
        /// The arguments of the function passed by reference by name.
        /// </summary>
        internal HashSet<string> ArgumentsByRef { get; private set; }

        /// <summary>
        /// A list of the register slot indices corresponding to the function's parameters, in order.
        /// This is populated by the parser after the slot allocation pass.
        /// </summary>
        internal int[] ArgumentRegisterIndices { get; private set; }

        /// <summary>
        /// Keeps track which namespace the function is defined in.
        /// </summary>
        internal FluenceScope DefiningScope { get; init; }

        /// <summary>
        /// The total amount of register slots this function requires to execute its bytecode.
        /// </summary>
        internal int TotalRegisterSlots { get; set; }

        /// <summary>
        /// Indicates whether the function is an instance or a static method of some struct type.
        /// </summary>
        internal bool BelongsToAStruct { get; set; }

        /// <summary>
        /// Sets the bytecode start address for this function. Called by the parser during the second pass.
        /// </summary>
        internal void SetStartAddress(int addr) => StartAddress = addr;

        /// <summary>
        /// Sets the bytecode end address for this function. Called by the parser during the second pass.
        /// This is usually the final return instruction of the function's body.
        /// </summary>
        internal void SetEndAddress(int adr) => EndAddress = adr;

        /// <summary>Sets the register indices of this functions arguments.</summary>>
        internal void SetArgumentRegisterIndices(int[] indices) => ArgumentRegisterIndices = indices;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetStartAndEndAddresses(int start, int end)
        {
            StartAddress = start;
            EndAddress = end;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionSymbol"/> class for a native C# intrinsic.
        /// </summary>
        /// <param name="name">The name of the intrinsic function.</param>
        /// <param name="arity">The number of arguments the function expects.</param>
        /// <param name="body">The C# delegate that executes the function's logic.</param>
        internal FunctionSymbol(string name, int arity, IntrinsicMethod body, FluenceScope definingScope, List<string> arguments) : base(name)
        {
            Arity = arity;
            StartAddress = -1; // Special address for intrinsics.
            IsIntrinsic = true;
            IntrinsicBody = body;
            Arguments = arguments;

            ArgumentHashCodes = new List<int>();
            for (int i = 0; i < Arguments.Count; i++)
            {
                ArgumentHashCodes.Add(Arguments[i].GetHashCode());
            }

            DefiningScope = definingScope;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionSymbol"/> class for a user-defined Fluence function.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <param name="arity">The number of arguments the function expects.</param>
        /// <param name="startAddress">The initial start address (usually -1, resolved later).</param>
        internal FunctionSymbol(string name, int arity, int startAddress, int lineInSource, FluenceScope definingScope, List<string> arguments, HashSet<string> argumentsByRef) : base(name)
        {
            StartAddressInSource = lineInSource;
            Arity = arity;
            StartAddress = startAddress;
            IsIntrinsic = false;
            Arguments = arguments;

            ArgumentHashCodes = new List<int>();
            for (int i = 0; i < Arguments.Count; i++)
            {
                ArgumentHashCodes.Add(Arguments[i].GetHashCode());
            }

            ArgumentsByRef = argumentsByRef;
            DefiningScope = definingScope;
        }

        internal override string ToFluenceString() => $"<internal: function_symbol>";

        public override string ToString()
        {
            string args = (Arguments == null || Arguments.Count == 0) ? "None" : string.Join(",", Arguments);
            return $"FunctionSymbol: {Name}, Intrinsic:{IsIntrinsic}, {FluenceDebug.FormatByteCodeAddress(StartAddress)}, #{Arity} args: {args}, LocationInSource: {StartAddressInSource}.";
        }
    }
}