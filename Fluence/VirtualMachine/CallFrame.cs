using Fluence.RuntimeTypes;

namespace Fluence.VirtualMachine
{
    /// <summary>
    /// Represents the state of a single function call on the stack. It contains the function being executed,
    /// its local variables (registers), the return address, and the destination for the return value.
    /// </summary>
    internal sealed record class CallFrame
    {
        internal Dictionary<int, RuntimeValue> Registers { get; } = new();
        internal TempValue DestinationRegister { get; private set; }
        internal FunctionObject Function { get; private set; }
        internal int ReturnAddress { get; private set; }
        internal Dictionary<int, int> RefParameterMap { get; } = new();

        /// <summary>
        /// A cache to store the readonly status of variables in this scope.
        /// Key: variable name. Value: true if readonly, false if writable.
        /// </summary>
        internal readonly Dictionary<int, bool> WritableCache = new();

        public CallFrame()
        {
        }

        public void Reset()
        {
            RefParameterMap.Clear();
            Registers.Clear();
            WritableCache.Clear();
            DestinationRegister = null!;
            Function = null!;
            ReturnAddress = 0;
        }

        public void Initialize(FunctionObject function, int returnAddress, TempValue destination)
        {
            Function = function;
            ReturnAddress = returnAddress;
            DestinationRegister = destination;
        }
    }
}