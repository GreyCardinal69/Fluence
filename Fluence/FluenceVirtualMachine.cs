using static Fluence.FluenceByteCode;
using static Fluence.FluenceParser;
using static Fluence.FluenceByteCode.InstructionLine;

namespace Fluence
{
    internal sealed class FluenceVirtualMachine
    {
        private List<InstructionLine> _byteCode;
        private int _ip;

        private readonly Dictionary<string, Value> _registers = new();
        private readonly Stack<Value> _argumentStack = new();
        private readonly Stack<CallFrame> _callStack;

        private readonly FluenceScope _globalScope;
        private readonly Dictionary<string, FluenceScope> _nameSpaces;

        // Temporary way to store variables untill more complex operations are implemented.
        private readonly Dictionary<string, Value> _globals;

        internal void DumpVariables()
        {
            foreach (var item in _globals)
            {
                Console.WriteLine(item);
            }
        }

        internal FluenceVirtualMachine(List<InstructionLine> bytecode, ParseState parseState)
        {
            _byteCode = bytecode;
            _globalScope = parseState.GlobalScope;
            _nameSpaces = parseState.NameSpaces;
            _argumentStack = new Stack<Value>();
            _callStack = new Stack<CallFrame>();
            _ip = 0;
            _registers = new Dictionary<string, Value>();
            _globals = new Dictionary<string, Value>();
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
            while (_ip < _byteCode.Count)
            {
                InstructionLine instruction = _byteCode[_ip];
                _ip++;

                switch (instruction.Instruction)
                {
                    case InstructionCode.Assign:
                        ExecuteAssign(instruction);
                        break;
                    case InstructionCode.Add:
                        ExecuteAdd(instruction);
                        break;
                    case InstructionCode.Terminate:
                        // End of code, we simply quit.
                        return;
                    case InstructionCode.CallFunction:
                        // Ignore for now.
                        break;
                    default:
                        throw new NotImplementedException($"VM has not yet implemented the '{instruction.Instruction}' opcode.");
                }
            }
        }

        private Value GetValue(Value val)
        {
            if (val is VariableValue variable)
            {
                if (_globals.TryGetValue(variable.Name, out var value)) return value;
                throw new FluenceRuntimeException($"Undefined variable '{variable.Name}'.");
            }

            if (val is TempValue temp)
            {
                if (_registers.TryGetValue(temp.TempName, out var value)) return value;
                throw new FluenceRuntimeException($"Undefined variable '{temp.TempName}'.");
            }

            return val;
        }

        private void ExecuteAssign(InstructionLine instruction)
        {
            // 1. The destination must be a variable.
            if (instruction.Lhs is not VariableValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'Assign' must be a variable.");
            }

            // 2. READ the value of the source operand.
            Value sourceValue = GetValue(instruction.Rhs);

            // 3. WRITE the value to the variable storage.
            _globals[destination.Name] = sourceValue;
        }

        private void ExecuteAdd(InstructionLine instruction)
        {
            Value left = GetValue(instruction.Rhs);
            Value right = GetValue(instruction.Rhs2);

            if ((left is BooleanValue && right is NumberValue) || (right is BooleanValue && left is NumberValue))
            {
                throw new FluenceRuntimeException("Cannot apply operator '+' to types Boolean and Numeric.");
            }

            if ((left is NilValue && right is not NilValue) || (right is NilValue && left is not NilValue) )
            {
                throw new FluenceRuntimeException("Cannot apply operator '+' to types a non Nil type and Nil type.");
            }

            if (left is BooleanValue && right is BooleanValue)
            {
                throw new FluenceRuntimeException("Cannot apply operator '+' to types Boolean and Boolean.");
            }

            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'Add' must be a temporary register.");
            }

            // TO DO CHECK FOR LIST + NON LIST addition.
            // check for func + non func addition.

            if (left is NumberValue numLeft && right is NumberValue numRight)
            {
                object result;
                switch ((numLeft.Type, numRight.Type))
                {
                    case (NumberValue.NumberType.Integer, NumberValue.NumberType.Integer):
                        result = Convert.ToInt32(numLeft.Value) + Convert.ToInt32(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Integer);
                        break;
                    case (NumberValue.NumberType.Double, NumberValue.NumberType.Double):
                        result = Convert.ToDouble(numLeft.Value) + Convert.ToDouble(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Double);
                        break;
                    case (NumberValue.NumberType.Float, NumberValue.NumberType.Float):
                        result = Convert.ToSingle(numLeft.Value) + Convert.ToSingle(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Float);
                        break;
                    case (NumberValue.NumberType.Integer, NumberValue.NumberType.Double):
                    case (NumberValue.NumberType.Double, NumberValue.NumberType.Integer):
                    case (NumberValue.NumberType.Float, NumberValue.NumberType.Double):
                    case (NumberValue.NumberType.Double, NumberValue.NumberType.Float):
                        // If either operand is a Double, the result is a Double.
                        result = Convert.ToDouble(numLeft.Value) + Convert.ToDouble(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Float);
                        break;
                    case (NumberValue.NumberType.Integer, NumberValue.NumberType.Float):
                    case (NumberValue.NumberType.Float, NumberValue.NumberType.Integer):
                        // If either operand is a float, the result is a float.
                        result = Convert.ToSingle(numLeft.Value) + Convert.ToSingle(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Float);
                        break;
                }
                return;
            }

            if (left is StringValue leftStr && right is StringValue rightStr)
            {
                _registers[destination.TempName] = new StringValue($"{leftStr.Value}{rightStr.Value}");
                return;
            }
            else if (left is StringValue leftStr2)
            {
                _registers[destination.TempName] = new StringValue(string.Concat(leftStr2.Value, right.GetValue()));
                return;
            }
            else if (right is StringValue rightStr2)
            {
                _registers[destination.TempName] = new StringValue(string.Concat(left.GetValue(), rightStr2.Value));
                return;
            }

            // TO DO LIST + LIST addition.

            throw new FluenceRuntimeException($"Cannot apply operator '+' to types {left.GetType().Name} and {right.GetType().Name}.");
        }
    }
}