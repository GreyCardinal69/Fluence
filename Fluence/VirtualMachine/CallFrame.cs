using Fluence.RuntimeTypes;

namespace Fluence.VirtualMachine
{
    /// <summary>
    /// Represents the execution context of a single function call on the call stack.
    /// Manages local registers, temporary values, return information, and reference parameter tracking.
    /// </summary>
    internal sealed record class CallFrame
    {
        /// <summary>
        /// Local registers containing function local variables and temporary values.
        /// </summary>
        internal RuntimeValue[] Registers { get; private set; }

        /// <summary>
        /// Tracks writability status of each register to enforce readonly rules.
        /// </summary>
        internal bool[] WritableCache { get; private set; }

        /// <summary>
        /// The number of total register slots the current frame is allocated to have.
        /// </summary>
        internal int RegisterCount { get; private set; }

        /// <summary>
        /// Destination register for the function's return value.
        /// </summary>
        internal TempValue DestinationRegister { get; private set; }

        /// <summary>
        /// The function object being executed in this call frame.
        /// </summary>
        internal FunctionObject Function { get; private set; }

        /// <summary>
        /// Instruction pointer for return address after function completion.
        /// </summary>
        internal int ReturnAddress { get; private set; }

        /// <summary>
        /// Maps reference parameters to their corresponding register indices in the parent call frame.
        /// </summary>
        internal Dictionary<int, int> RefParameterMap { get; } = new();

        public CallFrame()
        {
            Registers = [];
            WritableCache = [];
            RegisterCount = 0;
        }

        public void Reset()
        {
            RefParameterMap.Clear();

            if (Registers.Length > 0)
            {
                for (int i = 0; i < RegisterCount; i++)
                {
                    WritableCache[i] = false;
                    Registers[i] = RuntimeValue.Nil;
                }
            }

            DestinationRegister = null!;
            Function = null!;
            ReturnAddress = 0;
            RegisterCount = 0;
        }

        /// <summary>
        /// Initializes this call frame for the execution of the specified function.
        /// Allocates and initializes registers based on the function's requirements.
        /// </summary>
        /// <param name="function">The function to execute in this call frame.</param>
        /// <param name="returnAddress">The instruction address to return to after function completion.</param>
        /// <param name="destination">The temporary register to store the return value, if any.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="function"/> is null.</exception>
        public void Initialize(FluenceVirtualMachine vm, FunctionObject function, int returnAddress, TempValue destination)
        {
            if (function is null)
            {
                throw vm.ConstructRuntimeException("Internal VM Error: Can not initialize a new CallFrame with a null function blueprint.");
            }

            int requiredSize = function.RegistersSize;
            EnsureRegisterCapacity(requiredSize);

            RegisterCount = requiredSize;
            Function = function;
            Function.RegistersSize = function.RegistersSize;
            ReturnAddress = returnAddress;
            DestinationRegister = destination;

            for (int i = 0; i < requiredSize; i++)
            {
                Registers[i] = RuntimeValue.Nil;
                WritableCache[i] = false;
            }
        }

        /// <summary>
        /// Ensures the registers arrays have sufficient capacity for the specified register count.
        /// Resizes only when necessary to optimize memory allocation.
        /// </summary>
        /// <param name="requiredSize">The minimum number of registers needed.</param>
        private void EnsureRegisterCapacity(int requiredSize)
        {
            if (Registers.Length < requiredSize)
            {
                Registers = new RuntimeValue[requiredSize];
                WritableCache = new bool[requiredSize];
            }
        }
    }
}