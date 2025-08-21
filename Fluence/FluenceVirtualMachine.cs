using static Fluence.FluenceByteCode;
using static Fluence.FluenceParser;
using static Fluence.FluenceByteCode.InstructionLine;
using System.Text;

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
                        ExecuteBitwiseNot(instruction);
                        break;
                    case InstructionCode.BitwiseAnd:
                        ExecuteBitwiseAnd(instruction);
                        break;
                    case InstructionCode.BitwiseLShift:
                        ExecuteBitwiseLeftShift(instruction);
                        break;
                    case InstructionCode.BitwiseRShift:
                        ExecuteBitwiseRightShift(instruction);
                        break;
                    case InstructionCode.BitwiseXor:
                        ExecuteBitwiseXor(instruction);
                        break;
                    case InstructionCode.BitwiseOr:
                        ExecuteBitwiseOr(instruction);
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

        private void ExecuteBitwiseRightShift(InstructionLine instruction)
        {
            Value left = GetValue(instruction.Rhs);
            Value right = GetValue(instruction.Rhs2);

            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'Bitwise Right Shift' must be a temporary register.");
            }

            if (left is not NumberValue leftNum || right is not NumberValue rightNum)
            {
                throw new FluenceRuntimeException($"Can not Bitwise Right Shift an objects of type {left.GetType().Name} and {right.GetType().Name}.");
            }

            long longLeft = Convert.ToInt64(leftNum.Value);
            int intRight = Convert.ToInt32(rightNum.Value);

            _registers[destination.TempName] = new NumberValue(longLeft >> intRight, NumberValue.NumberType.Integer);
        }

        private void ExecuteBitwiseLeftShift(InstructionLine instruction)
        {
            Value left = GetValue(instruction.Rhs);
            Value right = GetValue(instruction.Rhs2);

            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'Bitwise Left Shift' must be a temporary register.");
            }

            if (left is not NumberValue leftNum || right is not NumberValue rightNum)
            {
                throw new FluenceRuntimeException($"Can not Bitwise Left Shift an objects of type {left.GetType().Name} and {right.GetType().Name}.");
            }

            long longLeft = Convert.ToInt64(leftNum.Value);
            int intRight = Convert.ToInt32(rightNum.Value);

            _registers[destination.TempName] = new NumberValue(longLeft << intRight, NumberValue.NumberType.Integer);
        }

        private void ExecuteBitwiseXor(InstructionLine instruction)
        {
            Value left = GetValue(instruction.Rhs);
            Value right = GetValue(instruction.Rhs2);

            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'Bitwise Xor' must be a temporary register.");
            }

            if (left is not NumberValue leftNum || right is not NumberValue rightNum)
            {
                throw new FluenceRuntimeException($"Can not Bitwise Xor an objects of type {left.GetType().Name} and {right.GetType().Name}.");
            }

            long longLeft = Convert.ToInt64(leftNum.Value);
            long longRight = Convert.ToInt64(rightNum.Value);

            _registers[destination.TempName] = new NumberValue(longLeft ^ longRight, NumberValue.NumberType.Integer);
        }

        private void ExecuteBitwiseOr(InstructionLine instruction)
        {
            Value left = GetValue(instruction.Rhs);
            Value right = GetValue(instruction.Rhs2);

            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'Bitwise Or' must be a temporary register.");
            }

            if (left is not NumberValue leftNum || right is not NumberValue rightNum)
            {
                throw new FluenceRuntimeException($"Can not Bitwise Or an objects of type {left.GetType().Name} and {right.GetType().Name}.");
            }

            long longLeft = Convert.ToInt64(leftNum.Value);
            long longRight = Convert.ToInt64(rightNum.Value);

            _registers[destination.TempName] = new NumberValue(longLeft | longRight, NumberValue.NumberType.Integer);
        }

        private void ExecuteBitwiseAnd(InstructionLine instruction)
        {
            Value left = GetValue(instruction.Rhs);
            Value right = GetValue(instruction.Rhs2);

            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'Bitwise And' must be a temporary register.");
            }

            if (left is not NumberValue leftNum || right is not NumberValue rightNum)
            {
                throw new FluenceRuntimeException($"Can not Bitwise And an objects of type {left.GetType().Name} and {right.GetType().Name}.");
            }

            long longLeft = Convert.ToInt64(leftNum.Value);
            long longRight = Convert.ToInt64(rightNum.Value);

            _registers[destination.TempName] = new NumberValue(longLeft & longRight, NumberValue.NumberType.Integer);
        }

        private void ExecuteBitwiseNot(InstructionLine instruction)
        {
            Value left = GetValue(instruction.Rhs);

            if (instruction.Lhs is not TempValue destination)
            {
                throw new FluenceRuntimeException("Internal VM Error: Destination of 'Bitwise Not' must be a temporary register.");
            }

            if (left is not NumberValue leftNum)
            {
                throw new FluenceRuntimeException($"Can not Bitwise not an object of type {left.GetType().Name}.");
            }

            long integerLong = Convert.ToInt64(leftNum.Value);

            _registers[destination.TempName] = new NumberValue(~integerLong, NumberValue.NumberType.Integer);
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