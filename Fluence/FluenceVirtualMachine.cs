using static Fluence.FluenceByteCode;
using static Fluence.FluenceParser;

namespace Fluence
{
    internal sealed class FluenceVirtualMachine
    {
        private List<InstructionLine> _byteCode;
        private int _ip;
        private readonly Stack<Value> _valueStack;
        private readonly Stack<CallFrame> _callStack;

        private readonly FluenceScope _globalScope;
        private readonly Dictionary<string, FluenceScope> _nameSpaces;

        internal FluenceVirtualMachine(List<InstructionLine> bytecode, ParseState parseState)
        {
            _byteCode = bytecode;
            _globalScope = parseState.GlobalScope;
            _nameSpaces = parseState.NameSpaces;
        }

        internal sealed class CallFrame
        {
            /// <summary>
            /// The function that is being executed in this frame.
            /// </summary>
            public FunctionValue Function { get; }

            /// <summary>
            /// The instruction pointer (address) to return to when this function completes.
            /// </summary>
            public int ReturnAddress { get; }

            /// <summary>
            /// A dictionary to store the local variables for this specific function call.
            /// </summary>
            public Dictionary<string, Value> Locals { get; } = new();

            /// <summary>
            /// The base index on the VM's main value stack where this function's locals begin.
            /// </summary>
            public int StackBasePointer { get; }

            public CallFrame(FunctionValue function, int returnAddress, int stackBasePointer)
            {
                Function = function;
                ReturnAddress = returnAddress;
                StackBasePointer = stackBasePointer;
            }
        }

        internal void Run()
        {
   
        }
    }
}