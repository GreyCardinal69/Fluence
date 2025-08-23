using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static Fluence.FluenceByteCode;
using static Fluence.FluenceByteCode.InstructionLine;
using static Fluence.FluenceParser;

namespace Fluence
{
    /// <summary>
    /// The core execution engine for Fluence bytecode. It manages the call stack, instruction pointer,
    /// memory (registers and globals), and the main execution loop.
    /// </summary>
    internal sealed class FluenceVirtualMachine
    {
        /// <summary>A delegate for a natively implemented C# function that can be called from Fluence.</summary>
        internal delegate RuntimeValue IntrinsicRuntimeMethod(IReadOnlyList<RuntimeValue> args);

        /// <summary>A delegate representing the handler for a specific VM instruction.</summary>
        private delegate void OpcodeHandler(InstructionLine instruction);

        /// <summary>A performance-critical dispatch table that maps an instruction's opcode to its handler method.</summary>
        private readonly OpcodeHandler[] _dispatchTable;

        /// <summary>The immutable list of bytecode instructions to be executed.</summary>
        private readonly List<InstructionLine> _byteCode;

        /// <summary>The call stack, containing a <see cref="CallFrame"/> for each active function call.</summary>
        private readonly Stack<CallFrame> _callStack;

        /// <summary>
        /// A direct, cached reference to the registers of the currently executing function's call frame.
        /// </summary>
        private Dictionary<string, RuntimeValue> _cachedRegisters;

        /// <summary>A dictionary holding all global variables.</summary>
        private readonly Dictionary<string, RuntimeValue> _globals;

        /// <summary>The top-level global scope, used for resolving global functions and variables.</summary>
        private readonly FluenceScope _globalScope;

        /// <summary>The stack used for passing arguments to functions and for temporary operand storage.</summary>
        private readonly Stack<RuntimeValue> _operandStack = new();

        /// <summary>The Instruction Pointer, which holds the address of the *next* instruction to be executed.</summary>
        private int _ip;

        /// <summary>Gets the currently active call frame from the top of the call stack.</summary>
        private CallFrame CurrentFrame => _callStack.Peek();

        /// <summary>Gets the registers for the current call frame via the cached reference.</summary>
        private Dictionary<string, RuntimeValue> CurrentRegisters => _cachedRegisters;

        /// <summary>
        /// Represents the state of a single function call on the stack. It contains the function being executed,
        /// its local variables (registers), the return address, and the destination for the return value.
        /// </summary>
        private sealed class CallFrame
        {
            internal Dictionary<string, RuntimeValue> Registers { get; } = new();
            internal readonly TempValue DestinationRegister;
            internal readonly FunctionObject Function;
            internal readonly int ReturnAddress;

            internal CallFrame(FunctionObject function, int returnAddress, TempValue destination)
            {
                Function = function;
                ReturnAddress = returnAddress;
                DestinationRegister = destination;
            }
        }

        /// <summary>
        /// Initializes a new instance of the Fluence Virtual Machine.
        /// </summary>
        /// <param name="bytecode">The compiled bytecode to execute.</param>
        /// <param name="parseState">The final state from the parser, containing scope information.</param>
        internal FluenceVirtualMachine(List<InstructionLine> bytecode, ParseState parseState)
        {
            _byteCode = bytecode;
            _globalScope = parseState.GlobalScope;
            _globals = new Dictionary<string, RuntimeValue>();
            _callStack = new Stack<CallFrame>();

            // This represents the top-level global execution context.
            var mainScriptFunc = new FunctionObject("<script>", 0, new List<string>(), 0, _globalScope);
            var initialFrame = new CallFrame(mainScriptFunc, _byteCode.Count, null!);
            _callStack.Push(initialFrame);

            _cachedRegisters = initialFrame.Registers;

            var opCodeValues = Enum.GetValues<InstructionCode>();
            int maxOpCode = 0;

            foreach (var value in opCodeValues)
            {
                if ((int)value > maxOpCode) maxOpCode = (int)value;
            }
            _dispatchTable = new OpcodeHandler[maxOpCode + 1];

            _dispatchTable[(int)InstructionCode.Assign] = ExecuteAssign;
            _dispatchTable[(int)InstructionCode.Add] = ExecuteAdd;
            _dispatchTable[(int)InstructionCode.Subtract] = ExecuteSubtraction;
            _dispatchTable[(int)InstructionCode.Multiply] = ExecuteMultiplication;
            _dispatchTable[(int)InstructionCode.Divide] = ExecuteDivision;
            _dispatchTable[(int)InstructionCode.Modulo] = ExecuteModulo;
            _dispatchTable[(int)InstructionCode.Power] = ExecutePower;
            _dispatchTable[(int)InstructionCode.Negate] = ExecuteNegate;
            _dispatchTable[(int)InstructionCode.Not] = ExecuteNot;
            _dispatchTable[(int)InstructionCode.CallFunction] = ExecuteCallFunction;
            _dispatchTable[(int)InstructionCode.Return] = ExecuteReturn;
            _dispatchTable[(int)InstructionCode.NewInstance] = ExecuteNewInstance;
            _dispatchTable[(int)InstructionCode.SetField] = ExecuteSetField;
            _dispatchTable[(int)InstructionCode.GetField] = ExecuteGetField;
            _dispatchTable[(int)InstructionCode.CallMethod] = ExecuteCallMethod;
            _dispatchTable[(int)InstructionCode.NewList] = ExecuteNewList;
            _dispatchTable[(int)InstructionCode.PushElement] = ExecutePushElement;
            _dispatchTable[(int)InstructionCode.GetElement] = ExecuteGetElement;
            _dispatchTable[(int)InstructionCode.SetElement] = ExecuteSetElement;
            _dispatchTable[(int)InstructionCode.NewRange] = ExecuteNewRange;
            _dispatchTable[(int)InstructionCode.GetLength] = ExecuteGetLength;
            _dispatchTable[(int)InstructionCode.ToString] = ExecuteToString;
            _dispatchTable[(int)InstructionCode.Goto] = ExecuteGoto;
            _dispatchTable[(int)InstructionCode.NewIterator] = ExecuteNewIterator;
            _dispatchTable[(int)InstructionCode.IterNext] = ExecuteIterNext;
            _dispatchTable[(int)InstructionCode.PushParam] = ExecutePushParam;

            _dispatchTable[(int)InstructionCode.BitwiseNot] = ExecuteBitwiseOperation;
            _dispatchTable[(int)InstructionCode.BitwiseAnd] = ExecuteBitwiseOperation;
            _dispatchTable[(int)InstructionCode.BitwiseOr] = ExecuteBitwiseOperation;
            _dispatchTable[(int)InstructionCode.BitwiseXor] = ExecuteBitwiseOperation;
            _dispatchTable[(int)InstructionCode.BitwiseLShift] = ExecuteBitwiseOperation;
            _dispatchTable[(int)InstructionCode.BitwiseRShift] = ExecuteBitwiseOperation;

            _dispatchTable[(int)InstructionCode.GreaterThan] = ExecuteGreaterThan;
            _dispatchTable[(int)InstructionCode.GreaterEqual] = ExecuteGreaterEqual;
            _dispatchTable[(int)InstructionCode.LessThan] = ExecuteLessThan;
            _dispatchTable[(int)InstructionCode.LessEqual] = ExecuteLessEqual;

            _dispatchTable[(int)InstructionCode.Equal] = (inst) => ExecuteEqualityComparison(inst, true);
            _dispatchTable[(int)InstructionCode.NotEqual] = (inst) => ExecuteEqualityComparison(inst, false);

            _dispatchTable[(int)InstructionCode.And] = (inst) => ExecuteLogicalOp(inst, true);
            _dispatchTable[(int)InstructionCode.Or] = (inst) => ExecuteLogicalOp(inst, false);

            _dispatchTable[(int)InstructionCode.GotoIfFalse] = (inst) => ExecuteGotoIf(inst, false);
            _dispatchTable[(int)InstructionCode.GotoIfTrue] = (inst) => ExecuteGotoIf(inst, true);

            // Simple case for Terminate
            _dispatchTable[(int)InstructionCode.Terminate] = (inst) => _ip = _byteCode.Count;
        }

        /// <summary>
        /// Begins execution of the loaded bytecode and runs until completion or an error occurs.
        /// </summary>
        internal void Run()
        {
            while (_ip < _byteCode.Count)
            {
                InstructionLine instruction = _byteCode[_ip];
                _ip++;
                _dispatchTable[(int)instruction.Instruction](instruction);
            }
        }

        /// <summary>
        /// Converts a compile-time <see cref="Value"/> from bytecode into a runtime <see cref="RuntimeValue"/>.
        /// This is the bridge between the parser's representation and the VM's execution values.
        /// </summary>
        private RuntimeValue GetRuntimeValue(Value val)
        {
            if (val is TempValue temp)
            {
                ref var valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(CurrentRegisters, temp.TempName);

                if (!Unsafe.IsNullRef(ref valueRef))
                {
                    return valueRef;
                }

                return RuntimeValue.Nil;
            }

            if (val is VariableValue variable)
            {
                return ResolveVariable(variable.Name);
            }

            return val switch
            {
                EnumValue enumVal => new RuntimeValue(enumVal.Value),
                NumberValue num => num.Type switch
                {
                    NumberValue.NumberType.Integer => new RuntimeValue((int)num.Value),
                    NumberValue.NumberType.Float => new RuntimeValue((float)num.Value),
                    NumberValue.NumberType.Double => new RuntimeValue((double)num.Value),
                    _ => throw new FluenceRuntimeException($"Internal VM Error: Unrecognized NumberType '{num.Type}' in bytecode.")
                },
                BooleanValue boolean => new RuntimeValue(boolean.Value),
                NilValue => RuntimeValue.Nil,
                StringValue str => new RuntimeValue(new StringObject(str.Value)),
                RangeValue range => new RuntimeValue(new RangeObject(GetRuntimeValue(range.Start), GetRuntimeValue(range.End))),
                FunctionValue func => new RuntimeValue(func),
                _ => throw new FluenceRuntimeException($"Internal VM Error: Unrecognized Value type '{val.GetType().Name}' during conversion.")
            };
        }

        /// <summary>
        /// Handles the CALL_METHOD instruction code, which invokes a method on an object instance.
        /// </summary>
        private void ExecuteCallMethod(InstructionLine instruction)
        {
            if (instruction.Rhs2 is not StringValue methodNameVal)
            {
                throw new FluenceRuntimeException("Internal VM Error: Invalid operands for CallMethod. Expected a method name as a string.");
            }

            string methodName = methodNameVal.Value;

            var instanceVal = GetRuntimeValue(instruction.Rhs);

            // Check if the object is a native type (like List) that exposes fast C# methods.
            if (instanceVal.ObjectReference is IFluenceObject fluenceObject)
            {
                // Ask the object if it has a built-in method with this name.
                if (fluenceObject.TryGetIntrinsicMethod(methodName, out var intrinsicMethod))
                {
                    // We found an intrinsic.
                    var args = new List<RuntimeValue>();
                    // The first argument to an intrinsic method is always 'self'.
                    args.Add(instanceVal);

                    int argCount = _operandStack.Count;
                    for (int i = 0; i < argCount; i++)
                    {
                        args.Add(_operandStack.Pop());
                    }
                    args.Reverse(1, argCount); // Reverse the explicit args, but keep 'self' at the front.

                    SetRegister((TempValue)instruction.Lhs, intrinsicMethod(args));
                    return;
                }
            }

            if (instanceVal.IsNot(out InstanceObject instance))
            {
                throw new FluenceRuntimeException($"Internal VM Error: Cannot call method '{methodName}' on a non-instance object.");
            }

            FunctionValue methodBlueprint;

            if (methodName == "init")
            {
                methodBlueprint = instance!.Class.Constructor;
                if (methodBlueprint == null)
                {
                    SetRegister((TempValue)instruction.Lhs, instanceVal);
                    // No explicit init, do nothing.
                    return;
                }
            }
            else
            {
                if (!instance!.Class.Functions.TryGetValue(methodName, out methodBlueprint))
                {
                    throw new FluenceRuntimeException($"Internal VM Error:Undefined method '{methodName}' on struct '{instance.Class.Name}'.");
                }
            }

            // Convert the compile-time FunctionValue into a runtime FunctionObject.
            var functionToExecute = new FunctionObject(
                methodBlueprint.Name,
                methodBlueprint.Arity,
                methodBlueprint.Arguments,
                methodBlueprint.StartAddress,
                methodBlueprint.FunctionScope
            );

            int argCountOnStack = _operandStack.Count;
            if (functionToExecute.Arity != argCountOnStack)
            {
                throw new FluenceRuntimeException($"Internal VM Error:Mismatched arity for method '{functionToExecute.Name}'. Expected {functionToExecute.Arity}, but got {argCountOnStack}.");
            }

            var newFrame = new CallFrame(functionToExecute, _ip, (TempValue)instruction.Lhs);

            // Implicitly pass 'self'.
            newFrame.Registers["self"] = instanceVal;

            for (int i = functionToExecute.Parameters.Count - 1; i >= 0; i--)
            {
                string paramName = functionToExecute.Parameters[i];
                RuntimeValue argValue = _operandStack.Pop();
                newFrame.Registers[paramName] = argValue;
            }

            _callStack.Push(newFrame);
            _cachedRegisters = newFrame.Registers;
            _ip = functionToExecute.StartAddress;
        }

        /// <summary>
        /// Resolves a variable name to its runtime value by searching the current scope hierarchy.
        /// The search order is: 1. Current function's local registers, 2. Lexical scopes (closures).
        /// </summary>
        /// <param name="name">The name of the variable to resolve.</param>
        /// <returns>The <see cref="RuntimeValue"/> associated with the variable name.</returns>
        /// <exception cref="FluenceRuntimeException">Thrown if the variable is not defined in any accessible scope.</exception>
        private RuntimeValue ResolveVariable(string name)
        {
            string internedName = name;

            // Check the current function's local variables.
            ref var localValue = ref CollectionsMarshal.GetValueRefOrNullRef(CurrentRegisters, internedName);
            if (!Unsafe.IsNullRef(ref localValue))
            {
                return localValue;
            }

            // Check the current function's lexical scope and its parents.
            var lexicalScope = CurrentFrame.Function.DefiningScope;
            if (lexicalScope.TryResolve(internedName, out var symbol))
            {
                // Conver the symbol into a runtime type.
                return symbol switch
                {
                    FunctionSymbol funcSymbol => new RuntimeValue(
                        funcSymbol.IsIntrinsic
                            // Create an intrinsic (C#) function object.
                            ? new FunctionObject(
                                funcSymbol.Name,
                                funcSymbol.Arity,
                                funcSymbol.IntrinsicBody,
                                CurrentFrame.Function.DefiningScope
                            )
                            // Create a user-defined (bytecode) function object.
                            : new FunctionObject(
                                funcSymbol.Name,
                                funcSymbol.Arity,
                                funcSymbol.Arguments,
                                funcSymbol.StartAddress,
                                CurrentFrame.Function.DefiningScope
                            )
                    ),
                    // In the future, other symbol types like global constants could be handled here.
                    _ => throw new FluenceRuntimeException($"Internal VM Error: Don't know how to create a runtime value for symbol of type '{symbol.GetType().Name}'.")
                };
            }

            throw new FluenceRuntimeException($"Runtime Error: Undefined variable '{name}'.");
        }

        /// <summary>
        /// Writes a value to a specified temporary register in the current call frame.
        /// This is a high-performance method using direct memory references.
        /// </summary>
        private void SetRegister(TempValue destination, RuntimeValue value)
        {
            ref var valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(CurrentRegisters, destination.TempName, out _);
            valueRef = value;
        }

        /// <summary>
        /// Assigns a value to a variable, correctly handling local vs. global scope.
        /// </summary>
        private void AssignVariable(string name, RuntimeValue value)
        {
            // If we are not in the top-level script, assign to the current frame's local registers.
            if (CurrentFrame.Function.Name != "<script>")
            {
                ref var valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(CurrentRegisters, name, out _);
                valueRef = value;
            }
            else // For globals...
            {
                ref var valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_globals, name, out _);
                valueRef = value;
            }
        }

        /// <summary>
        /// A generic handler for all binary numeric operations. It correctly handles type promotion
        ///  and dispatches to the appropriate C# operator.
        /// </summary>
        private void ExecuteNumericBinaryOperation(
            InstructionLine instruction, RuntimeValue left, RuntimeValue right,
            Func<int, int, RuntimeValue> intOp,
            Func<long, long, RuntimeValue> longOp,
            Func<float, float, RuntimeValue> floatOp,
            Func<double, double, RuntimeValue> doubleOp)
        {
            TempValue desination = (TempValue)instruction.Lhs;

            if (left.Type != RuntimeValueType.Number || right.Type != RuntimeValueType.Number)
            {
                throw new FluenceRuntimeException($"Internal VM Error: ExecuteNumericBinaryOperation called with non-numeric types.");
            }

            var result = left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => intOp(left.IntValue, right.IntValue),
                    RuntimeNumberType.Long => longOp(left.IntValue, right.LongValue),
                    RuntimeNumberType.Float => floatOp(left.IntValue, right.FloatValue),
                    RuntimeNumberType.Double => doubleOp(left.IntValue, right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => longOp(left.LongValue, right.IntValue),
                    RuntimeNumberType.Long => longOp(left.LongValue, right.LongValue),
                    RuntimeNumberType.Float => floatOp(left.LongValue, right.FloatValue),
                    RuntimeNumberType.Double => doubleOp(left.LongValue, right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => floatOp(left.FloatValue, right.IntValue),
                    RuntimeNumberType.Long => floatOp(left.FloatValue, right.LongValue),
                    RuntimeNumberType.Float => floatOp(left.FloatValue, right.FloatValue),
                    RuntimeNumberType.Double => doubleOp(left.FloatValue, right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => doubleOp(left.DoubleValue, right.IntValue),
                    RuntimeNumberType.Long => doubleOp(left.DoubleValue, right.LongValue),
                    RuntimeNumberType.Float => doubleOp(left.DoubleValue, right.FloatValue),
                    RuntimeNumberType.Double => doubleOp(left.DoubleValue, right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type."),
                },
                _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported left-hand number type."),
            };
            SetRegister((TempValue)instruction.Lhs, result);
        }

        /// <summary>Handles the ADD instruction, which performs numeric addition, string concatenation, or list concatenation.</summary>
        private void ExecuteAdd(InstructionLine instruction)
        {
            var left = GetRuntimeValue(instruction.Rhs);
            var right = GetRuntimeValue(instruction.Rhs2);

            if (left.Type == RuntimeValueType.Number && right.Type == RuntimeValueType.Number)
            {
                ExecuteNumericBinaryOperation(
                    instruction, left, right,
                    (a, b) => new RuntimeValue(a + b),              // int + int -> int.
                    (a, b) => new RuntimeValue(a + b),              // long + long -> long.
                    (a, b) => new RuntimeValue(a + b),              // float + float -> float.
                    (a, b) => new RuntimeValue(a + b)               // double + double -> double.
                );
                return;
            }

            // String concatenation.
            if ((left.Type == RuntimeValueType.Object && left.Is<StringObject>()) ||
                (right.Type == RuntimeValueType.Object && right.Is<StringObject>()))
            {
                string resultString = string.Concat(left.ToString(), right.ToString());
                SetRegister((TempValue)instruction.Lhs, new RuntimeValue(new StringObject(resultString)));
                return;
            }

            if (left.Type == RuntimeValueType.Object && left.ObjectReference is ListObject leftList &&
                right.Type == RuntimeValueType.Object && right.ObjectReference is ListObject rightList)
            {
                var concatenatedList = new ListObject();

                concatenatedList.Elements.AddRange(leftList.Elements);
                concatenatedList.Elements.AddRange(rightList.Elements);
                SetRegister((TempValue)instruction.Lhs, new RuntimeValue(concatenatedList));
                return;
            }

            throw new FluenceRuntimeException($"Runtime Error: Cannot apply operator '+' to types {GetDetailedTypeName(left)} and {GetDetailedTypeName(right)}.");
        }

        /// <summary>Handles the SUBTRACT instruction for numeric subtraction or list difference.</summary>
        private void ExecuteSubtraction(InstructionLine instruction)
        {
            var left = GetRuntimeValue(instruction.Rhs);
            var right = GetRuntimeValue(instruction.Rhs2);

            if (left.Type == RuntimeValueType.Number && right.Type == RuntimeValueType.Number)
            {
                ExecuteNumericBinaryOperation(
                    instruction, left, right,
                    (a, b) => new RuntimeValue(a - b),
                    (a, b) => new RuntimeValue(a - b),
                    (a, b) => new RuntimeValue(a - b),
                    (a, b) => new RuntimeValue(a - b)
                );
                return;
            }

            if (left.Type == RuntimeValueType.Object && left.ObjectReference is ListObject leftList &&
                right.Type == RuntimeValueType.Object && right.ObjectReference is ListObject rightList)
            {
                var concatenatedList = new ListObject();
                // This performs a set difference, which is the intuitive meaning of list subtraction.
                concatenatedList.Elements.AddRange(leftList.Elements.Except(rightList.Elements));
                SetRegister((TempValue)instruction.Lhs, new RuntimeValue(concatenatedList));
                return;
            }

            throw new FluenceRuntimeException($"Runtime Error: Cannot apply operator '-' to types {GetDetailedTypeName(left)} and {GetDetailedTypeName(right)}.");
        }

        /// <summary>Handles the MULTIPLY instruction for numeric multiplication or string/list repetition.</summary>
        private void ExecuteMultiplication(InstructionLine instruction)
        {
            var left = GetRuntimeValue(instruction.Rhs);
            var right = GetRuntimeValue(instruction.Rhs2);

            if (left.Type == RuntimeValueType.Number && right.Type == RuntimeValueType.Number)
            {
                ExecuteNumericBinaryOperation(instruction, left, right,
                    (a, b) => new RuntimeValue(a * b),
                    (a, b) => new RuntimeValue(a * b),
                    (a, b) => new RuntimeValue(a * b),
                    (a, b) => new RuntimeValue(a * b)
                );
                return;
            }

            if (left.ObjectReference is StringObject strLeft && right.Type == RuntimeValueType.Number)
            {
                SetRegister((TempValue)instruction.Lhs, HandleStringRepetition(strLeft, right));
                return;
            }
            if (left.Type == RuntimeValueType.Number && right.ObjectReference is StringObject strRight)
            {
                SetRegister((TempValue)instruction.Lhs, HandleStringRepetition(strRight, left));
                return;
            }

            if (left.ObjectReference is ListObject listLeft && right.Type == RuntimeValueType.Number)
            {
                SetRegister((TempValue)instruction.Lhs, HandleListRepetition(listLeft, right));
                return;
            }
            if (left.Type == RuntimeValueType.Number && right.ObjectReference is ListObject listRight)
            {
                SetRegister((TempValue)instruction.Lhs, HandleListRepetition(listRight, left));
                return;
            }
            throw new FluenceRuntimeException($"Runtime Error: Cannot apply operator '*' to types {GetDetailedTypeName(left)} and {GetDetailedTypeName(right)}.");
        }

        /// <summary>Handles the DIVIDE instruction.</summary>
        private void ExecuteDivision(InstructionLine instruction)
        {
            var left = GetRuntimeValue(instruction.Rhs);
            var right = GetRuntimeValue(instruction.Rhs2);
            ExecuteNumericBinaryOperation(
                instruction, left, right,
                (a, b) => new RuntimeValue(a / b),
                (a, b) => new RuntimeValue(a / b),
                (a, b) => new RuntimeValue(a / b),
                (a, b) => new RuntimeValue(a / b)
            );
        }

        /// <summary>Handles the MODULO instruction.</summary>
        private void ExecuteModulo(InstructionLine instruction)
        {
            var left = GetRuntimeValue(instruction.Rhs);
            var right = GetRuntimeValue(instruction.Rhs2);
            ExecuteNumericBinaryOperation(
                instruction, left, right,
                (a, b) => new RuntimeValue(a % b),
                (a, b) => new RuntimeValue(a % b),
                (a, b) => new RuntimeValue(a % b),
                (a, b) => new RuntimeValue(a % b)
            );
        }

        /// <summary>Handles the POWER instruction.</summary>
        private void ExecutePower(InstructionLine instruction)
        {
            var left = GetRuntimeValue(instruction.Rhs);
            var right = GetRuntimeValue(instruction.Rhs2);

            ExecuteNumericBinaryOperation(
                instruction, left, right,
                (a, b) => new RuntimeValue(Math.Pow(a, b)),
                (a, b) => new RuntimeValue(Math.Pow(a, b)),
                (a, b) => new RuntimeValue(Math.Pow(a, b)),
                (a, b) => new RuntimeValue(Math.Pow(a, b))
            );
        }

        /// <summary>Handles the ASSIGN instruction, which is used for variable assignment and range-to-list expansion.</summary>
        private void ExecuteAssign(InstructionLine instruction)
        {
            var sourceValue = GetRuntimeValue(instruction.Rhs);
            if (instruction.Lhs is VariableValue destVar && sourceValue.ObjectReference is RangeObject range)
            {
                ListObject list = new ListObject();
                RuntimeValue startValue = range.Start;
                RuntimeValue endValue = range.End;

                if (startValue.Type != RuntimeValueType.Number || endValue.Type != RuntimeValueType.Number)
                {
                    throw new FluenceRuntimeException($"Runtime Error: Range bounds must be numbers, not {GetDetailedTypeName(range.Start)} and {GetDetailedTypeName(range.End)}.");
                }

                int start = Convert.ToInt32(startValue.IntValue);
                int end = Convert.ToInt32(endValue.IntValue);

                if (start <= end)
                {
                    for (int i = start; i <= end; i++)
                    {
                        list.Elements.Add(new RuntimeValue(i));
                    }
                }
                else // Decreasing range
                {
                    for (int i = start; i >= end; i--)
                    {
                        list.Elements.Add(new RuntimeValue(i));
                    }
                }
                AssignVariable(destVar.Name, new RuntimeValue(list));
                return;
            }
            // Standard assignment.
            else if (instruction.Lhs is VariableValue destVar2)
            {
                AssignVariable(destVar2.Name, sourceValue);
            }
            else if (instruction.Lhs is TempValue destTemp) AssignVariable(destTemp.TempName, sourceValue);
            else throw new FluenceRuntimeException("Internal VM Error: Destination of 'Assign' must be a variable or temporary.");
        }

        /// <summary>
        /// A unified handler for all bitwise operations (~, &amp;, |, ^, &lt;&lt;, &gt;&gt;).
        /// </summary>
        private void ExecuteBitwiseOperation(InstructionLine instruction)
        {
            var leftValue = GetRuntimeValue(instruction.Rhs);
            long leftLong = ToLong(leftValue);

            if (instruction.Instruction == InstructionCode.BitwiseNot)
            {
                SetRegister((TempValue)instruction.Lhs, new RuntimeValue(~leftLong));
                return;
            }

            var rightValue = GetRuntimeValue(instruction.Rhs2);
            var result = instruction.Instruction switch
            {
                InstructionCode.BitwiseAnd => leftLong & ToLong(rightValue),
                InstructionCode.BitwiseOr => leftLong | ToLong(rightValue),
                InstructionCode.BitwiseXor => leftLong ^ ToLong(rightValue),
                InstructionCode.BitwiseLShift => leftLong << ToInt(rightValue),
                InstructionCode.BitwiseRShift => leftLong >> ToInt(rightValue),
                _ => throw new FluenceRuntimeException("Internal VM Error: Unhandled bitwise operation routed to ExecuteBitwiseOperation."),
            };

            SetRegister((TempValue)instruction.Lhs, new RuntimeValue(result));
        }

        /// <summary>
        /// Handles the NEGATE instruction (unary minus).
        /// </summary>
        private void ExecuteNegate(InstructionLine instruction)
        {
            var destination = (TempValue)instruction.Lhs;
            RuntimeValue value = GetRuntimeValue(instruction.Rhs);

            if (value.Type != RuntimeValueType.Number)
            {
                throw new FluenceRuntimeException($"Runtime Error: The unary minus operator '-' cannot be applied to a value of type '{GetDetailedTypeName(value)}'.");
            }

            RuntimeValue result = value.NumberType switch
            {
                RuntimeNumberType.Int => new RuntimeValue(-value.IntValue),
                RuntimeNumberType.Double => new RuntimeValue(-value.DoubleValue),
                RuntimeNumberType.Float => new RuntimeValue(-value.FloatValue),
                RuntimeNumberType.Long => new RuntimeValue(-value.LongValue),
                _ => throw new FluenceRuntimeException("Internal VM Error: Invalid number type for negate operation.")
            };

            SetRegister(destination, result);
        }

        /// <summary>
        /// Handles the NOT instruction (logical not).
        /// </summary>
        private void ExecuteNot(InstructionLine instruction)
        {
            RuntimeValue value = GetRuntimeValue(instruction.Rhs);
            SetRegister((TempValue)instruction.Lhs, new RuntimeValue(!value.IsTruthy));
        }

        /// <summary>
        /// Handles the GOTO_IF_TRUE and GOTO_IF_FALSE instructions for conditional jumps.
        /// </summary>
        /// <param name="requiredCondition">The truthiness value that triggers the jump (true for GotoIfTrue, false for GotoIfFalse).</param>
        private void ExecuteGotoIf(InstructionLine instruction, bool requiredCondition)
        {
            RuntimeValue condition = GetRuntimeValue(instruction.Rhs);

            if (instruction.Lhs is not NumberValue target)
            {
                throw new FluenceRuntimeException("Internal VM Error: The target of a jump instruction must be a NumberValue.");
            }

            if (condition.IsTruthy == requiredCondition)
            {
                _ip = (int)target.Value;
            }
        }

        /// <summary>
        /// Handles the GOTO instruction for unconditional jumps.
        /// </summary>
        private void ExecuteGoto(InstructionLine instruction)
        {
            if (instruction.Lhs is not NumberValue target)
            {
                throw new FluenceRuntimeException("Internal VM Error: The target of a GOTO instruction must be a NumberValue.");
            }
            _ip = (int)target.Value;
        }

        /// <summary>
        /// Handles the AND and OR logical instructions with short-circuiting behavior.
        /// </summary>
        /// <param name="isAnd">True if the operation is a logical AND, false for logical OR.</param>
        private void ExecuteLogicalOp(InstructionLine instruction, bool isAnd)
        {
            RuntimeValue left = GetRuntimeValue(instruction.Rhs);
            // Short-circuiting for efficiency.
            if (isAnd)
            {
                if (!left.IsTruthy)
                {
                    SetRegister((TempValue)instruction.Lhs, new RuntimeValue(false));
                    return;
                }
            }
            else // is OR
            {
                if (left.IsTruthy)
                {
                    SetRegister((TempValue)instruction.Lhs, new RuntimeValue(true));
                    return;
                }
            }

            // If we didn't short-circuit, the result of the expression is the truthiness of the right-hand side.
            RuntimeValue right = GetRuntimeValue(instruction.Rhs2);
            SetRegister((TempValue)instruction.Lhs, new RuntimeValue(right.IsTruthy));
        }

        /// <summary>
        /// Handles the TO_STRING instruction, which explicitly converts any runtime value to a string object.
        /// </summary>
        private void ExecuteToString(InstructionLine instruction)
        {
            RuntimeValue valueToConvert = GetRuntimeValue(instruction.Rhs);
            string resultString = valueToConvert.ToString();
            SetRegister((TempValue)instruction.Lhs, new RuntimeValue(new StringObject(resultString)));
        }

        /// <summary>
        /// Handles the EQUAL and NOT_EQUAL instructions.
        /// </summary>
        /// <param name="isEqual">True if the operation is for equality (==), false for inequality (!=).</param>
        private void ExecuteEqualityComparison(InstructionLine instruction, bool isEqual)
        {
            RuntimeValue left = GetRuntimeValue(instruction.Rhs);
            RuntimeValue right = GetRuntimeValue(instruction.Rhs2);
            bool result = left.Equals(right);
            SetRegister((TempValue)instruction.Lhs, new RuntimeValue(isEqual ? result : !result));
        }

        /// <summary>Handles the GREATER_THAN instruction.</summary>
        private void ExecuteGreaterThan(InstructionLine instruction)
        {
            ExecuteNumericComparison(instruction,
                (a, b) => a > b,
                (a, b) => a > b,
                (a, b) => a > b,
                (a, b) => a > b
            );
        }

        /// <summary>Handles the GREATER_EQUAL instruction.</summary>
        private void ExecuteGreaterEqual(InstructionLine instruction)
        {
            ExecuteNumericComparison(instruction,
                (a, b) => a >= b,
                (a, b) => a >= b,
                (a, b) => a >= b,
                (a, b) => a >= b
            );
        }

        /// <summary>Handles the LESS_THAN instruction.</summary>
        private void ExecuteLessThan(InstructionLine instruction)
        {
            ExecuteNumericComparison(instruction,
                (a, b) => a < b,
                (a, b) => a < b,
                (a, b) => a < b,
                (a, b) => a < b
            );
        }

        /// <summary>Handles the LESS_EQUAL instruction.</summary>
        private void ExecuteLessEqual(InstructionLine instruction)
        {
            ExecuteNumericComparison(instruction,
                (a, b) => a <= b,
                (a, b) => a <= b,
                (a, b) => a <= b,
                (a, b) => a <= b
            );
        }

        /// <summary>
        /// Handles the NEW_LIST instruction, creating a new, empty list object.
        /// </summary>
        private void ExecuteNewList(InstructionLine instruction)
        {
            SetRegister((TempValue)instruction.Lhs, new RuntimeValue(new ListObject()));
        }

        /// <summary>
        /// A generic handler for all relational comparison operations (&gt;, &gt;=, &lt;, &lt;=).
        /// It correctly handles comparisons for both numbers and strings.
        /// </summary>
        private void ExecuteNumericComparison(
            InstructionLine instruction,
            Func<int, int, bool> intOp,
            Func<long, long, bool> longOp,
            Func<float, float, bool> floatOp,
            Func<double, double, bool> doubleOp)
        {
            var destination = (TempValue)instruction.Lhs;
            var left = GetRuntimeValue(instruction.Rhs);
            var right = GetRuntimeValue(instruction.Rhs2);

            if (left.ObjectReference is StringObject leftStr && right.ObjectReference is StringObject rightStr)
            {
                bool stringResult = instruction.Instruction switch
                {
                    InstructionCode.LessThan => string.CompareOrdinal(leftStr.Value, rightStr.Value) < 0,
                    InstructionCode.GreaterThan => string.CompareOrdinal(leftStr.Value, rightStr.Value) > 0,
                    InstructionCode.LessEqual => string.CompareOrdinal(leftStr.Value, rightStr.Value) <= 0,
                    InstructionCode.GreaterEqual => string.CompareOrdinal(leftStr.Value, rightStr.Value) >= 0,
                    _ => throw new FluenceRuntimeException("Internal VM Error: Invalid comparison instruction for strings.")
                };
                SetRegister(destination, new RuntimeValue(stringResult));
            }

            if (left.Type != RuntimeValueType.Number || right.Type != RuntimeValueType.Number)
            {
                throw new FluenceRuntimeException($"Internal VM Error: Cannot perform numeric comparison on non-number types: ({left.Type}, {right.Type}).");
            }

            var result = left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => intOp(left.IntValue, right.IntValue),
                    RuntimeNumberType.Long => longOp(left.IntValue, right.LongValue),
                    RuntimeNumberType.Float => floatOp(left.IntValue, right.FloatValue),
                    RuntimeNumberType.Double => doubleOp(left.IntValue, right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => longOp(left.LongValue, right.IntValue),
                    RuntimeNumberType.Long => longOp(left.LongValue, right.LongValue),
                    RuntimeNumberType.Float => floatOp(left.LongValue, right.FloatValue),
                    RuntimeNumberType.Double => doubleOp(left.LongValue, right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => floatOp(left.FloatValue, right.IntValue),
                    RuntimeNumberType.Long => floatOp(left.FloatValue, right.LongValue),
                    RuntimeNumberType.Float => floatOp(left.FloatValue, right.FloatValue),
                    RuntimeNumberType.Double => doubleOp(left.FloatValue, right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => doubleOp(left.DoubleValue, right.IntValue),
                    RuntimeNumberType.Long => doubleOp(left.DoubleValue, right.LongValue),
                    RuntimeNumberType.Float => doubleOp(left.DoubleValue, right.FloatValue),
                    RuntimeNumberType.Double => doubleOp(left.DoubleValue, right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type."),
                },
                _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported left-hand number type."),
            };
            SetRegister(destination, new RuntimeValue(result));
        }

        /// <summary>
        /// Handles the NEW_RANGE instruction, creating a runtime range object.
        /// </summary>
        private void ExecuteNewRange(InstructionLine instruction)
        {
            if (instruction.Rhs is null)
            {
                throw new FluenceRuntimeException("Internal VM Error: NewRange opcode requires a non-null RangeValue operand.");
            }

            RuntimeValue rangeRuntimeValue = GetRuntimeValue(instruction.Rhs);
            SetRegister((TempValue)instruction.Lhs, rangeRuntimeValue);
        }

        /// <summary>
        /// Handles the GET_LENGTH instruction for any collection that has a length (string, list, range).
        /// </summary>
        private void ExecuteGetLength(InstructionLine instruction)
        {
            RuntimeValue collection = GetRuntimeValue(instruction.Rhs);
            int length;

            if (collection.Type == RuntimeValueType.Object)
            {
                switch (collection.ObjectReference)
                {
                    case StringObject str:
                        length = str.Value.Length;
                        break;
                    case ListObject list:
                        length = list.Elements.Count;
                        break;
                    case RangeObject range:
                        if (range.Start.Type != RuntimeValueType.Number || range.End.Type != RuntimeValueType.Number)
                        {
                            throw new FluenceRuntimeException($"Runtime Error: Cannot get length of a range with non-numeric bounds ({GetDetailedTypeName(range.Start)}, {GetDetailedTypeName(range.End)}).");
                        }
                        int start = Convert.ToInt32(range.Start.IntValue);
                        int end = Convert.ToInt32(range.End.IntValue);
                        length = (end < start) ? 0 : (end - start + 1);
                        break;
                    default:
                        throw new FluenceRuntimeException($"Runtime Error: Cannot get the length of a value of type '{GetDetailedTypeName(collection)}'.");
                }
            }
            else
            {
                throw new FluenceRuntimeException($"Cannot get length of a non-object type '{collection.Type}'.");
            }

            SetRegister((TempValue)instruction.Lhs, new RuntimeValue(length));
        }

        /// <summary>
        /// Handles the PUSH_ELEMENT instruction, which adds an element or an expanded range to a list.
        /// </summary>
        private void ExecutePushElement(InstructionLine instruction)
        {
            RuntimeValue listVal = GetRuntimeValue(instruction.Lhs);
            if (listVal.ObjectReference is not ListObject list)
            {
                throw new FluenceRuntimeException($"Runtime Error: Cannot push an element to a non-list value (got type '{GetDetailedTypeName(listVal)}').");
            }

            RuntimeValue elementToAdd = GetRuntimeValue(instruction.Rhs);

            if (elementToAdd.ObjectReference is RangeObject range)
            {
                RuntimeValue startValue = range.Start;
                RuntimeValue endValue = range.End;

                if (startValue.Type != RuntimeValueType.Number || endValue.Type != RuntimeValueType.Number)
                {
                    throw new FluenceRuntimeException($"Runtime Error: Range bounds must be numbers, not {GetDetailedTypeName(range.Start)} and {GetDetailedTypeName(range.End)}.");
                }

                int start = Convert.ToInt32(startValue.IntValue);
                int end = Convert.ToInt32(endValue.IntValue);

                if (start <= end)
                {
                    for (int i = start; i <= end; i++)
                    {
                        list.Elements.Add(new RuntimeValue(i));
                    }
                }
                else // Decreasing range.
                {
                    for (int i = start; i >= end; i--)
                    {
                        list.Elements.Add(new RuntimeValue(i));
                    }
                }
            }
            else
            {
                list.Elements.Add(elementToAdd);
            }
        }

        /// <summary>
        /// Handles the NEW_INSTANCE instruction, allocating a new, empty instance of a user-defined struct.
        /// </summary>
        private void ExecuteNewInstance(InstructionLine instruction)
        {
            // The RHS is the StructSymbol from the bytecode.
            if (instruction.Rhs is not StructSymbol classSymbol)
            {
                throw new FluenceRuntimeException("Internal VM Error: NewInstance requires a StructSymbol as its operand.");
            }

            var instance = new InstanceObject(classSymbol);
            SetRegister((TempValue)instruction.Lhs, new RuntimeValue(instance));
        }

        /// <summary>
        /// Handles the GET_FIELD instruction, which retrieves the value of a field or method from a struct instance.
        /// </summary>
        private void ExecuteGetField(InstructionLine instruction)
        {
            if (instruction.Rhs2 is not StringValue fieldName)
            {
                throw new FluenceRuntimeException("Internal VM Error: GetField requires a string literal for the field name.");
            }

            var instanceValue = GetRuntimeValue(instruction.Rhs);
            if (instanceValue.ObjectReference is not InstanceObject instance)
            {
                throw new FluenceRuntimeException($"Runtime Error: Cannot access property '{fieldName.Value}' on a non-instance value (got type '{GetDetailedTypeName(instanceValue)}').");
            }
            SetRegister((TempValue)instruction.Lhs, instance.GetField(fieldName.Value));
        }

        /// <summary>
        /// Handles the SET_FIELD instruction, which assigns a new value to a field of a struct instance.
        /// </summary>
        private void ExecuteSetField(InstructionLine instruction)
        {
            // Lhs: The instance to modify.
            // Rhs: The field name.
            // Rhs2: The new value.

            if (instruction.Rhs is not StringValue fieldName)
            {
                throw new FluenceRuntimeException("Internal VM Error: SetField requires a string literal for the field name.");
            }

            var instanceValue = GetRuntimeValue(instruction.Lhs);
            if (instanceValue.ObjectReference is not InstanceObject instance)
            {
                throw new FluenceRuntimeException($"Runtime Error: Cannot set property '{fieldName.Value}' on a non-instance value (got type '{GetDetailedTypeName(instanceValue)}').");
            }

            var valueToSet = GetRuntimeValue(instruction.Rhs2);
            instance.SetField(fieldName.Value, valueToSet);
        }

        /// <summary>
        /// Handles the GET_ELEMENT instruction for retrieving an element from a list by its index.
        /// </summary>
        private void ExecuteGetElement(InstructionLine instruction)
        {
            RuntimeValue collection = GetRuntimeValue(instruction.Rhs);
            RuntimeValue indexVal = GetRuntimeValue(instruction.Rhs2);

            if (collection.As<ListObject>() is not ListObject list)
            {
                throw new FluenceRuntimeException($"Internal VM Error: Cannot apply index operator [...] to a non-list value (got type '{GetDetailedTypeName(collection)}').");
            }

            if (indexVal.Type != RuntimeValueType.Number)
            {
                throw new FluenceRuntimeException($"Runtime Error: List index must be a number, not '{GetDetailedTypeName(indexVal)}'.");
            }

            int index = indexVal.IntValue;

            if (index < 0 || index >= list.Elements.Count)
            {
                throw new FluenceRuntimeException($"Runtime Error: Index out of range. Index was {index}, but list size is {list.Elements.Count}.");
            }

            SetRegister((TempValue)instruction.Lhs, list.Elements[index]);
        }

        /// <summary>
        /// Handles the SET_ELEMENT instruction for updating an element in a list at a given index.
        /// </summary>
        private void ExecuteSetElement(InstructionLine instruction)
        {
            RuntimeValue collection = GetRuntimeValue(instruction.Lhs);
            RuntimeValue indexVal = GetRuntimeValue(instruction.Rhs);
            RuntimeValue valueToSet = GetRuntimeValue(instruction.Rhs2);

            if (collection.As<ListObject>() is not ListObject list)
            {
                throw new FluenceRuntimeException($"Internal VM Error: Cannot apply index operator [...] to a non-list value (got type '{GetDetailedTypeName(collection)}').");
            }

            if (indexVal.Type != RuntimeValueType.Number)
            {
                throw new FluenceRuntimeException($"Runtime Error: List index must be a number, not '{GetDetailedTypeName(indexVal)}'.");
            }

            int index = indexVal.IntValue;

            if (index < 0 || index >= list.Elements.Count)
            {
                throw new FluenceRuntimeException($"Runtime Error: Index out of range. Index was {index}, but list size is {list.Elements.Count}.");
            }

            list.Elements[index] = valueToSet;
        }

        /// <summary>
        /// Handles the NEW_ITERATOR instruction, creating an iterator object for a for-in loop.
        /// </summary>
        private void ExecuteNewIterator(InstructionLine instruction)
        {
            RuntimeValue iterable = GetRuntimeValue(instruction.Rhs);

            if (iterable.ObjectReference is ListObject || iterable.ObjectReference is RangeObject)
            {
                var iterator = new IteratorObject(iterable.ObjectReference);
                SetRegister((TempValue)instruction.Lhs, new RuntimeValue(iterator));
                return;
            }

            throw new FluenceRuntimeException($"Runtime Error: Cannot create an iterator from a non-iterable type '{GetDetailedTypeName(iterable)}'.");
        }

        /// <summary>
        /// Handles the ITER_NEXT instruction, which advances an iterator and retrieves the next value.
        /// </summary>
        private void ExecuteIterNext(InstructionLine instruction)
        {
            // Lhs:  The source iterator register.
            // Rhs:  The destination register for the value.
            // Rhs2: The destination register for the continue flag.

            if (instruction.Lhs is not TempValue iteratorReg ||
                instruction.Rhs is not TempValue valueReg ||
                instruction.Rhs2 is not TempValue continueFlagReg)
            {
                throw new FluenceRuntimeException("Internal VM Error: Invalid operands for IterNext. Expected (Source Iterator, Dest Value, Dest Flag).");
            }

            RuntimeValue iteratorVal = CurrentRegisters[iteratorReg.TempName];

            if (iteratorVal.As<IteratorObject>() is not IteratorObject iterator)
            {
                throw new FluenceRuntimeException("Internal VM Error: Attempted to iterate over a non-iterator value.");
            }

            bool continueLoop = false;
            RuntimeValue nextValue = RuntimeValue.Nil;

            switch (iterator.Iterable)
            {
                case RangeObject range:
                    int start = Convert.ToInt32(range.Start.IntValue);
                    int end = Convert.ToInt32(range.End.IntValue);
                    int currentValue = start + iterator.CurrentIndex;

                    if (start <= end ? currentValue <= end : currentValue >= end)
                    {
                        nextValue = new RuntimeValue(currentValue);
                        continueLoop = true;
                        iterator.CurrentIndex += start <= end ? 1 : -1;
                    }
                    break;
                case ListObject list:
                    if (iterator.CurrentIndex < list.Elements.Count)
                    {
                        nextValue = list.Elements[iterator.CurrentIndex];
                        continueLoop = true;
                        iterator.CurrentIndex++;
                    }
                    break;
            }

            CurrentRegisters[valueReg.TempName] = nextValue;
            CurrentRegisters[continueFlagReg.TempName] = new RuntimeValue(continueLoop);
        }

        /// <summary>
        /// Handles the PUSH_PARAM instruction, which pushes a value onto the operand stack in preparation for a function call.
        /// </summary>
        private void ExecutePushParam(InstructionLine instruction)
        {
            var valueToPush = GetRuntimeValue(instruction.Lhs);
            _operandStack.Push(valueToPush);
        }

        /// <summary>
        /// Handles the CALL_FUNCTION instruction, which invokes a standalone function.
        /// </summary>
        private void ExecuteCallFunction(InstructionLine instruction)
        {
            var functionVal = GetRuntimeValue(instruction.Rhs);
            if (functionVal.As<FunctionObject>() is not FunctionObject function)
            {
                throw new FluenceRuntimeException($"Internal VM Error: Attempted to call a value that is not a function (got type '{GetDetailedTypeName(functionVal)}').");
            }

            int argCount = GetRuntimeValue(instruction.Rhs2).IntValue;

            if (function.Arity != argCount)
            {
                throw new FluenceRuntimeException($"Internal VM Error: Mismatched arguments for function '{function.Name}'. Expected {function.Arity}, but got {argCount}.");
            }

            if (function.IsIntrinsic)
            {
                var args = new List<Value>(argCount);
                for (int i = 0; i < argCount; i++)
                {
                    // Intrinsics work with the raw `Value` types.
                    args.Add(ToValue(_operandStack.Pop()));
                }
                args.Reverse(); // The stack gives them in reverse order, so we fix it.

                Value resultValue = function.IntrinsicBody(args);
                SetRegister((TempValue)instruction.Lhs, GetRuntimeValue(resultValue));
                return;
            }

            var newFrame = new CallFrame(function, _ip, (TempValue)instruction.Lhs);

            for (int i = function.Parameters.Count - 1; i >= 0; i--)
            {
                string paramName = function.Parameters[i];
                RuntimeValue argValue = _operandStack.Pop();
                newFrame.Registers[paramName] = argValue;
            }

            _callStack.Push(newFrame);
            _cachedRegisters = newFrame.Registers;
            _ip = function.StartAddress;
        }

        /// <summary>
        /// Handles the RETURN instruction, which ends the current function's execution.
        /// </summary>
        private void ExecuteReturn(InstructionLine instruction)
        {
            RuntimeValue returnValue = GetRuntimeValue(instruction.Lhs);

            CallFrame finishedFrame = _callStack.Pop();
            _cachedRegisters = _callStack.Peek().Registers;

            if (_callStack.Count == 0)
            {
                // This means we are trying to return from the top-level script.
                // In this case, we can treat it like a Terminate.
                _ip = _byteCode.Count;
                return;
            }

            if (finishedFrame.DestinationRegister != null)
            {
                CurrentRegisters[finishedFrame.DestinationRegister.TempName] = returnValue;
            }

            _ip = finishedFrame.ReturnAddress;

            if (instruction.Lhs is TempValue destination)
            {
                CurrentRegisters[destination.TempName] = returnValue;
            }
        }

        /// <summary>
        /// Helper to safely convert any numeric RuntimeValue to a long for bitwise operations.
        /// </summary>
        private static long ToLong(RuntimeValue value)
        {
            if (value.Type != RuntimeValueType.Number)
            {
                throw new FluenceRuntimeException($"Internal VM Error: Bitwise operations require integer numbers, but got a {value.Type}.");
            }

            return value.NumberType switch
            {
                RuntimeNumberType.Int => value.IntValue,
                RuntimeNumberType.Long => value.LongValue,
                // Floats and doubles are truncated (decimal part is cut off).
                RuntimeNumberType.Float => (long)value.FloatValue,
                RuntimeNumberType.Double => (long)value.DoubleValue,
                _ => throw new FluenceRuntimeException("Internal VM Error: Unhandled number type in bitwise op.")
            };
        }

        /// <summary>
        /// Helper to safely convert any numeric RuntimeValue to an int for bit shift amounts.
        /// </summary>
        private static int ToInt(RuntimeValue value)
        {
            if (value.Type != RuntimeValueType.Number)
            {
                throw new FluenceRuntimeException($"Internal VM Error: Left or Right Bit Shift amount must be an integer number, but got a {value.Type}.");
            }

            return (int)ToLong(value);
        }

        /// <summary>
        /// Handles the logic for repeating a list's elements N times.
        /// </summary>
        private static RuntimeValue HandleListRepetition(ListObject list, RuntimeValue num)
        {
            if (num.NumberType != RuntimeNumberType.Int && num.NumberType != RuntimeNumberType.Long)
            {
                throw new FluenceRuntimeException($"Internal VM Error: Cannot multiply a list by a non-integer number ({num.NumberType}).");
            }

            int count = Convert.ToInt32(num.IntValue);
            var repeatedList = new ListObject();

            if (count > 0)
            {
                // Pre-allocate capacity for efficiency.
                repeatedList.Elements.Capacity = list.Elements.Count * count;
                for (int i = 0; i < count; i++)
                {
                    repeatedList.Elements.AddRange(list.Elements);
                }
            }

            // Multiplying by 0 or a negative number results in an empty list.
            return new RuntimeValue(repeatedList);
        }

        /// <summary>
        /// Helper for string/list repetition. Throws a user-friendly runtime exception for non-integer multipliers.
        /// </summary>
        private static RuntimeValue HandleStringRepetition(StringObject str, RuntimeValue num)
        {
            if (num.NumberType != RuntimeNumberType.Int && num.NumberType != RuntimeNumberType.Long)
            {
                throw new FluenceRuntimeException($"Internal VM Error: Cannot multiply a string by a non-integer number (got {num.NumberType}).");
            }

            int count = Convert.ToInt32(num.IntValue);

            // Multiplying by 0 or a negative number results in an empty string.
            if (count <= 0)
            {
                return new RuntimeValue(new StringObject(""));
            }

            var sb = new StringBuilder(str.Value.Length * count);
            for (int i = 0; i < count; i++)
            {
                sb.Append(str.Value);
            }

            return new RuntimeValue(new StringObject(sb.ToString()));
        }

        /// <summary>
        /// Gets a detailed, user-friendly type name for a runtime value.
        /// </summary>
        private static string GetDetailedTypeName(RuntimeValue value)
        {
            if (value.Type == RuntimeValueType.Object && value.ObjectReference != null)
            {
                return value.ObjectReference.GetType().Name;
            }

            // For primitives.
            return value.Type.ToString();
        }

        /// <summary>
        /// Converts a <see cref="RuntimeValue"/> back into a compile-time <see cref="Value"/>.
        /// This is primarily used for passing arguments to intrinsic functions that expect the parser's types.
        /// </summary>
        private Value ToValue(RuntimeValue rtValue)
        {
            return rtValue.Type switch
            {
                RuntimeValueType.Nil => new NilValue(),
                RuntimeValueType.Boolean => new BooleanValue(rtValue.IntValue != 0),
                RuntimeValueType.Number => new NumberValue(rtValue.NumberType switch
                {
                    RuntimeNumberType.Int => rtValue.IntValue,
                    RuntimeNumberType.Long => rtValue.LongValue,
                    RuntimeNumberType.Float => rtValue.FloatValue,
                    _ => rtValue.DoubleValue
                }, rtValue.NumberType switch
                {
                    RuntimeNumberType.Int => NumberValue.NumberType.Integer,
                    RuntimeNumberType.Float => NumberValue.NumberType.Float,
                    _ => NumberValue.NumberType.Double,
                }),
                RuntimeValueType.Object when rtValue.ObjectReference is StringObject str => new StringValue(str.Value),
                RuntimeValueType.Object when rtValue.ObjectReference is ListObject list => new ListValue(list.Elements.Select(ToValue).ToList()),
                RuntimeValueType.Object when rtValue.ObjectReference is InstanceObject instance => new StructValue(instance.Class, instance._fields),
                _ => throw new FluenceRuntimeException($"Internal VM Error: Cannot convert runtime type '{rtValue.Type}' back to a bytecode value.")
            };
        }
    }
}