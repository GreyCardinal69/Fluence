using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static Fluence.FluenceByteCode;
using static Fluence.FluenceByteCode.InstructionLine;
using static Fluence.FluenceInterpreter;
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

        /// <summary>
        /// A cache to store the readonly status of variables in the global scope.
        /// </summary>
        internal readonly Dictionary<string, bool> GlobalWritableCache = new();

        /// <summary> The list of all namespaces in the source code. </summary>
        private readonly Dictionary<string, FluenceScope> Namespaces;

        /// <summary>The Instruction Pointer, which holds the address of the *next* instruction to be executed.</summary>
        private int _ip;

        /// <summary>Gets the currently active call frame from the top of the call stack.</summary>
        internal CallFrame CurrentFrame => _callStack.Peek();

        /// <summary>Gets the registers for the current call frame via the cached reference.</summary>
        internal Dictionary<string, RuntimeValue> CurrentRegisters => _cachedRegisters;

        /// <summary>
        /// The current state of the Virtual Machine.
        /// </summary>
        public FluenceVMState State { get; private set; } = FluenceVMState.NotStarted;

        /// <summary>
        /// A flag that, when set to true, will cause the execution loop to pause at the next instruction.
        /// </summary>
        private bool _stopRequested;

        /// <summary>The delegate method used for non-newline output.</summary>
        private readonly TextOutputMethod _output;

        /// <summary>The delegate method used for line-based output.</summary>
        private readonly TextOutputMethod _outputLine;

        /// <summary>The delegate method used to receive input.</summary>
        private readonly TextInputMethod _input;

#if DEBUG
        //
        //      Debug fields, useful for measuring performance, but for release builds only a waste of memory and performance.
        //

        private readonly Dictionary<InstructionCode, long> _instructionTimings = new();
        private readonly Dictionary<InstructionCode, long> _instructionCounts = new();

        /// <summary>
        /// A debug stopwatch measuring the approximate time the Virtual Machine has been running for.
        /// </summary>
        private readonly Stopwatch _stopwatch = new();

        /// <summary>
        /// Dumps a detailed performance profile to the console, showing instruction counts,
        /// total time spent, and average time per instruction. This should be called AFTER Run() completes.
        /// </summary>
        public void DumpPerformanceProfile()
        {
            _outputLine("\n--- FLUENCE VM EXECUTION PROFILE ---");

            if (_instructionCounts.Count == 0)
            {
                _outputLine("No instructions were executed or profiling was not enabled.");
                return;
            }

            long totalInstructions = 0;
            long totalTicks = 0;
            var profileData = new List<(InstructionCode OpCode, long Count, long Ticks)>();

            foreach (var kvp in _instructionCounts)
            {
                long ticks = _instructionTimings.GetValueOrDefault(kvp.Key, 0);
                profileData.Add((kvp.Key, kvp.Value, ticks));
                totalInstructions += kvp.Value;
                totalTicks += ticks;
            }

            _outputLine($"Total Instructions Executed: {totalInstructions:N0}");
            _outputLine($"Total Execution Time: {new TimeSpan(totalTicks).TotalMilliseconds:N3} ms\n");

            _outputLine($"{"OpCode",-20} | {"Count",-15} | {"% of Total",-12} | {"Total Time (ms)",-18} | {"% of Time",-12} | {"Avg. Ticks/Op",-15}");
            _outputLine(new string('-', 100));

            profileData.Sort((a, b) => b.Ticks.CompareTo(a.Ticks));

            foreach (var (OpCode, Count, Ticks) in profileData)
            {
                double percentOfTotalCount = (double)Count / totalInstructions * 100;
                double totalMs = new TimeSpan(Ticks).TotalMilliseconds;
                double percentOfTotalTime = (double)Ticks / totalTicks * 100;
                double avgTicksPerOp = (double)Ticks / Count;

                string opCodeStr = OpCode.ToString();
                string countStr = Count.ToString("N0");
                string percentCountStr = $"{percentOfTotalCount:F2}%";
                string totalMsStr = totalMs.ToString("N4");
                string percentTimeStr = $"{percentOfTotalTime:F2}%";
                string avgTicksStr = avgTicksPerOp.ToString("F2");

                _outputLine($"{opCodeStr,-20} | {countStr,-15} | {percentCountStr,-12} | {totalMsStr,-18} | {percentTimeStr,-12} | {avgTicksStr,-15}");
            }
            _outputLine("--------------------------------------\n");
        }
#endif

        /// <summary>
        /// Represents the state of a single function call on the stack. It contains the function being executed,
        /// its local variables (registers), the return address, and the destination for the return value.
        /// </summary>
        internal readonly record struct CallFrame
        {
            internal Dictionary<string, RuntimeValue> Registers { get; } = new();
            internal readonly TempValue DestinationRegister;
            internal readonly FunctionObject Function;
            internal readonly int ReturnAddress;

            /// <summary>
            /// A cache to store the readonly status of variables in this scope.
            /// Key: variable name. Value: true if readonly, false if writable.
            /// This avoids expensive TryResolve calls on every assignment.
            /// </summary>
            internal readonly Dictionary<string, bool> WritableCache = new();

            internal CallFrame(FunctionObject function, int returnAddress, TempValue destination)
            {
                Function = function;
                ReturnAddress = returnAddress;
                DestinationRegister = destination;
            }
        }

        /// <summary>
        /// Captures the state of the virtual machine at the point of the creation of the object.
        /// </summary>
        internal readonly struct VMDebugContext
        {
            internal int InstructionPointer { get; }
            internal InstructionLine CurrentInstruction { get; }
            internal IReadOnlyDictionary<string, RuntimeValue> CurrentLocals { get; }
            internal IReadOnlyList<RuntimeValue> OperandStackSnapshot { get; }
            internal int CallStackDepth { get; }
            internal string CurrentFunctionName { get; }

            internal VMDebugContext(FluenceVirtualMachine vm)
            {
                InstructionPointer = vm._ip - 1;
                CurrentInstruction = vm._byteCode[InstructionPointer];
                CurrentLocals = vm.CurrentRegisters;
                OperandStackSnapshot = [.. vm._operandStack];
                CallStackDepth = vm._callStack.Count;
                CurrentFunctionName = vm.CurrentFrame.Function.Name;
            }

            /// <summary>
            /// Formats the captured VM state into a detailed string for display.
            /// </summary>
            /// <returns>A formatted string representing the VM state.</returns>
            internal string DumpContext()
            {
                var sb = new StringBuilder();
                var separator = new string('-', 50);

                sb.AppendLine(separator);
                sb.AppendLine("--- FLUENCE VM STATE SNAPSHOT ---");
                sb.AppendLine(separator);

                sb.AppendLine($"IP: {InstructionPointer:D4}   Function: {CurrentFunctionName}   Call Stack Depth: {CallStackDepth}");
                sb.AppendLine($"Executing: {CurrentInstruction}");
                sb.AppendLine(separator);

                sb.AppendLine("OPERAND STACK (Top to Bottom):");
                if (OperandStackSnapshot.Count == 0)
                {
                    sb.AppendLine("  [Empty]");
                }
                else
                {
                    for (int i = 0; i < OperandStackSnapshot.Count; i++)
                    {
                        sb.AppendLine($"  [{i}]: {OperandStackSnapshot[i]}");
                    }
                }
                sb.AppendLine(separator);

                sb.AppendLine("CURRENT FRAME REGISTERS:");
                if (CurrentLocals.Count == 0)
                {
                    sb.AppendLine("  [No locals or temporaries]");
                }
                else
                {
                    int maxKeyLength = 0;
                    foreach (var key in CurrentLocals.Keys)
                    {
                        if (key.Length > maxKeyLength) maxKeyLength = key.Length;
                    }

                    var sortedLocals = CurrentLocals.OrderBy(kvp => kvp.Key);
                    foreach (var kvp in sortedLocals)
                    {
                        sb.AppendLine($"  {kvp.Key.PadRight(maxKeyLength)} : {kvp.Value}");
                    }
                }
                sb.AppendLine(separator);

                return sb.ToString();
            }
        }

        /// <summary>
        /// Initializes a new instance of the Fluence Virtual Machine.
        /// </summary>
        /// <param name="bytecode">The compiled bytecode to execute.</param>
        /// <param name="parseState">The final state from the parser, containing scope information.</param>
        /// <param name="output">The delegate to handle non-newline output.</param>
        /// <param name="outputLine">The delegate to handle line-based output.</param>
        /// <param name="input">The delegate to handle user input.</param>
        internal FluenceVirtualMachine(List<InstructionLine> bytecode, ParseState parseState, TextOutputMethod? output, TextOutputMethod? outputLine, TextInputMethod? input)
        {
            _byteCode = bytecode;
            _globalScope = parseState.GlobalScope;
            _globals = new Dictionary<string, RuntimeValue>();
            _callStack = new Stack<CallFrame>();

            _output = output ?? Console.Write;
            _outputLine = outputLine ?? Console.WriteLine;

            // TO DO, Needs testing.
            _input = input ?? Console.ReadLine!;

            // This represents the top-level global execution context.
            FunctionObject mainScriptFunc = new FunctionObject("<script>", 0, new List<string>(), 0, _globalScope);
            CallFrame initialFrame = new CallFrame(mainScriptFunc, _byteCode.Count, null!);
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

            _dispatchTable[(int)InstructionCode.CallStatic] = ExecuteCallStatic;
            _dispatchTable[(int)InstructionCode.GetStatic] = ExecuteGetStatic;
            _dispatchTable[(int)InstructionCode.SetStatic] = ExecuteSetStatic;

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

            //      ==!!==
            //      The following are unique opCodes generated only by the FluenceOptimizer.
            //      Some of these perfectly map to existing functions.
            //

            _dispatchTable[(int)InstructionCode.AddAssign] = ExecuteAdd;
            _dispatchTable[(int)InstructionCode.MulAssign] = ExecuteMultiplication;
            _dispatchTable[(int)InstructionCode.DivAssign] = ExecuteDivision;
            _dispatchTable[(int)InstructionCode.ModAssign] = ExecuteModulo;
            _dispatchTable[(int)InstructionCode.SubAssign] = ExecuteSubtraction;

            _dispatchTable[(int)InstructionCode.AssignTwo] = ExecuteAssignTwo;

            // Goto family.
            _dispatchTable[(int)InstructionCode.BranchIfEqual] = (inst) => ExecuteBranchIfEqual(inst, true);
            _dispatchTable[(int)InstructionCode.BranchIfNotEqual] = (inst) => ExecuteBranchIfEqual(inst, false);

            // Simple case for Terminate.
            _dispatchTable[(int)InstructionCode.Terminate] = (inst) => _ip = _byteCode.Count;

            Namespaces = parseState.NameSpaces;
        }

        /// <summary>
        /// Tries to retrieve a global variable by name.
        /// </summary>
        /// <param name="name">The name of the global variable.</param>
        /// <param name="val">When this method returns, contains the value of the global variable, if found; otherwise, the default value.</param>
        /// <returns>True if the global variable was found; otherwise, false.</returns>
        internal bool TryGetGlobalVariable(string name, out RuntimeValue val) => _globals.TryGetValue(name, out val);

        /// <summary>
        /// Sets a global variable in the VM's global scope, converting from a standard C# type.
        /// This is the primary way for a host application to pass data into a Fluence script.
        /// </summary>
        /// <param name="name">The name the variable will have in the script.</param>
        /// <param name="value">The C# object to convert and assign. Supported types are:
        /// null, bool, int, long, float, double, string, and char.
        /// </param>
        /// <exception cref="ArgumentException">Thrown if the value is of an unsupported type.</exception>
        public void SetGlobal(string name, object? value)
        {
            RuntimeValue runtimeValue = value switch
            {
                null => RuntimeValue.Nil,

                int intVal => new RuntimeValue(intVal),
                long longVal => new RuntimeValue(longVal),
                double doubleVal => new RuntimeValue(doubleVal),
                float floatVal => new RuntimeValue(floatVal),

                bool boolVal => new RuntimeValue(boolVal),

                string stringVal => new RuntimeValue(new StringObject(stringVal)),
                char charVal => new RuntimeValue(new CharObject(charVal)),

                // TO DO, lists.

                _ => throw new FluenceRuntimeException(
                    $"Unsupported type '{value.GetType().FullName}' for SetGlobal. " +
                    "Supported types are null, bool, int, long, float, double, string, char.")
            };

            AssignVariable(name, runtimeValue);
        }

        /// <summary>
        /// Runs the loaded bytecode for a specified duration.
        /// The main execution loop of the virtual machine.
        /// </summary>
        /// <param name="duration">The maximum time to run before pausing.</param>
        internal void RunFor(TimeSpan duration)
        {
            if (State == FluenceVMState.Finished || State == FluenceVMState.Error) return;

            _stopRequested = false;
            State = FluenceVMState.Running;
            var stopwatch = Stopwatch.StartNew();

            while (_ip < _byteCode.Count)
            {
                if (_stopRequested || stopwatch.Elapsed >= duration)
                {
                    State = FluenceVMState.Paused;
                    return;
                }

                InstructionLine instruction = _byteCode[_ip];
                _ip++;

#if DEBUG
                _stopwatch.Restart();
#endif

                if (instruction.Instruction is InstructionCode.Goto)
                {
                    _ip = (int)((NumberValue)instruction.Lhs).Value;
                    continue;
                }

                if (instruction.SpecializedHandler != null)
                {
                    instruction.SpecializedHandler(instruction, this);
                }
                else
                {
                    _dispatchTable[(int)instruction.Instruction](instruction);
                }
#if DEBUG
                _stopwatch.Stop();
                _instructionCounts.TryAdd(instruction.Instruction, 0);
                _instructionCounts[instruction.Instruction]++;
                _instructionTimings.TryAdd(instruction.Instruction, 0);
                _instructionTimings[instruction.Instruction] += _stopwatch.ElapsedTicks;
#endif
            }

            // If the loop finishes naturally, the script is done.
            State = FluenceVMState.Finished;
        }

        /// <summary>
        /// Signals the VM to stop execution at the next available opportunity.
        /// </summary>
        internal void Stop()
        {
            _stopRequested = true;
        }

        /// <summary>
        /// Directly sets the instruction pointer. Used for debugging or advanced control.
        /// </summary>
        /// <param name="id">The address of the next instruction to execute.</param>
        internal void SetInstructionPointer(int id) => _ip = id;

        /// <summary>Handles the ADD instruction, which performs numeric addition, string concatenation, or list concatenation.</summary>
        private void ExecuteAdd(InstructionLine instruction)
        {
            if (instruction.SpecializedHandler != null)
            {
                instruction.SpecializedHandler(instruction, this);
                return;
            }

            ExecuteGenericAdd(instruction);
        }

        /// <summary>
        /// The generic, "slow path" handler for the ADD instruction. It performs the full operation
        /// and attempts to create and cache a specialized handler for future executions.
        /// </summary>
        internal void ExecuteGenericAdd(InstructionLine instruction)
        {
            RuntimeValue left = GetRuntimeValue(instruction.Rhs);
            RuntimeValue right = GetRuntimeValue(instruction.Rhs2);

            if (left.Type == RuntimeValueType.Number && right.Type == RuntimeValueType.Number)
            {
                var handler = InlineCacheManager.CreateSpecializedAddHandler(instruction, this, left, right);
                if (handler != null)
                {
                    instruction.SpecializedHandler = handler;
                    handler(instruction, this);
                    return;
                }
            }

            // String concatenation.
            if ((left.Type == RuntimeValueType.Object && left.Is<StringObject>()) ||
                (right.Type == RuntimeValueType.Object && right.Is<StringObject>()))
            {
                string resultString = string.Concat(left.ToString(), right.ToString());

                SetVariableOrRegister(instruction.Lhs, new RuntimeValue(new StringObject(resultString)));
                return;
            }

            if (left.Type == RuntimeValueType.Object && left.ObjectReference is ListObject leftList &&
                right.Type == RuntimeValueType.Object && right.ObjectReference is ListObject rightList)
            {
                var concatenatedList = new ListObject();

                concatenatedList.Elements.AddRange(leftList.Elements);
                concatenatedList.Elements.AddRange(rightList.Elements);

                SetVariableOrRegister(instruction.Lhs, new RuntimeValue(concatenatedList));
                return;
            }

            ConstructAndThrowException($"Runtime Error: Cannot apply operator '+' to types {GetDetailedTypeName(left)} and {GetDetailedTypeName(right)}.");
        }

        /// <summary>Handles the SUBTRACT instruction for numeric subtraction or list difference.</summary>
        private void ExecuteSubtraction(InstructionLine instruction)
        {
            if (instruction.SpecializedHandler != null)
            {
                instruction.SpecializedHandler(instruction, this);
                return;
            }

            ExecuteGenericSubtraction(instruction);
        }

        /// <summary>
        /// The generic, "slow path" handler for the SUBTRACT instruction. It performs the full operation
        /// and attempts to create and cache a specialized handler for future executions.
        /// </summary>
        internal void ExecuteGenericSubtraction(InstructionLine instruction)
        {
            RuntimeValue left = GetRuntimeValue(instruction.Rhs);
            RuntimeValue right = GetRuntimeValue(instruction.Rhs2);

            if (left.Type == RuntimeValueType.Number && right.Type == RuntimeValueType.Number)
            {
                var handler = InlineCacheManager.CreateSpecializedSubtractionHandler(instruction, left, right);
                if (handler != null)
                {
                    instruction.SpecializedHandler = handler;
                    handler(instruction, this);
                    return;
                }
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

            ConstructAndThrowException($"Runtime Error: Cannot apply operator '-' to types {GetDetailedTypeName(left)} and {GetDetailedTypeName(right)}.");
        }

        /// <summary>Handles the MULTIPLY instruction for numeric subtraction or list difference.</summary>
        private void ExecuteMultiplication(InstructionLine instruction)
        {
            if (instruction.SpecializedHandler != null)
            {
                instruction.SpecializedHandler(instruction, this);
                return;
            }

            ExecuteGenericMultiplication(instruction);
        }

        /// <summary>
        /// The generic, "slow path" handler for the MULTIPLY instruction. It performs the full operation
        /// and attempts to create and cache a specialized handler for future executions.
        /// </summary>
        internal void ExecuteGenericMultiplication(InstructionLine instruction)
        {
            RuntimeValue left = GetRuntimeValue(instruction.Rhs);
            RuntimeValue right = GetRuntimeValue(instruction.Rhs2);

            if (left.Type == RuntimeValueType.Number && right.Type == RuntimeValueType.Number)
            {
                var handler = InlineCacheManager.CreateSpecializedMulHandler(instruction, left, right);
                if (handler != null)
                {
                    instruction.SpecializedHandler = handler;
                    handler(instruction, this);
                    return;
                }
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

            if (left.ObjectReference is CharObject charLeft && right.Type == RuntimeValueType.Number)
            {
                SetRegister((TempValue)instruction.Lhs, HandleStringRepetition(new StringObject(charLeft.Value.ToString()), right));
                return;
            }
            if (left.Type == RuntimeValueType.Number && right.ObjectReference is CharObject charRight)
            {
                SetRegister((TempValue)instruction.Lhs, HandleStringRepetition(new StringObject(charRight.Value.ToString()), left));
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

            ConstructAndThrowException($"Runtime Error: Cannot apply operator '*' to types {GetDetailedTypeName(left)} and {GetDetailedTypeName(right)}.");
        }

        /// <summary>Handles the DIVIDE instruction for numeric subtraction or list difference.</summary>
        internal void ExecuteDivision(InstructionLine instruction)
        {
            if (instruction.SpecializedHandler != null)
            {
                instruction.SpecializedHandler(instruction, this);
                return;
            }

            RuntimeValue left = GetRuntimeValue(instruction.Rhs);
            RuntimeValue right = GetRuntimeValue(instruction.Rhs2);

            if (left.Type == RuntimeValueType.Number && right.Type == RuntimeValueType.Number)
            {
                var handler = InlineCacheManager.CreateSpecializedDivHandler(instruction, left, right);
                if (handler != null)
                {
                    instruction.SpecializedHandler = handler;
                    handler(instruction, this);
                    return;
                }
            }

            ConstructAndThrowException($"Runtime Error: Cannot apply operator '/' to types {GetDetailedTypeName(left)} and {GetDetailedTypeName(right)}.");
        }

        /// <summary>Handles the MULTIPLY instruction for numeric subtraction or list difference.</summary>
        private void ExecuteModulo(InstructionLine instruction)
        {
            if (instruction.SpecializedHandler != null)
            {
                instruction.SpecializedHandler(instruction, this);
                return;
            }

            RuntimeValue left = GetRuntimeValue(instruction.Rhs);
            RuntimeValue right = GetRuntimeValue(instruction.Rhs2);

            if (left.Type == RuntimeValueType.Number && right.Type == RuntimeValueType.Number)
            {
                var handler = InlineCacheManager.CreateSpecializedModuloHandler(instruction, left, right);
                if (handler != null)
                {
                    instruction.SpecializedHandler = handler;
                    handler(instruction, this);
                    return;
                }
            }

            ConstructAndThrowException($"Runtime Error: Cannot apply operator '%' to types {GetDetailedTypeName(left)} and {GetDetailedTypeName(right)}.");
        }

        /// <summary>Handles the POWER instruction.</summary>
        private void ExecutePower(InstructionLine instruction)
        {
            if (instruction.SpecializedHandler != null)
            {
                instruction.SpecializedHandler(instruction, this);
                return;
            }

            RuntimeValue left = GetRuntimeValue(instruction.Rhs);
            RuntimeValue right = GetRuntimeValue(instruction.Rhs2);

            if (left.Type == RuntimeValueType.Number && right.Type == RuntimeValueType.Number)
            {
                var handler = InlineCacheManager.CreateSpecializedPowerHandler(instruction, left, right);
                if (handler != null)
                {
                    instruction.SpecializedHandler = handler;
                    handler(instruction, this);
                    return;
                }
            }

            ConstructAndThrowException($"Runtime Error: Cannot apply operator '**' to types {GetDetailedTypeName(left)} and {GetDetailedTypeName(right)}.");
        }

        /// <summary>Handles the ASSIGN_TWO instruction, which is used for variable assignment of two variables at once.</summary>
        private void ExecuteAssignTwo(InstructionLine instruction)
        {
            AssignTo(instruction.Lhs, instruction.Rhs);
            AssignTo(instruction.Rhs2, instruction.Rhs3);
        }

        /// <summary>Handles the ASSIGN instruction, which is used for variable assignment and range-to-list expansion.</summary>
        private void ExecuteAssign(InstructionLine instruction)
        {
            AssignTo(instruction.Lhs, instruction.Rhs);
        }

        private void AssignTo(Value left, Value right)
        {
            RuntimeValue sourceValue = GetRuntimeValue(right);
            if (left is VariableValue destVar && sourceValue.ObjectReference is RangeObject range)
            {
                ListObject list = new ListObject();
                RuntimeValue startValue = range.Start;
                RuntimeValue endValue = range.End;

                if (startValue.Type != RuntimeValueType.Number || endValue.Type != RuntimeValueType.Number)
                {
                    ConstructAndThrowException($"Runtime Error: Range bounds must be numbers, not {GetDetailedTypeName(range.Start)} and {GetDetailedTypeName(range.End)}.");
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
            else if (left is VariableValue destVar2)
            {
                AssignVariable(destVar2.Name, sourceValue, destVar2.IsReadOnly);
            }
            else if (left is TempValue destTemp) SetRegister(destTemp, sourceValue);
            else ConstructAndThrowException("Internal VM Error: Destination of 'Assign' must be a variable or temporary.");
        }

        /// <summary>
        /// A generic handler for all binary numeric operations. It correctly handles type promotion
        ///  and dispatches to the appropriate C# operator.
        /// </summary>
        internal void ExecuteNumericBinaryOperation(
            InstructionLine instruction, RuntimeValue left, RuntimeValue right,
            Func<int, int, RuntimeValue> intOp,
            Func<long, long, RuntimeValue> longOp,
            Func<float, float, RuntimeValue> floatOp,
            Func<double, double, RuntimeValue> doubleOp)
        {
            if (left.Type != RuntimeValueType.Number || right.Type != RuntimeValueType.Number)
            {
                ConstructAndThrowException($"Internal VM Error: ExecuteNumericBinaryOperation called with non-numeric types.");
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

            SetVariableOrRegister(instruction.Lhs, result);
        }

        /// <summary>
        /// A unified handler for all bitwise operations (~, &amp;, |, ^, &lt;&lt;, &gt;&gt;).
        /// </summary>
        private void ExecuteBitwiseOperation(InstructionLine instruction)
        {
            RuntimeValue leftValue = GetRuntimeValue(instruction.Rhs);
            long leftLong = ToLong(leftValue);

            if (instruction.Instruction == InstructionCode.BitwiseNot)
            {
                SetRegister((TempValue)instruction.Lhs, new RuntimeValue(~leftLong));
                return;
            }

            RuntimeValue rightValue = GetRuntimeValue(instruction.Rhs2);
            long result = instruction.Instruction switch
            {
                InstructionCode.BitwiseAnd => leftLong & ToLong(rightValue),
                InstructionCode.BitwiseOr => leftLong | ToLong(rightValue),
                InstructionCode.BitwiseXor => leftLong ^ ToLong(rightValue),
                InstructionCode.BitwiseLShift => leftLong << ToInt(rightValue),
                InstructionCode.BitwiseRShift => leftLong >> ToInt(rightValue),
                _ => throw new FluenceRuntimeException("Internal VM Error: Unhandled bitwise operation routed to ExecuteBitwiseOperation."),
            };

            if (instruction.Lhs is VariableValue var)
            {
                AssignVariable(var.Name, new RuntimeValue(result), var.IsReadOnly);
                return;
            }

            SetRegister((TempValue)instruction.Lhs, new RuntimeValue(result));
        }

        /// <summary>
        /// Handles the NEGATE instruction (unary minus).
        /// </summary>
        private void ExecuteNegate(InstructionLine instruction)
        {
            TempValue destination = (TempValue)instruction.Lhs;
            RuntimeValue value = GetRuntimeValue(instruction.Rhs);

            if (value.Type != RuntimeValueType.Number)
            {
                ConstructAndThrowException($"Runtime Error: The unary minus operator '-' cannot be applied to a value of type '{GetDetailedTypeName(value)}'.");
                return;
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
                ConstructAndThrowException("Internal VM Error: The target of a jump instruction must be a NumberValue.");
                return;
            }

            if (condition.IsTruthy == requiredCondition)
            {
                _ip = (int)target.Value;
            }
        }

        internal void ExecuteBranchIfEqual(InstructionLine instruction, bool target)
        {
            if (instruction.SpecializedHandler != null)
            {
                instruction.SpecializedHandler(instruction, this);
                return;
            }

            if (instruction.Lhs is not NumberValue jmp)
            {
                ConstructAndThrowException("Internal VM Error: The target of a jump instruction must be a NumberValue.");
                return;
            }

            RuntimeValue left = GetRuntimeValue(instruction.Rhs);
            RuntimeValue right = GetRuntimeValue(instruction.Rhs2);

            instruction.SpecializedHandler = InlineCacheManager.CreateSpecializedBranchHandler(instruction, left, right, target);
            bool result = left.Equals(right);

            if (result == target)
            {
                _ip = (int)jmp.Value;
            }
        }

        /// <summary>
        /// Handles the GOTO instruction for unconditional jumps.
        /// </summary>
        private void ExecuteGoto(InstructionLine instruction)
        {
            if (instruction.Lhs is not NumberValue target)
            {
                ConstructAndThrowException("Internal VM Error: The target of a GOTO instruction must be a NumberValue.");
                return;
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
            TempValue destination = (TempValue)instruction.Lhs;
            RuntimeValue left = GetRuntimeValue(instruction.Rhs);
            RuntimeValue right = GetRuntimeValue(instruction.Rhs2);

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
                ConstructAndThrowException($"Internal VM Error: Cannot perform numeric comparison on non-number types: ({left.Type}, {right.Type}).");
            }

            bool result = left.NumberType switch
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
                ConstructAndThrowException("Internal VM Error: NewRange opcode requires a non-null RangeValue operand.");
                return;
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
                            ConstructAndThrowException($"Runtime Error: Cannot get length of a range with non-numeric bounds ({GetDetailedTypeName(range.Start)}, {GetDetailedTypeName(range.End)}).");
                            return;
                        }
                        int start = Convert.ToInt32(range.Start.IntValue);
                        int end = Convert.ToInt32(range.End.IntValue);
                        length = (end < start) ? 0 : (end - start + 1);
                        break;
                    default:
                        ConstructAndThrowException($"Runtime Error: Cannot get the length of a value of type '{GetDetailedTypeName(collection)}'.");
                        return;
                }
            }
            else
            {
                ConstructAndThrowException($"Cannot get length of a non-object type '{collection.Type}'.");
                return;
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
                ConstructAndThrowException($"Runtime Error: Cannot push an element to a non-list value (got type '{GetDetailedTypeName(listVal)}').");
                return;
            }

            RuntimeValue elementToAdd = GetRuntimeValue(instruction.Rhs);

            if (elementToAdd.ObjectReference is RangeObject range)
            {
                RuntimeValue startValue = range.Start;
                RuntimeValue endValue = range.End;

                if (startValue.Type != RuntimeValueType.Number || endValue.Type != RuntimeValueType.Number)
                {
                    ConstructAndThrowException($"Runtime Error: Range bounds must be numbers, not {GetDetailedTypeName(range.Start)} and {GetDetailedTypeName(range.End)}.");
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
                ConstructAndThrowException("Internal VM Error: NewInstance requires a StructSymbol as its operand.");
                return;
            }

            InstanceObject instance = new InstanceObject(classSymbol);
            SetRegister((TempValue)instruction.Lhs, new RuntimeValue(instance));
        }

        /// <summary>
        /// Handles the GET_FIELD instruction, which retrieves the value of a field or method from a struct instance.
        /// </summary>
        private void ExecuteGetField(InstructionLine instruction)
        {
            if (instruction.Rhs2 is not StringValue fieldName)
            {
                ConstructAndThrowException("Internal VM Error: GetField requires a string literal for the field name.");
                return;
            }

            RuntimeValue instanceValue = GetRuntimeValue(instruction.Rhs);
            if (instanceValue.ObjectReference is not InstanceObject instance)
            {
                ConstructAndThrowException($"Runtime Error: Cannot access property '{fieldName.Value}' on a non-instance value (got type '{GetDetailedTypeName(instanceValue)}').");
                return;
            }

            SetRegister((TempValue)instruction.Lhs, instance.GetField(fieldName.Value));
        }

        /// <summary>
        /// Handles the GET_STATIC instruction, retrieving a static field's value from a struct symbol.
        /// </summary>
        private void ExecuteGetStatic(InstructionLine instruction)
        {
            if (instruction.Rhs is not StructSymbol structSymbol ||
                instruction.Rhs2 is not StringValue fieldName)
            {
                ConstructAndThrowException("Internal VM Error: Invalid operands for GET_STATIC. Expected StructSymbol and StringValue.");
                return;
            }

            if (structSymbol.StaticFields.TryGetValue(fieldName.Value, out var value))
            {
                SetRegister((TempValue)instruction.Lhs, value);
                return;
            }

            ConstructAndThrowException($"Internal VM Erorr: Attempt to retrieve a non-existant static struct field: {structSymbol}__Field:{fieldName.Value}.");
        }

        /// <summary>
        /// Handles the SET_STATIC instruction, assigning a value to a struct's static field.
        /// </summary>
        private void ExecuteSetStatic(InstructionLine instruction)
        {
            if (instruction.Lhs is not StructSymbol structSymbol ||
                instruction.Rhs is not StringValue fieldName)
            {
                ConstructAndThrowException("Internal VM Error: Invalid operands for SET_STATIC. Expected StructSymbol and StringValue.");
                return;
            }

            RuntimeValue valueToSet = GetRuntimeValue(instruction.Rhs2);
            structSymbol.StaticFields[fieldName.Value] = valueToSet;
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
                ConstructAndThrowException("Internal VM Error: SetField requires a string literal for the field name.");
                return;
            }

            var instanceValue = GetRuntimeValue(instruction.Lhs);
            if (instanceValue.ObjectReference is not InstanceObject instance)
            {
                ConstructAndThrowException($"Runtime Error: Cannot set property '{fieldName.Value}' on a non-instance value (got type '{GetDetailedTypeName(instanceValue)}').");
                return;
            }

            if (instance.Class.StaticFields.ContainsKey(fieldName.Value))
            {
                ConstructAndThrowException($"Runtime Error: Cannot set solid ( static ) property '{fieldName.Value}' of a struct.");
                return;
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

            switch (collection.ObjectReference)
            {
                case ListObject list:
                    {
                        if (indexVal.Type != RuntimeValueType.Number)
                        {
                            ConstructAndThrowException($"Runtime Error: List index must be a number, not '{GetDetailedTypeName(indexVal)}'.");
                        }
                        int index = indexVal.IntValue;
                        if (index < 0 || index >= list.Elements.Count)
                        {
                            ConstructAndThrowException($"Runtime Error: Index out of range. Index was {index}, but list size is {list.Elements.Count}.");
                        }
                        SetRegister((TempValue)instruction.Lhs, list.Elements[index]);
                        break;
                    }
                case StringObject str:
                    {
                        if (indexVal.Type != RuntimeValueType.Number)
                        {
                            ConstructAndThrowException($"Runtime Error: String index must be a number, not '{GetDetailedTypeName(indexVal)}'.");
                        }
                        int index = indexVal.IntValue;
                        if (index < 0 || index >= str.Value.Length)
                        {
                            ConstructAndThrowException($"Runtime Error: Index out of range. Index was {index}, but string length is {str.Value.Length}.");
                        }

                        char charAsString = str.Value[index];
                        CharObject resultStringObject = new CharObject(charAsString);
                        SetRegister((TempValue)instruction.Lhs, new RuntimeValue(resultStringObject));
                        break;
                    }
                case StringValue str2:
                    {
                        if (indexVal.Type != RuntimeValueType.Number)
                        {
                            ConstructAndThrowException($"Runtime Error: String index must be a number, not '{GetDetailedTypeName(indexVal)}'.");
                        }
                        int index = indexVal.IntValue;
                        if (index < 0 || index >= str2.Value.Length)
                        {
                            ConstructAndThrowException($"Runtime Error: Index out of range. Index was {index}, but string length is {str2.Value.Length}.");
                        }

                        char charAsString = str2.Value[index];
                        CharObject resultStringObject = new CharObject(charAsString);
                        SetRegister((TempValue)instruction.Lhs, new RuntimeValue(resultStringObject));
                        break;
                    }
                // Not an indexable type.
                default:
                    ConstructAndThrowException($"Runtime Error: Cannot apply index operator [...] to a non-indexable type '{GetDetailedTypeName(collection)}'.");
                    return;
            }
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
                ConstructAndThrowException($"Internal VM Error: Cannot apply index operator [...] to a non-list value (got type '{GetDetailedTypeName(collection)}').");
                return;
            }

            if (indexVal.Type != RuntimeValueType.Number)
            {
                ConstructAndThrowException($"Runtime Error: List index must be a number, not '{GetDetailedTypeName(indexVal)}'.");
            }

            int index = indexVal.IntValue;

            if (index < 0 || index >= list.Elements.Count)
            {
                ConstructAndThrowException($"Runtime Error: Index out of range. Index was {index}, but list size is {list.Elements.Count}.");
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
                IteratorObject iterator = new IteratorObject(iterable.ObjectReference);
                SetRegister((TempValue)instruction.Lhs, new RuntimeValue(iterator));
                return;
            }

            ConstructAndThrowException($"Runtime Error: Cannot create an iterator from a non-iterable type '{GetDetailedTypeName(iterable)}'.");
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
                ConstructAndThrowException("Internal VM Error: Attempted to iterate over a non-iterator value.");
                return;
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

            SetRegister(valueReg, nextValue);
            SetRegister(continueFlagReg, new RuntimeValue(continueLoop));
        }

        /// <summary>
        /// Handles the PUSH_PARAM instruction, which pushes a value onto the operand stack in preparation for a function call.
        /// </summary>
        private void ExecutePushParam(InstructionLine instruction)
        {
            RuntimeValue valueToPush = GetRuntimeValue(instruction.Lhs);
            _operandStack.Push(valueToPush);
        }

        /// <summary>
        /// Handles the CALL_FUNCTION instruction, which invokes a standalone function.
        /// </summary>
        private void ExecuteCallFunction(InstructionLine instruction)
        {
            RuntimeValue functionVal = GetRuntimeValue(instruction.Rhs);
            if (functionVal.As<FunctionObject>() is not FunctionObject function)
            {
                ConstructAndThrowException($"Internal VM Error: Attempted to call a value that is not a function (got type '{GetDetailedTypeName(functionVal)}').");
                return;
            }

            int argCount = GetRuntimeValue(instruction.Rhs2).IntValue;

            // Intrinsic functions that have arity of -100 accept a dynamic amount of arguments.
            if (function.Arity != argCount && function.Arity != -100)
            {
                ConstructAndThrowException($"Internal VM Error: Mismatched arguments for function '{function.Name}'. Expected {function.Arity}, but got {argCount}.");
            }

            if (function.IsIntrinsic)
            {
                List<Value> args = new List<Value>(argCount);
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

            CallFrame newFrame = new CallFrame(function, _ip, (TempValue)instruction.Lhs);

            for (int i = function.Parameters.Count - 1; i >= 0; i--)
            {
                string paramName = function.Parameters[i];
                RuntimeValue argValue = _operandStack.Pop();
                ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(newFrame.Registers, paramName, out _);
                valueRef = argValue;
            }

            _callStack.Push(newFrame);
            _cachedRegisters = newFrame.Registers;
            _ip = function.StartAddress;
        }

        /// <summary>
        /// Handles the CALL_METHOD instruction code, which invokes a method on an object instance.
        /// </summary>
        private void ExecuteCallMethod(InstructionLine instruction)
        {
            if (instruction.Rhs2 is not StringValue methodNameVal)
            {
                ConstructAndThrowException("Internal VM Error: Invalid operands for CallMethod. Expected a method name as a string.");
                return;
            }

            string methodName = methodNameVal.Value;
            RuntimeValue instanceVal = GetRuntimeValue(instruction.Rhs);

            // Check if the object is a native type (like List) that exposes fast C# methods.
            if (instanceVal.ObjectReference is IFluenceObject fluenceObject)
            {
                // Ask the object if it has a built-in method with this name.
                if (fluenceObject.TryGetIntrinsicMethod(methodName, out var intrinsicMethod))
                {
                    // We found an intrinsic.
                    List<RuntimeValue> args = new List<RuntimeValue>();
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

            if (instanceVal.ObjectReference is not InstanceObject instance)
            {
                ConstructAndThrowException($"Internal VM Error: Cannot call method '{methodName}' on a non-instance object.");
                return;
            }

            FunctionValue methodBlueprint;

            if (string.Equals(methodName, "init", StringComparison.Ordinal))
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
                    ConstructAndThrowException($"Internal VM Error: Undefined method '{methodName}' on struct '{instance.Class.Name}'.");
                    return;
                }
            }

            // Convert the compile-time FunctionValue into a runtime FunctionObject.
            FunctionObject functionToExecute = new FunctionObject(
                methodBlueprint.Name,
                methodBlueprint.Arity,
                methodBlueprint.Arguments,
                methodBlueprint.StartAddress,
                methodBlueprint.FunctionScope
            );

            int argCountOnStack = _operandStack.Count;
            if (functionToExecute.Arity != argCountOnStack)
            {
                ConstructAndThrowException($"Internal VM Error: Mismatched arity for method '{functionToExecute.Name}'. Expected {functionToExecute.Arity}, but got {argCountOnStack}.");
                return;
            }

            CallFrame newFrame = new CallFrame(functionToExecute, _ip, (TempValue)instruction.Lhs);

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

        private void ExecuteCallStatic(InstructionLine instruction)
        {
            if (instruction.Rhs is not StructSymbol structSymbol ||
                instruction.Rhs2 is not StringValue methodName)
            {
                ConstructAndThrowException("Internal VM Error: Invalid operands for CALL_STATIC.");
                return;
            }

            if (structSymbol.StaticIntrinsics.TryGetValue(methodName.Value, out var intrinsicSymbol))
            {
                int argCount = _operandStack.Count;
                if (intrinsicSymbol.Arity != argCount)
                {
                    ConstructAndThrowException($"Runtime Error: Mismatched arity for static intrinsic struct function '{intrinsicSymbol.Name}'. Expected {intrinsicSymbol.Arity}, but got {argCount}.");
                    return;
                }

                List<Value> args = new List<Value>(argCount);

                for (int i = 0; i < argCount; i++) 
                { 
                    args.Add(ToValue(_operandStack.Pop())); 
                }
                args.Reverse();

                Value resultValue = intrinsicSymbol.IntrinsicBody(args);
                SetRegister((TempValue)instruction.Lhs, GetRuntimeValue(resultValue));
                return;
            }

            if (!structSymbol.Functions.TryGetValue(methodName.Value, out var methodBlueprint))
            {
                ConstructAndThrowException($"Runtime Error: Static function '{methodName.Value}' not found on struct '{structSymbol.Name}'.");
                return;
            }

            FunctionObject functionToExecute = new FunctionObject(
                methodBlueprint.Name,
                methodBlueprint.Arity,
                methodBlueprint.Arguments,
                methodBlueprint.StartAddress,
                methodBlueprint.FunctionScope
            );

            int argCountOnStack = _operandStack.Count;
            if (functionToExecute.Arity != argCountOnStack)
            {
                ConstructAndThrowException($"Runtime Error: Mismatched arity for static function '{functionToExecute.Name}'. Expected {functionToExecute.Arity}, but got {argCountOnStack}.");
                return;
            }

            CallFrame newFrame = new CallFrame(functionToExecute, _ip, (TempValue)instruction.Lhs);

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
                ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(CurrentRegisters, finishedFrame.DestinationRegister.TempName, out _);
                valueRef = returnValue;
            }

            _ip = finishedFrame.ReturnAddress;

            if (instruction.Lhs is TempValue destination)
            {
                ref RuntimeValue valueRef2 = ref CollectionsMarshal.GetValueRefOrAddDefault(CurrentRegisters, destination.TempName, out _);
                valueRef2 = returnValue;
            }
        }

        internal void SetVariableOrRegister(Value target, RuntimeValue value)
        {
            if (target is VariableValue var)
            {
                AssignVariable(var.Name, value, var.IsReadOnly);
                return;
            }

            SetRegister((TempValue)target, value);
        }

        internal void SetVariable(VariableValue var, RuntimeValue val)
        {
            if (CurrentFrame.ReturnAddress != _byteCode.Count)
            {
                ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(CurrentRegisters, var.Name, out _);
                valueRef = val;
            }

            ref RuntimeValue valueRef2 = ref CollectionsMarshal.GetValueRefOrAddDefault(_globals, var.Name, out _);
            valueRef2 = val;
        }

        /// <summary>
        /// Converts a compile-time <see cref="Value"/> from bytecode into a runtime <see cref="RuntimeValue"/>.
        /// This is the bridge between the parser's representation and the VM's execution values.
        /// </summary>
        internal RuntimeValue GetRuntimeValue(Value val)
        {
            if (val is TempValue temp)
            {
                ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(CurrentRegisters, temp.TempName);

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

            if (val is FunctionValue func)
            {
                // A FunctionValue from the bytecode is just a blueprint.
                // We must convert it into a live, runtime FunctionObject.
                if (func.IsIntrinsic)
                {
                    // This handles global intrinsics like 'printl'.
                    return new RuntimeValue(new FunctionObject(func.Name, func.Arity, func.IntrinsicBody, func.FunctionScope));
                }
                else
                {
                    // This handles user-defined functions like 'double' and 'Main'.
                    return new RuntimeValue(new FunctionObject(func.Name, func.Arity, func.Arguments, func.StartAddress, func.FunctionScope));
                }
            }

            return val switch
            {
                CharValue ch => new RuntimeValue(new CharObject(ch.Value)),
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
                _ => throw new FluenceRuntimeException($"Internal VM Error: Unrecognized Value type '{val.GetType().Name}' during conversion.")
            };
        }

        /// <summary>
        /// Resolves a variable name to its runtime value by searching the current scope hierarchy.
        /// </summary>
        /// <param name="name">The name of the variable to resolve.</param>
        /// <returns>The <see cref="RuntimeValue"/> associated with the variable name.</returns>
        /// <exception cref="FluenceRuntimeException">Thrown if the variable is not defined in any accessible scope.</exception>
        private RuntimeValue ResolveVariable(string name)
        {
            ref RuntimeValue localValue = ref CollectionsMarshal.GetValueRefOrNullRef(CurrentRegisters, name);
            if (!Unsafe.IsNullRef(ref localValue))
            {
                return localValue;
            }

            var lexicalScope = CurrentFrame.Function.DefiningScope;
            if (lexicalScope.TryResolve(name, out Symbol symbol))
            {
                return ResolveVariableFromScopeSymbol(symbol, lexicalScope);
            }

            if (CurrentFrame.ReturnAddress == _byteCode.Count)
            {
                foreach (var item in Namespaces.Values)
                {
                    FluenceScope lexicalScope2 = item;

                    if (lexicalScope2.TryResolve(name, out Symbol symb))
                    {
                        return ResolveVariableFromScopeSymbol(symb, lexicalScope2);
                    }
                }
            }

            // Last case, check in global scope.
            foreach (Symbol valSymbol in _globalScope.Symbols.Values)
            {
                if (valSymbol is VariableSymbol varSymbol && string.Equals(varSymbol.Name, name, StringComparison.Ordinal))
                {
                    return GetRuntimeValue(varSymbol.Value);
                }
            }

            ConstructAndThrowException($"Runtime Error: Undefined variable '{name}'.");
            return new();
        }

        /// <summary>
        /// Resolves complex symbols into RuntimeValues.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        private RuntimeValue ResolveVariableFromScopeSymbol(Symbol symbol, FluenceScope scope)
        {
            if (symbol is FunctionSymbol funcSymbol2)
            {
                return new RuntimeValue(
                    funcSymbol2.IsIntrinsic
                        ? new FunctionObject(funcSymbol2.Name, funcSymbol2.Arity, funcSymbol2.IntrinsicBody, null!)
                        : new FunctionObject(funcSymbol2.Name, funcSymbol2.Arity, funcSymbol2.Arguments, funcSymbol2.StartAddress, funcSymbol2.DefiningScope
                        )
                    );
            }
            else if (symbol is VariableSymbol variable)
            {
                // Current scopt also contains all symbols from namespaces it uses.
                ref var namespaceRuntimeValue = ref CollectionsMarshal.GetValueRefOrNullRef(scope.RuntimeStorage, variable.Name);
                if (!Unsafe.IsNullRef(ref namespaceRuntimeValue))
                {
                    return namespaceRuntimeValue;
                }
                ref var namespaceRuntimeValue2 = ref CollectionsMarshal.GetValueRefOrNullRef(_globals, variable.Name);
                if (!Unsafe.IsNullRef(ref namespaceRuntimeValue2))
                {
                    return namespaceRuntimeValue2;
                }

                foreach (var item in Namespaces.Values)
                {
                    if (item.TryResolve(variable.Name, out Symbol symb))
                    {
                        return GetRuntimeValue(((VariableSymbol)symb).Value);
                    }
                }
            }

            // Won't reach here, but satisfies compiler.
            return new();
        }

        /// <summary>
        /// Writes a value to a specified temporary register in the current call frame.
        /// </summary>
        internal void SetRegister(TempValue destination, RuntimeValue value)
        {
            ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(CurrentRegisters, destination.TempName, out _);
            valueRef = value;
        }

        /// <summary>
        /// Assigns a value to a variable.
        /// </summary>
        private void AssignVariable(string name, RuntimeValue value, bool readOnly = false)
        {
            var cache = CurrentFrame.WritableCache;
            ref bool isReadonlyRef = ref CollectionsMarshal.GetValueRefOrNullRef(cache, name);
            ref bool isReadOnlyGlobalRef = ref CollectionsMarshal.GetValueRefOrNullRef(GlobalWritableCache, name);

            if (!Unsafe.IsNullRef(ref isReadonlyRef))
            {
                if (isReadonlyRef)
                {
                    ConstructAndThrowException($"Runtime Error: Cannot assign to the readonly variable '{name}'.");
                    return;
                }
            }
            else if (!Unsafe.IsNullRef(ref isReadOnlyGlobalRef))
            {
                if (isReadOnlyGlobalRef)
                {
                    ConstructAndThrowException($"Runtime Error: Cannot assign to the readonly global variable '{name}'.");
                    return;
                }
            }
            else
            {
                if (readOnly)
                {
                    cache[name] = true;
                    if (CurrentFrame.ReturnAddress == _byteCode.Count)
                    {
                        GlobalWritableCache[name] = true;
                    }
                }
                else if (CurrentFrame.Function.DefiningScope.TryResolve(name, out Symbol symbol) &&
                    symbol is VariableSymbol { IsReadonly: true })
                {
                    cache[name] = true;
                    ConstructAndThrowException($"Runtime Error: Cannot assign to the readonly or solid variable '{name}'.");
                    return;
                }
                else if (_globalScope.TryResolve(name, out symbol) && symbol is VariableSymbol symb && symb.IsReadonly)
                {
                    GlobalWritableCache[name] = true;
                    ConstructAndThrowException($"Runtime Error: Cannot assign to the readonly or solid variable '{name}'.");
                    return;
                }
                else
                {
                    if (CurrentFrame.ReturnAddress == _byteCode.Count)
                    {
                        GlobalWritableCache[name] = false;
                    }
                    cache[name] = false;
                }
            }

            // If we are not in the top-level script, assign to the current frame's local registers.
            if (CurrentFrame.ReturnAddress != _byteCode.Count)
            {
                ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(CurrentRegisters, name, out _);
                valueRef = value;
            }

            ref RuntimeValue valueRef2 = ref CollectionsMarshal.GetValueRefOrAddDefault(_globals, name, out _);
            valueRef2 = value;
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
            ListObject repeatedList = new ListObject();

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

            StringBuilder sb = new StringBuilder(str.Value.Length * count);
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
                RuntimeValueType.Object when rtValue.ObjectReference is ListObject list => new ListValue([.. list.Elements.Select(ToValue)]),
                RuntimeValueType.Object when rtValue.ObjectReference is InstanceObject instance => new StructValue(instance.Class, instance._fields),
                RuntimeValueType.Object when rtValue.ObjectReference is CharObject ch => new CharValue(ch.Value),
                RuntimeValueType.Object when rtValue.ObjectReference is Value val => val,
                _ => (Value)ConstructAndThrowException($"Internal VM Error: Cannot convert runtime object '{rtValue}' back to a bytecode value.")
            };
        }

        /// <summary>
        /// Creates and logs to the console a <see cref="VMDebugContext"/> with the current state of the virtual machine before throwing an exception.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        /// <exception cref="FluenceRuntimeException"></exception>
        internal object ConstructAndThrowException(string exception)
        {
            VMDebugContext ctx = new VMDebugContext(this);
            _outputLine(ctx.DumpContext());
            throw new FluenceRuntimeException(exception);
        }
    }
}