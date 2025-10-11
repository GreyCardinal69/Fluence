using Fluence.RuntimeTypes;
using static Fluence.FluenceByteCode;

namespace Fluence.VirtualMachine
{
    /// <summary>
    /// Captures the state of the virtual machine at the point of the creation of the object.
    /// </summary>
    internal sealed class VMDebugContext
    {
        internal int InstructionPointer { get; }
        internal InstructionLine CurrentInstruction { get; }
        internal IReadOnlyDictionary<int, RuntimeValue> CurrentLocals { get; }
        internal IReadOnlyList<RuntimeValue> OperandStackSnapshot { get; }
        internal int CallStackDepth { get; }
        internal string CurrentFunctionName { get; }

        internal VMDebugContext(FluenceVirtualMachine vm, Stack<RuntimeValue> operandStack, int callStackDepth)
        {
            InstructionPointer = vm.CurrentInstructionPointer > 0 ? vm.CurrentInstructionPointer - 1 : 0;
            CurrentInstruction = vm.ByteCode[InstructionPointer];

            CurrentLocals = new Dictionary<int, RuntimeValue>(vm.CurrentRegisters);

            OperandStackSnapshot = [.. operandStack];
            CallStackDepth = callStackDepth;
            CurrentFunctionName = vm.CurrentFrame.Function.Name;
        }
    }
}