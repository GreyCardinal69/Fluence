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

        /// <summary>The names of the function's parameters.</summary>
        internal List<string> Parameters { get; private set; }

        /// <summary>The hash codes of the function's parameters.</summary>
        internal List<int> ParametersHash { get; private set; }

        /// <summary>The names of the function's parameters passed by reference.</summary>
        internal HashSet<string> ParametersByRef { get; set; }

        /// <summary>The lexical scope in which the function was defined, used for resolving non-local variables.</summary>
        internal FluenceScope DefiningScope { get; private set; }

        /// <summary> A direct reference to the immutable, function symbol that defines this function. </summary>
        internal FunctionSymbol? BluePrint { get; private set; }

        /// <summary>Indicates whether this function is implemented in C# (intrinsic) or Fluence bytecode.</summary>
        internal bool IsIntrinsic { get; private set; }

        /// <summary>The C# delegate that implements the body of an intrinsic function.</summary>
        internal IntrinsicMethod IntrinsicBody { get; private set; }

        internal FunctionObject(string name, int arity, List<string> parameters, List<int> parametersHash, int startAddress, FluenceScope definingScope)
        {
            Name = name;
            Arity = arity;
            Parameters = parameters;
            ParametersHash = parametersHash;
            StartAddress = startAddress;
            DefiningScope = definingScope;
            IsIntrinsic = false;
        }

        // Constructor for C# intrinsic functions.
        internal FunctionObject(string name, int arity, IntrinsicMethod body, FluenceScope definingScope)
        {
            Name = name;
            Arity = arity;
            StartAddress = -1; // No bytecode address.
            Parameters = new List<string>();
            ParametersHash = new List<int>();
            DefiningScope = definingScope;
            IsIntrinsic = true;
            IntrinsicBody = body;
        }

        /// <summary>
        /// Public parameterless constructor required for object pooling.
        /// </summary>
        public FunctionObject() { }

        internal void Initialize(string name, int arity, List<string> parameters, List<int> parametersHash, int startAddress, FluenceScope definingScope, FunctionSymbol symb, int lineInSource)
        {
            StartAddressInSource = lineInSource;
            Name = name;
            Arity = arity;
            Parameters = parameters;
            ParametersHash = parametersHash;
            StartAddress = startAddress;
            DefiningScope = definingScope;
            IsIntrinsic = false;
            BluePrint = symb;
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

        internal void SetBluePrint(FunctionSymbol? symb) => BluePrint = symb;

        internal void Reset()
        {
            StartAddressInSource = 0;
            BluePrint = null;
            Name = null!;
            Arity = 0;
            Parameters = null!;
            StartAddress = 0;
            DefiningScope = null!;
            IsIntrinsic = false;
            ParametersHash = null!;
            ParametersByRef = null!;
        }

        public override string ToString() => $"<function {Name}/{Arity}>";

        internal string ToCodeLikeString()
        {
            StringBuilder sb = new StringBuilder($"func {Mangler.Demangle(Name)}(");
            for (int i = 0; i < Parameters?.Count; i++)
            {
                string arg = Parameters[i];
                if (ParametersByRef.Contains(arg))
                {
                    sb.Append($"ref {arg}");
                }
                else
                {
                    sb.Append(arg);
                }
                if (i < Parameters.Count - 1) sb.Append(", ");
            }
            sb.Append(") => ...");
            return sb.ToString();
        }
    }
}