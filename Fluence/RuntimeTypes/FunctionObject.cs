using System.Text;

namespace Fluence.RuntimeTypes
{
    /// <summary>
    /// Represents the runtime instance of a function, containing all information needed
    /// to execute it.
    /// </summary>
    internal sealed record class FunctionObject
    {
        /// <summary>The name of the function.</summary>
        internal string Name { get; private set; }

        /// <summary>The number of parameters the function expects.</summary>
        internal int Arity { get; private set; }

        /// <summary>The instruction pointer address where the function's bytecode begins.</summary>
        internal int StartAddress { get; private set; }

        /// <summary>
        /// Sets the bytecode start address for this function. Called by the parser during the second pass.
        /// </summary>
        internal int StartAddressInSource { get; private set; }

        /// <summary>
        /// Does the current function object belong to a lambda variable, if true it is not
        /// returned to a pool upon completion of its 'Return' instruction.
        /// </summary>
        internal bool IsLambda { get; set; }

        /// <summary>
        /// The total amount of register slots this function requires to execute its bytecode.
        /// </summary>
        internal int TotalRegisterSlots { get; set; }

        /// <summary>
        /// The address of the last instruction of the function's body in the bytecode.
        /// </summary>
        internal int EndAddress { get; set; }

        /// <summary>The names of the function's parameters.</summary>
        internal List<string> Arguments { get; private set; }

        /// <summary>The hash codes of the function's parameters.</summary>
        internal List<int> ArgumentHashCodes { get; private set; }

        /// <summary>The names of the function's parameters passed by reference.</summary>
        internal HashSet<string> ArgumentsByRef { get; set; }

        /// <summary>The lexical scope in which the function was defined, used for resolving non-local variables.</summary>
        internal FluenceScope DefiningScope { get; private set; }

        /// <summary> A direct reference to the immutable, function symbol that defines this function. </summary>
        internal FunctionSymbol? BluePrint { get; private set; }

        /// <summary>Indicates whether this function is implemented in C# (intrinsic) or Fluence bytecode.</summary>
        internal bool IsIntrinsic { get; private set; }

        /// <summary>The C# delegate that implements the body of an intrinsic function.</summary>
        internal IntrinsicMethod IntrinsicBody { get; private set; }

        /// <summary>
        /// Indicates whether the function is an instance or a static method of some struct type.
        /// </summary>
        internal bool BelongsToAStruct { get; private set; }

        internal FunctionObject(string name, int arity, List<string> parameters, List<int> parametersHash, int startAddress, FluenceScope definingScope)
        {
            Name = name;
            Arity = arity;
            Arguments = parameters;
            ArgumentHashCodes = parametersHash;
            StartAddress = startAddress;
            DefiningScope = definingScope;
            IsIntrinsic = false;
        }

        /// <summary>
        /// Public parameterless constructor required for object pooling.
        /// </summary>
        public FunctionObject() { }

        internal void Initialize(FunctionValue function)
        {
            BelongsToAStruct = function.BelongsToAStruct;
            Name = function.Name;
            Arity = function.Arity;
            Arguments = function.Arguments;
            ArgumentHashCodes = function.ArgumentHashCodes;
            StartAddress = function.StartAddress;
            DefiningScope = function.DefiningScope;
            StartAddressInSource = function.StartAddressInSource;
            ArgumentsByRef = function.ArgumentsByRef;
            TotalRegisterSlots = function.TotalRegisterSlots;
            EndAddress = function.EndAddress;
        }

        internal void Initialize(FunctionSymbol function)
        {
            BelongsToAStruct = function.BelongsToAStruct;
            Name = function.Name;
            Arity = function.Arity;
            Arguments = function.Arguments;
            ArgumentHashCodes = function.ArgumentHashCodes;
            StartAddress = function.StartAddress;
            DefiningScope = function.DefiningScope;
            BluePrint = function;
            StartAddressInSource = function.StartAddressInSource;
            ArgumentsByRef = function.ArgumentsByRef;
            TotalRegisterSlots = function.TotalRegisterSlots;
            EndAddress = function.EndAddress;
        }

        internal void Initialize(string name, int arity, IntrinsicMethod body, FluenceScope definingScope, FunctionSymbol symb)
        {
            StartAddressInSource = symb != null ? symb.StartAddressInSource : 0;
            IntrinsicBody = body;
            Name = name;
            Arity = arity;
            DefiningScope = definingScope;
            IsIntrinsic = true;
            BluePrint = symb;
        }

        internal string ToCodeLikeString()
        {
            StringBuilder sb = new StringBuilder($"func {Mangler.Demangle(Name)}(");
            for (int i = 0; i < Arguments?.Count; i++)
            {
                string arg = Arguments[i];
                if (ArgumentsByRef.Contains(arg))
                {
                    sb.Append($"ref {arg}");
                }
                else
                {
                    sb.Append(arg);
                }
                if (i < Arguments.Count - 1) sb.Append(", ");
            }
            sb.Append(") => ...");
            return sb.ToString();
        }

        public override string ToString() => $"<function {Name}/{Arity}>";
    }
}