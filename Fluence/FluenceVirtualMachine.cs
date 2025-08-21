using System.Collections.Generic;
using System.Text;
using static Fluence.FluenceByteCode;
using static Fluence.FluenceByteCode.InstructionLine;
using static Fluence.FluenceParser;

namespace Fluence
{
    internal sealed class FluenceVirtualMachine
    {
        private readonly List<InstructionLine> _byteCode;
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
            Console.WriteLine("---GLOBALS---");
            foreach (var item in _globals)
            {
                Console.WriteLine(item);
            }
            Console.WriteLine();
            Console.WriteLine("---REGISTERS---");
            foreach (var item in _registers)
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
#if DEBUG
                Console.WriteLine($"Executing bytecode {FluenceDebug.FormatByteCodeAddress(_ip)}: {_byteCode[_ip]}.");
#endif
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
                    case InstructionCode.Subtract:
                        ExecuteSubtraction(instruction);
                        break;
                    case InstructionCode.Multiply:
                        ExecuteMultiplication(instruction);
                        break;
                    case InstructionCode.Divide:
                        ExecuteDivision(instruction);
                        break;
                    case InstructionCode.Modulo:
                        ExecuteModulo(instruction);
                        break;
                    case InstructionCode.Power:
                        ExecuteExponentiation(instruction);
                        break;
                    case InstructionCode.Negate:
                        ExecuteNegate(instruction);
                        break;
                    case InstructionCode.Not:
                        ExecuteNot(instruction);
                        break;
                    case InstructionCode.BitwiseNot:
                    case InstructionCode.BitwiseAnd:
                    case InstructionCode.BitwiseLShift:
                    case InstructionCode.BitwiseRShift:
                    case InstructionCode.BitwiseXor:
                    case InstructionCode.BitwiseOr:
                        ExecuteBitwiseOperation(instruction);
                        break;
                    case InstructionCode.Equal:
                    case InstructionCode.NotEqual:
                        ExecuteEqualityComparison(instruction);
                        break;
                    case InstructionCode.GreaterThan:
                    case InstructionCode.GreaterEqual:
                    case InstructionCode.LessEqual:
                    case InstructionCode.LessThan:
                        ExecuteIndirectComparison(instruction);
                        break;
                    case InstructionCode.NewList:
                        ExecuteNewList(instruction);
                        break;
                    case InstructionCode.PushElement:
                        ExecutePushElement(instruction);
                        break;
                    case InstructionCode.Goto:
                    case InstructionCode.GotoIfFalse:
                    case InstructionCode.GotoIfTrue:
                        ExecuteGoTo(instruction);
                        break;
                    case InstructionCode.GetElement:
                        ExecuteGetElement(instruction);
                        break;
                    case InstructionCode.SetElement:
                        ExecuteSetElement(instruction);
                        break;
                    case InstructionCode.And:
                    case InstructionCode.Or:
                        ExecuteLogicalOp(instruction);
                        break;
                    case InstructionCode.NewRange:
                        ExecuteNewRange(instruction);
                        break;
                    case InstructionCode.ToString:
                        ExecuteToString(instruction);
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

        private void ExecuteLogicalOp(InstructionLine instruction)
        {
            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'And' must be a temporary register.");
            }

            Value right = GetValue(instruction.Rhs2);
            Value left = GetValue(instruction.Rhs);

            bool result = false;

            switch (instruction.Instruction)
            {
                case InstructionCode.And:
                    result = IsTruthy(right) && IsTruthy(left);
                    break;
                case InstructionCode.Or:
                    result = IsTruthy(left) || IsTruthy(right);
                    break;
            }

            _registers[destination.TempName] = new BooleanValue(result);
        }

        private static bool IsTruthy(Value value)
        {
            if (value is NilValue) return false;
            if (value is BooleanValue b && b.Value == false) return false;
            return true;
        }

        private void ExecuteGetElement(InstructionLine instruction)
        {
            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'GetElement' must be a temporary register.");
            }

            Value collectionValue = GetValue(instruction.Rhs);
            if (collectionValue is not ListValue list)
            {
                throw new FluenceRuntimeException($"Runtime Error: Cannot perform index access '[]' on a value of type '{collectionValue?.GetType().Name ?? "nil"}'.");
            }

            Value indexValue = GetValue(instruction.Rhs2);
            if (indexValue is not NumberValue numIndex)
            {
                throw new FluenceRuntimeException($"Runtime Error: List index must be an integer, not '{indexValue?.GetType().Name ?? "nil"}'.");
            }

            int index = Convert.ToInt32(numIndex.Value);

            if (index < 0 || index >= list.Elements.Count)
            {
                throw new FluenceRuntimeException($"Runtime Error: Index out of range. Index was {index}, but list size is {list.Elements.Count}.");
            }

            _registers[destination.TempName] = list.Elements[index];
        }

        private void ExecuteSetElement(InstructionLine instruction)
        {
            Value collectionValue = GetValue(instruction.Lhs); // The list is the LHS
            if (collectionValue is not ListValue list)
            {
                throw new FluenceRuntimeException($"Runtime Error: Cannot perform index assignment '[] =' on a value of type '{collectionValue?.GetType().Name ?? "nil"}'.");
            }

            Value indexValue = GetValue(instruction.Rhs); // The index is the RHS
            if (indexValue is not NumberValue numIndex)
            {
                throw new FluenceRuntimeException($"Runtime Error: List index must be an integer, not '{indexValue?.GetType().Name ?? "nil"}'.");
            }

            Value valueToSet = GetValue(instruction.Rhs2); // The value is the RHS2
            int index = Convert.ToInt32(numIndex.Value);
            if (index < 0 || index >= list.Elements.Count)
            {
                throw new FluenceRuntimeException($"Runtime Error: Index out of range. Cannot set element at index {index}, list size is {list.Elements.Count}.");
            }

            list.Elements[index] = valueToSet;
        }

        private void ExecuteGoTo(InstructionLine instruction)
        {
            if (instruction.Lhs is not NumberValue target)
            {
                throw new FluenceRuntimeException("Internal VM Error: Operand for Goto must be a NumberValue address.");
            }

            int targetAddress = Convert.ToInt32(target.Value);

            if (targetAddress < 0 || targetAddress >= _byteCode.Count)
            {
                throw new FluenceRuntimeException($"Internal VM Error: Invalid jump address '{targetAddress}'.");
            }

            if (instruction.Instruction == InstructionCode.Goto)
            {
                _ip = targetAddress;
                return;
            }

            Value condition = GetValue(instruction.Rhs);

            if (condition is not BooleanValue booleanCondition)
            {
                throw new FluenceRuntimeException($"Runtime Error: Conditional jump expects a boolean value, but got {condition.GetType().Name}.");
            }

            if (instruction.Instruction == InstructionCode.GotoIfFalse && booleanCondition.Value == false)
            {
                _ip = Convert.ToInt32(targetAddress);
                return;
            }

            if (booleanCondition.Value && instruction.Instruction == InstructionCode.GotoIfTrue)
            {
                _ip = Convert.ToInt32(targetAddress);
                return;
            }
        }

        private void ExecutePushElement(InstructionLine instruction)
        {
            if (GetValue(instruction.Lhs) is not ListValue list)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'PushElement' must be a temporary register.");
            }

            Value elementToAdd = GetValue(instruction.Rhs);

            if (elementToAdd is RangeValue range)
            {
                Value resolvedStartValue = GetValue(range.Start);
                Value resolvedEndValue = GetValue(range.End);

                if (resolvedStartValue is not NumberValue numStart || resolvedEndValue is not NumberValue numEnd)
                {
                    throw new FluenceRuntimeException($"Runtime Error: Range can only be created from numbers, not {resolvedStartValue.GetType().Name} and {resolvedEndValue.GetType().Name}.");
                }

                int start = Convert.ToInt32(numStart.Value);
                int end = Convert.ToInt32(numEnd.Value);

                if (start <= end) // Increasing range
                {
                    for (int i = start; i <= end; i++)
                    {
                        list.Elements.Add(new NumberValue(i));
                    }
                }
                else // Decreasing range
                {
                    for (int i = start; i >= end; i--)
                    {
                        list.Elements.Add(new NumberValue(i));
                    }
                }
            }
            else
            {
                list.Elements.Add(elementToAdd);
            }
        }

        private void ExecuteNewRange(InstructionLine instruction)
        {
            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'NewRange' must be a temporary register.");
            }

            Value start = GetValue(instruction.Rhs);
            Value end = GetValue(instruction.Rhs2);

            _registers[destination.TempName] = new RangeValue(start, end);
        }

        private void ExecuteToString(InstructionLine instruction)
        {
            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'ToString' must be a temporary register.");
            }

            Value valueToConvert = GetValue(instruction.Rhs);

            string stringResult;
            switch (valueToConvert)
            {
                case null:
                case NilValue:
                    stringResult = "nil";
                    break;
                case StringValue str:
                    stringResult = str.Value;
                    break;
                case BooleanValue b:
                    stringResult = b.Value ? "true" : "false";
                    break;
                default:
                    // For all other types (NumberValue, ListValue, etc.),
                    // we rely on their own .ToString() method.
                    stringResult = valueToConvert.ToString();
                    break;
            }

            // Store the final result in the destination register.
            _registers[destination.TempName] = new StringValue(stringResult);
        }

        private void ExecuteNewList(InstructionLine instruction)
        {
            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'NewList' must be a temporary register.");
            }

            _registers[destination.TempName] = new ListValue();
        }

        private void ExecuteIndirectComparison(InstructionLine instruction)
        {
            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException($"Internal VM Error: Destination of '{instruction.Instruction}' must be a temporary register.");
            }

            Value left = GetValue(instruction.Rhs);
            Value right = GetValue(instruction.Rhs2);

            Func<double, double, bool> doubleComparer = instruction.Instruction switch
            {
                InstructionCode.GreaterThan => (a, b) => a > b,
                InstructionCode.GreaterEqual => (a, b) => a >= b,
                InstructionCode.LessThan => (a, b) => a < b,
                InstructionCode.LessEqual => (a, b) => a <= b,
            };

            Func<int, bool> stringCharComparer = instruction.Instruction switch
            {
                InstructionCode.GreaterThan => result => result > 0,
                InstructionCode.GreaterEqual => result => result >= 0,
                InstructionCode.LessThan => result => result < 0,
                InstructionCode.LessEqual => result => result <= 0,
            };

            bool result = (left, right) switch
            {
                (NumberValue numLeft, NumberValue numRight) =>
                    doubleComparer(Convert.ToDouble(numLeft.Value), Convert.ToDouble(numRight.Value)),

                (StringValue strLeft, StringValue strRight) =>
                    stringCharComparer(string.Compare(strLeft.Value, strRight.Value, StringComparison.Ordinal)),

                (CharValue charLeft, CharValue charRight) =>
                    stringCharComparer(charLeft.Value.CompareTo(charRight.Value)),

                (StringValue str, CharValue c) =>
                    stringCharComparer(string.Compare(str.Value, c.Value.ToString(), StringComparison.Ordinal)),

                (CharValue c, StringValue str) =>
                    stringCharComparer(string.Compare(c.Value.ToString(), str.Value, StringComparison.Ordinal)),

                _ => throw new FluenceRuntimeException($"Cannot apply operator '{instruction.Instruction}' to types {left.GetType().Name} and {right.GetType().Name}.")
            };

            _registers[destination.TempName] = new BooleanValue(result);
        }

        private void ExecuteEqualityComparison(InstructionLine instruction)
        {
            Value left = GetValue(instruction.Rhs);
            Value right = GetValue(instruction.Rhs2);

            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'Equal' must be a temporary register.");
            }

            bool result = instruction.Instruction == InstructionCode.Equal ? left.Equals(right) : !left.Equals(right);

            _registers[destination.TempName] = new BooleanValue(result);
        }

        private void ExecuteBitwiseOperation(InstructionLine instruction)
        {
            Value left = GetValue(instruction.Rhs);

            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException($"Internal VM Error: Destination of '{instruction.Instruction}' must be a temporary register.");
            }

            if (left is not NumberValue leftNum)
            {
                throw new FluenceRuntimeException($"Can not {instruction.Instruction} an objects of type {left.GetType().Name}.");
            }

            long longLeft = Convert.ToInt64(leftNum.Value);

            if (instruction.Instruction == InstructionCode.BitwiseNot)
            {
                _registers[destination.TempName] = new NumberValue(~longLeft, NumberValue.NumberType.Integer);
                return;
            }

            Value right = GetValue(instruction.Rhs2);

            if (right is not NumberValue rightNum)
            {
                throw new FluenceRuntimeException($"Can not {instruction.Instruction} an objects of type {left.GetType().Name} and {right.GetType().Name}.");
            }

            long longRight = 0;

            switch (instruction.Instruction)
            {
                case InstructionCode.BitwiseOr:
                case InstructionCode.BitwiseXor:
                    longRight = Convert.ToInt64(rightNum.Value);

                    _registers[destination.TempName] = new NumberValue(instruction.Instruction == InstructionCode.BitwiseXor
                        ? longLeft ^ longRight
                        : longLeft | longRight, NumberValue.NumberType.Integer);
                    break;
                case InstructionCode.BitwiseRShift:
                case InstructionCode.BitwiseLShift:
                    int intRight = Convert.ToInt32(rightNum.Value);

                    _registers[destination.TempName] = new NumberValue(instruction.Instruction == InstructionCode.BitwiseLShift
                        ? longLeft << intRight
                        : longLeft >> intRight, NumberValue.NumberType.Integer);
                    break;
                case InstructionCode.BitwiseAnd:
                    longRight = Convert.ToInt64(rightNum.Value);
                    _registers[destination.TempName] = new NumberValue(longLeft & longRight, NumberValue.NumberType.Integer);
                    break;
            }
        }

        private void ExecuteNot(InstructionLine instruction)
        {
            Value left = GetValue(instruction.Rhs);

            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'Not' must be a temporary register.");
            }

            if (left is not BooleanValue leftBool)
            {
                throw new FluenceRuntimeException($"Can not boolean flip an object of type {left.GetType().Name}.");
            }

            _registers[destination.TempName] = new BooleanValue(!(bool)leftBool.Value);
        }

        private void ExecuteNegate(InstructionLine instruction)
        {
            Value left = GetValue(instruction.Rhs);

            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'Negate' must be a temporary register.");
            }

            if (left is not NumberValue leftNum)
            {
                throw new FluenceRuntimeException($"Can not negate an object of type {left.GetType().Name}.");
            }

            object result = null!;

            switch (leftNum.Type)
            {
                case NumberValue.NumberType.Double:
                    result = -Convert.ToDouble(leftNum.Value);
                    break;
                case NumberValue.NumberType.Float:
                    result = -Convert.ToSingle(leftNum.Value);
                    break;
                case NumberValue.NumberType.Integer:
                    result = -Convert.ToInt32(leftNum.Value);
                    break;
            }

            _registers[destination.TempName] = new NumberValue(result, leftNum.Type);
        }

        private void ExecuteExponentiation(InstructionLine instruction)
        {
            Value left = GetValue(instruction.Rhs);
            Value right = GetValue(instruction.Rhs2);

            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'Exponentiation' must be a temporary register.");
            }

            if (left is NumberValue numLeft && right is NumberValue numRight)
            {
                object result;
                switch ((numLeft.Type, numRight.Type))
                {
                    case (NumberValue.NumberType.Integer, NumberValue.NumberType.Integer):
                        result = Math.Pow(Convert.ToInt32(numLeft.Value), Convert.ToInt32(numRight.Value));
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Integer);
                        break;
                    case (NumberValue.NumberType.Double, NumberValue.NumberType.Double):
                        result = Math.Pow(Convert.ToDouble(numLeft.Value), Convert.ToDouble(numRight.Value));
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Double);
                        break;
                    case (NumberValue.NumberType.Float, NumberValue.NumberType.Float):
                        result = Math.Pow(Convert.ToSingle(numLeft.Value), Convert.ToSingle(numRight.Value));
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Float);
                        break;
                    case (NumberValue.NumberType.Integer, NumberValue.NumberType.Double):
                    case (NumberValue.NumberType.Double, NumberValue.NumberType.Integer):
                    case (NumberValue.NumberType.Float, NumberValue.NumberType.Double):
                    case (NumberValue.NumberType.Double, NumberValue.NumberType.Float):
                        // If either operand is a Double, the result is a Double.
                        result = Math.Pow(Convert.ToDouble(numLeft.Value), Convert.ToDouble(numRight.Value));
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Float);
                        break;
                    case (NumberValue.NumberType.Integer, NumberValue.NumberType.Float):
                    case (NumberValue.NumberType.Float, NumberValue.NumberType.Integer):
                        // If either operand is a float, the result is a float.
                        result = Math.Pow(Convert.ToSingle(numLeft.Value), Convert.ToSingle(numRight.Value));
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Float);
                        break;
                }
                return;
            }
            else
            {
                throw new FluenceRuntimeException($"Can not multiply objects of type {left.GetType().Name} and {right.GetType().Name}.");
            }
        }

        private void ExecuteModulo(InstructionLine instruction)
        {
            Value left = GetValue(instruction.Rhs);
            Value right = GetValue(instruction.Rhs2);

            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'Modulo' must be a temporary register.");
            }

            if (left is NumberValue numLeft && right is NumberValue numRight)
            {
                object result;
                switch ((numLeft.Type, numRight.Type))
                {
                    case (NumberValue.NumberType.Integer, NumberValue.NumberType.Integer):
                        result = Convert.ToInt32(numLeft.Value) % Convert.ToInt32(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Integer);
                        break;
                    case (NumberValue.NumberType.Double, NumberValue.NumberType.Double):
                        result = Convert.ToDouble(numLeft.Value) % Convert.ToDouble(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Double);
                        break;
                    case (NumberValue.NumberType.Float, NumberValue.NumberType.Float):
                        result = Convert.ToSingle(numLeft.Value) % Convert.ToSingle(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Float);
                        break;
                    case (NumberValue.NumberType.Integer, NumberValue.NumberType.Double):
                    case (NumberValue.NumberType.Double, NumberValue.NumberType.Integer):
                    case (NumberValue.NumberType.Float, NumberValue.NumberType.Double):
                    case (NumberValue.NumberType.Double, NumberValue.NumberType.Float):
                        // If either operand is a Double, the result is a Double.
                        result = Convert.ToDouble(numLeft.Value) % Convert.ToDouble(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Float);
                        break;
                    case (NumberValue.NumberType.Integer, NumberValue.NumberType.Float):
                    case (NumberValue.NumberType.Float, NumberValue.NumberType.Integer):
                        // If either operand is a float, the result is a float.
                        result = Convert.ToSingle(numLeft.Value) % Convert.ToSingle(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Float);
                        break;
                }
                return;
            }
            else
            {
                throw new FluenceRuntimeException($"Can not multiply objects of type {left.GetType().Name} and {right.GetType().Name}.");
            }
        }

        private static string MultiplyString(string str, int n)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                sb.Append(str);
            }
            return sb.ToString();
        }

        private static string MultiplyChar(char c, int n)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                sb.Append(c);
            }
            return sb.ToString();
        }

        private void ExecuteDivision(InstructionLine instruction)
        {
            Value left = GetValue(instruction.Rhs);
            Value right = GetValue(instruction.Rhs2);

            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'Division' must be a temporary register.");
            }

            if (left is NumberValue numLeft && right is NumberValue numRight)
            {
                if ((numLeft.Value is 0 or 0.0 or 0.0f) || (numRight.Value is 0 or 0.0 or 0.0f))
                {
                    throw new FluenceRuntimeException("Division by zero.");
                }

                object resultValue;
                NumberValue.NumberType resultType;

                // 5 / 2 will not truncate to 2, rather 2.5 as double.
                switch ((numLeft.Type, numRight.Type))
                {
                    case (NumberValue.NumberType.Integer, NumberValue.NumberType.Integer):
                        resultValue = Convert.ToDouble(numLeft.Value) / Convert.ToDouble(numRight.Value);
                        resultType = NumberValue.NumberType.Double;
                        break;
                    case (NumberValue.NumberType.Float, NumberValue.NumberType.Float):
                        resultValue = Convert.ToSingle(numLeft.Value) / Convert.ToSingle(numRight.Value);
                        resultType = NumberValue.NumberType.Float;
                        break;
                    case (NumberValue.NumberType.Double, NumberValue.NumberType.Double):
                        resultValue = Convert.ToDouble(numLeft.Value) / Convert.ToDouble(numRight.Value);
                        resultType = NumberValue.NumberType.Double;
                        break;
                    case (NumberValue.NumberType.Integer, NumberValue.NumberType.Double):
                    case (NumberValue.NumberType.Double, NumberValue.NumberType.Integer):
                    case (NumberValue.NumberType.Float, NumberValue.NumberType.Double):
                    case (NumberValue.NumberType.Double, NumberValue.NumberType.Float):
                        resultValue = Convert.ToDouble(numLeft.Value) / Convert.ToDouble(numRight.Value);
                        resultType = NumberValue.NumberType.Double;
                        break;
                    case (NumberValue.NumberType.Integer, NumberValue.NumberType.Float):
                    case (NumberValue.NumberType.Float, NumberValue.NumberType.Integer):
                        resultValue = Convert.ToSingle(numLeft.Value) / Convert.ToSingle(numRight.Value);
                        resultType = NumberValue.NumberType.Float;
                        break;
                    default:
                        throw new FluenceRuntimeException($"Internal VM Error: Unhandled numeric type combination for operator '/'.");
                }

                _registers[destination.TempName] = new NumberValue(resultValue, resultType);
                return;
            }
            else
            {
                throw new FluenceRuntimeException($"Can not divide objects of type {left.GetType().Name} and {right.GetType().Name}.");
            }
        }

        private void ExecuteMultiplication(InstructionLine instruction)
        {
            Value left = GetValue(instruction.Rhs);
            Value right = GetValue(instruction.Rhs2);

            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'Multiplication' must be a temporary register.");
            }

            if (left is StringValue leftStr && right is NumberValue rightNum)
            {
                if (rightNum.Type != NumberValue.NumberType.Integer)
                {
                    throw new FluenceRuntimeException("Can not multiply string by a non integer number.");
                }
                _registers[destination.TempName] = new StringValue(MultiplyString(leftStr.Value, Convert.ToInt32(rightNum.GetValue())));
                return;
            }
            else if (left is NumberValue leftNum && right is StringValue rightStr)
            {
                if (leftNum.Type != NumberValue.NumberType.Integer)
                {
                    throw new FluenceRuntimeException("Can not multiply string by a non integer number.");
                }
                _registers[destination.TempName] = new StringValue(MultiplyString(rightStr.Value, Convert.ToInt32(leftNum.GetValue())));
                return;
            }

            if (left is CharValue leftChar && right is NumberValue rightNum2)
            {
                if (rightNum2.Type != NumberValue.NumberType.Integer)
                {
                    throw new FluenceRuntimeException("Can not multiply a character by a non integer number.");
                }
                _registers[destination.TempName] = new StringValue(MultiplyChar(leftChar.Value, Convert.ToInt32(rightNum2.GetValue())));
                return;
            }
            else if (left is NumberValue leftNum2 && right is CharValue rightChar)
            {
                if (leftNum2.Type != NumberValue.NumberType.Integer)
                {
                    throw new FluenceRuntimeException("Can not multiply a character by a non integer number.");
                }
                _registers[destination.TempName] = new StringValue(MultiplyChar(rightChar.Value, Convert.ToInt32(leftNum2.GetValue())));
                return;
            }

            // TO DO, multiplying list and integer
            // [1,2] * 2 => [1,2,1,2].
            if (left is ListValue leftList && right is NumberValue rightRep)
            {
                throw new NotImplementedException("List * Num not yet done.");
            }
            else if (left is NumberValue leftRep && right is ListValue rightList)
            {
                throw new NotImplementedException("Num * List not yet done.");
            }

            if (left is NumberValue numLeft && right is NumberValue numRight)
            {
                object result;
                switch ((numLeft.Type, numRight.Type))
                {
                    case (NumberValue.NumberType.Integer, NumberValue.NumberType.Integer):
                        result = Convert.ToInt32(numLeft.Value) * Convert.ToInt32(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Integer);
                        break;
                    case (NumberValue.NumberType.Double, NumberValue.NumberType.Double):
                        result = Convert.ToDouble(numLeft.Value) * Convert.ToDouble(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Double);
                        break;
                    case (NumberValue.NumberType.Float, NumberValue.NumberType.Float):
                        result = Convert.ToSingle(numLeft.Value) * Convert.ToSingle(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Float);
                        break;
                    case (NumberValue.NumberType.Integer, NumberValue.NumberType.Double):
                    case (NumberValue.NumberType.Double, NumberValue.NumberType.Integer):
                    case (NumberValue.NumberType.Float, NumberValue.NumberType.Double):
                    case (NumberValue.NumberType.Double, NumberValue.NumberType.Float):
                        // If either operand is a Double, the result is a Double.
                        result = Convert.ToDouble(numLeft.Value) * Convert.ToDouble(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Float);
                        break;
                    case (NumberValue.NumberType.Integer, NumberValue.NumberType.Float):
                    case (NumberValue.NumberType.Float, NumberValue.NumberType.Integer):
                        // If either operand is a float, the result is a float.
                        result = Convert.ToSingle(numLeft.Value) * Convert.ToSingle(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Float);
                        break;
                }
                return;
            }
            else
            {
                throw new FluenceRuntimeException($"Can not multiply objects of type {left.GetType().Name} and {right.GetType().Name}.");
            }
        }

        private void ExecuteAssign(InstructionLine instruction)
        {
            if (instruction.Lhs is not VariableValue && instruction.Lhs is not TempValue)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'Assign' must be a variable or a TempValue.");
            }

            if (instruction.Lhs is VariableValue destination)
            {
                Value sourceValue = GetValue(instruction.Rhs);
                _globals[destination.Name] = sourceValue;
            }
            else if (instruction.Lhs is TempValue tempDestination)
            {
                Value sourceValue = GetValue(instruction.Rhs);
                _registers[tempDestination.TempName] = sourceValue;
            }
        }

        private void ExecuteSubtraction(InstructionLine instruction)
        {
            Value left = GetValue(instruction.Rhs);
            Value right = GetValue(instruction.Rhs2);

            if (left is ListValue && right is ListValue)
            {
                // TO DO, list1 - list2 is the same as taking elements of list2
                // and removing them, if present from list1, a sort of filter operation.
                throw new NotImplementedException("List - List not yet done.");
            }

            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'Subtract' must be a temporary register.");
            }

            if (left is NumberValue numLeft && right is NumberValue numRight)
            {
                object result;
                switch ((numLeft.Type, numRight.Type))
                {
                    case (NumberValue.NumberType.Integer, NumberValue.NumberType.Integer):
                        result = Convert.ToInt32(numLeft.Value) - Convert.ToInt32(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Integer);
                        break;
                    case (NumberValue.NumberType.Double, NumberValue.NumberType.Double):
                        result = Convert.ToDouble(numLeft.Value) - Convert.ToDouble(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Double);
                        break;
                    case (NumberValue.NumberType.Float, NumberValue.NumberType.Float):
                        result = Convert.ToSingle(numLeft.Value) - Convert.ToSingle(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Float);
                        break;
                    case (NumberValue.NumberType.Integer, NumberValue.NumberType.Double):
                    case (NumberValue.NumberType.Double, NumberValue.NumberType.Integer):
                    case (NumberValue.NumberType.Float, NumberValue.NumberType.Double):
                    case (NumberValue.NumberType.Double, NumberValue.NumberType.Float):
                        // If either operand is a Double, the result is a Double.
                        result = Convert.ToDouble(numLeft.Value) - Convert.ToDouble(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Float);
                        break;
                    case (NumberValue.NumberType.Integer, NumberValue.NumberType.Float):
                    case (NumberValue.NumberType.Float, NumberValue.NumberType.Integer):
                        // If either operand is a float, the result is a float.
                        result = Convert.ToSingle(numLeft.Value) - Convert.ToSingle(numRight.Value);
                        _registers[destination.TempName] = new NumberValue(result, NumberValue.NumberType.Float);
                        break;
                }
                return;
            }
            else
            {
                throw new FluenceRuntimeException($"Cannot apply operator '-' to types {left.GetType().Name} and {right.GetType().Name}.");
            }
        }

        private void ExecuteAdd(InstructionLine instruction)
        {
            Value left = GetValue(instruction.Rhs);
            Value right = GetValue(instruction.Rhs2);

            if ((left is BooleanValue && right is NumberValue) || (right is BooleanValue && left is NumberValue))
            {
                throw new FluenceRuntimeException("Cannot apply operator '+' to types Boolean and Numeric.");
            }

            if ((left is NilValue && right is not NilValue) || (right is NilValue && left is not NilValue))
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

            if ((left is ListValue && right is not ListValue) || (left is not ListValue && right is ListValue))
            {
                throw new FluenceRuntimeException("Cannot apply opertor '+' to types List and not List.");
            }

            if (left is FunctionValue || right is FunctionValue)
            {
                throw new FluenceException("Cannot apply operator '+' between a function and other types, or functions.");
            }

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
            if (left is ListValue && right is ListValue)
            {
                // combinations of both lists' elements.
                throw new NotImplementedException("List - List not yet done.");
            }

            throw new FluenceRuntimeException($"Cannot apply operator '+' to types {left.GetType().Name} and {right.GetType().Name}.");
        }
    }
}