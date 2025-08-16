namespace Fluence
{
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

    internal sealed class FunctionSymbol : Symbol
    {
        internal string Name { get; }
        internal int Arity { get; }
        internal int StartAddress { get; private set; }

        internal void SetStartAddress(int addr) => StartAddress = addr;

        internal FunctionSymbol(string name, int arity, int startAddress)
        {
            Name = name;
            Arity = arity;
            StartAddress = startAddress;
        }
    }
}