namespace Fluence
{
    // Symbols represent structs, enums, functions.
    // In a struct, in a namespace, in global scope.
    internal abstract class Symbol : Value { }

    internal sealed class EnumSymbol : Symbol
    {
        internal string Name { get; }
        internal Dictionary<string, EnumValue> Members { get; } = new();

        internal EnumSymbol(string name)
        {
            Name = name;
        }
    }

    internal sealed class StructSymbol : Symbol
    {
        internal string Name { get; }
        internal List<string> Fields { get; } = new();
        internal Dictionary<string, FunctionValue> Functions { get; } = new();
        // In the first pass of the parser, we can not modify token stream, so we store the.
        // Expressions of the default values to be generated as bytecode during the second pass.
        // More precisely when generating the init constructor function bytecode.
        internal Dictionary<string, List<Token>> DefaultFieldValuesAsTokens { get; } = new();
        // Constructor is defined as func init(...) {...}
        // The fields are initialized as their default values first, then come user defined code.
        internal FunctionValue Constructor { get; set; }

        internal StructSymbol(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"StructSymbol<{Name}>";
        }
    }

    internal delegate Value IntrinsicMethod(IReadOnlyList<Value> args);

    internal sealed class FunctionSymbol : Symbol
    {
        internal string Name { get; }
        internal int Arity { get; }
        internal int StartAddress { get; private set; }
        internal bool IsIntrinsic { get; }
        internal IntrinsicMethod IntrinsicBody { get; }

        internal void SetStartAddress(int addr) => StartAddress = addr;

        internal FunctionSymbol(string name, int arity, IntrinsicMethod body)
        {
            Name = name;
            Arity = arity;
            StartAddress = -1; // Special address for intrinsics
            IsIntrinsic = true;
            IntrinsicBody = body;
        }

        internal FunctionSymbol(string name, int arity, int startAddress)
        {
            Name = name;
            Arity = arity;
            StartAddress = startAddress;
        }
    }
}