using Fluence.Exceptions;
using Fluence.Global;
using Fluence.RuntimeTypes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        internal delegate RuntimeValue IntrinsicRuntimeMethod(FluenceVirtualMachine vm, RuntimeValue self);

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
        private Dictionary<int, RuntimeValue> _cachedRegisters;

        /// <summary>A dictionary holding all global variables.</summary>
        private readonly Dictionary<int, RuntimeValue> _globals;

        /// <summary>The top-level global scope, used for resolving global functions and variables.</summary>
        private readonly FluenceScope _globalScope;

        /// <summary>The stack used for passing arguments to functions and for temporary operand storage.</summary>
        private readonly Stack<RuntimeValue> _operandStack = new Stack<RuntimeValue>();

        private readonly Stack<TryCatchValue> _tryCatchBlocks = new Stack<TryCatchValue>();

        /// <summary>
        /// A collection of the standard library names that are permitted to be loaded by a script.
        /// If this set is empty, all standard libraries are allowed. If it is populated, only the
        /// libraries whose names are in this set can be imported via the 'use' statement.
        /// This acts as a security whitelist for sandboxing script execution.
        /// </summary>
        private HashSet<string> _allowedIntrinsicLibraries = new HashSet<string>();

        /// <summary>
        /// A collection of the standard library names that are not permitted to be loaded by the script.
        /// libraries whose names are in this set can not be imported via the 'use' statement.
        /// This acts as a security blacklist for sandboxing script execution.
        /// </summary>
        private HashSet<string> _disallowedIntrinsicLibraries = new HashSet<string>();

        /// <summary> A pool of CallFrame objects to reuse. </summary>
        private readonly ObjectPool<CallFrame> _callFramePool;

        /// <summary> A pool of IteratorObject-s to reuse. </summary>
        private readonly ObjectPool<IteratorObject> _iteratorObjectPool;

        /// <summary> A pool of CharObject-s objects to reuse. </summary>
        private readonly ObjectPool<CharObject> _charObjectPool;

        /// <summary> A pool of FunctionObject-s objects to reuse. </summary>
        private readonly ObjectPool<FunctionObject> _functionObjectPool;

        /// <summary> A pool of StringObject-s objects to reuse. </summary>
        private readonly ObjectPool<StringObject> _stringObjectPool;

        /// <summary>  A pool of RangeObject-s objects to reuse. </summary>
        private readonly ObjectPool<RangeObject> _rangeObjectPool;

        private readonly FluenceParser _parser;

        /// <summary>
        /// The interval representing the amount of instructions per which executed we check the
        /// elapsed time since the start of the Virtual Machine.
        /// </summary>
        // TO DO, the value of this needs to be tested, while the VM is very fast, the higher the value the less
        // accurate the elapsed time check is. But the elapsed check is very expensive, so a golden middle needs
        // to be identified.
        private const int _timeCheckInterval = 100000;

        /// <summary>
        /// A cache to store the readonly status of variables in the global scope.
        /// </summary>
        internal readonly Dictionary<int, bool> GlobalWritableCache = new();

        /// <summary> The list of all namespaces in the source code. </summary>
        private readonly Dictionary<string, FluenceScope> Namespaces;

        /// <summary>The Instruction Pointer, which holds the address of the *next* instruction to be executed.</summary>
        private int _ip;

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

        /// <summary>
        /// The current instance of the <see cref="VirtualMachineConfiguration"/> given by the interpreter.
        /// </summary>
        private readonly VirtualMachineConfiguration _configuration;

        /// <summary>Gets the currently active call frame from the top of the call stack.</summary>
        internal CallFrame CurrentFrame => _callStack.Peek();

        /// <summary>Gets the registers for the current call frame via the cached reference.</summary>
        internal Dictionary<int, RuntimeValue> CurrentRegisters => _cachedRegisters;

        internal Dictionary<int, RuntimeValue> GlobalRegisters => _globals;

        internal int CurrentInstructionPointer => _ip;

        internal List<InstructionLine> ByteCode => _byteCode;

        internal FluenceParser Parser => _parser;

        internal FluenceScope GlobalScope => _globalScope;

        /// <summary>
        /// The current state of the Virtual Machine.
        /// </summary>
        public FluenceVMState State { get; private set; } = FluenceVMState.NotStarted;

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
        /// total time spent, and average time per instruction.
        /// </summary>
        internal void DumpPerformanceProfile()
        {
            _outputLine("\n--- FLUENCE VM EXECUTION PROFILE ---");

            if (_instructionCounts.Count == 0)
            {
                _outputLine("No instructions were executed or profiling was not enabled.");
                return;
            }

            long totalInstructions = 0;
            long totalTicks = 0;
            List<(InstructionCode OpCode, long Count, long Ticks)> profileData = new List<(InstructionCode OpCode, long Count, long Ticks)>();

            foreach (KeyValuePair<InstructionCode, long> kvp in _instructionCounts)
            {
                long ticks = _instructionTimings.GetValueOrDefault(kvp.Key, 0);
                profileData.Add((kvp.Key, kvp.Value, ticks));
                totalInstructions += kvp.Value;
                totalTicks += ticks;
            }

            _outputLine($"Total Instructions Executed: {totalInstructions:N0}");
            _outputLine($"Total Execution Time: {new TimeSpan(totalTicks).TotalMilliseconds:N3} ms\n");

            _outputLine($"{"OpCode",-20} | {"Count",-15} | {"% of Total",-12} | {"Total Time (ms)",-18} | {"% of Time",-12} | {"Avg. Ticks/Op",-15}");
            _outputLine(new string('-', 110));

            profileData.Sort((a, b) => b.Ticks.CompareTo(a.Ticks));

            foreach ((InstructionCode OpCode, long Count, long Ticks) in profileData)
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
            _outputLine(new string('-', 110));
            _outputLine(Environment.NewLine);
        }
#endif

        internal sealed class ObjectPool<T> where T : class, new()
        {
            private readonly Stack<T> _pool = new Stack<T>();
            private readonly Action<T> _resetAction;

            internal ObjectPool(Action<T> resetAction = null!, int initialCapacity = 16)
            {
                _resetAction = resetAction;
                for (int i = 0; i < initialCapacity; i++)
                {
                    _pool.Push(new T());
                }
            }

            /// <summary>
            /// Gets an object from the pool. If the pool is empty, a new object is created.
            /// </summary>
            internal T Get()
            {
                if (_pool.TryPop(out T? item))
                {
                    return item;
                }
                return new T();
            }

            /// <summary>
            /// Returns an object to the pool for reuse.
            /// </summary>
            internal void Return(T item)
            {
                _resetAction.Invoke(item);
                _pool.Push(item);
            }
        }

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

            internal VMDebugContext(FluenceVirtualMachine vm)
            {
                InstructionPointer = vm._ip > 0 ? vm._ip - 1 : 0;
                CurrentInstruction = vm._byteCode[InstructionPointer];

                CurrentLocals = new Dictionary<int, RuntimeValue>(vm.CurrentRegisters);

                OperandStackSnapshot = [.. vm._operandStack];
                CallStackDepth = vm._callStack.Count;
                CurrentFunctionName = vm.CurrentFrame.Function.Name;
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
        internal FluenceVirtualMachine(List<InstructionLine> bytecode, VirtualMachineConfiguration config, ParseState parseState, TextOutputMethod? output, TextOutputMethod? outputLine, TextInputMethod? input)
        {
            _callFramePool = new ObjectPool<CallFrame>(frame => frame.Reset());
            _iteratorObjectPool = new ObjectPool<IteratorObject>(iter => iter.Reset());
            _charObjectPool = new ObjectPool<CharObject>(chr => chr.Reset());
            _functionObjectPool = new ObjectPool<FunctionObject>(func => func.Reset());
            _stringObjectPool = new ObjectPool<StringObject>(str => str.Reset());
            _rangeObjectPool = new ObjectPool<RangeObject>(range => range.Reset());
            _parser = parseState.ParserInstance;
            _byteCode = bytecode;
            _globalScope = parseState.GlobalScope;
            _globals = new Dictionary<int, RuntimeValue>();
            _callStack = new Stack<CallFrame>();

            _output = output ?? Console.Write;
            _outputLine = outputLine ?? Console.WriteLine;

            _configuration = config;

            // TO DO, Needs testing.
            _input = input ?? Console.ReadLine!;

            // This represents the top-level global execution context.
            FunctionObject mainScriptFunc = new FunctionObject("<script>", 0, new List<string>(), 0, _globalScope);

            CallFrame initialFrame = new CallFrame();
            initialFrame.Initialize(mainScriptFunc, _byteCode.Count, null!);

            _callStack.Push(initialFrame);

            _cachedRegisters = initialFrame.Registers;

            InstructionCode[] opCodeValues = Enum.GetValues<InstructionCode>();
            int maxOpCode = 0;

            foreach (InstructionCode value in opCodeValues)
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
            // Inlined directly in Run function.
            // _dispatchTable[(int)InstructionCode.Goto] = ExecuteGoto;
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

            _dispatchTable[(int)InstructionCode.NewLambda] = ExecuteNewLambda;
            _dispatchTable[(int)InstructionCode.IncrementIntUnrestricted] = ExecuteIncrementIntUnrestricted;
            _dispatchTable[(int)InstructionCode.LoadAddress] = ExecuteLoadAddress;

            _dispatchTable[(int)InstructionCode.GetType] = ExecuteGetType;

            _dispatchTable[(int)InstructionCode.TryBlock] = ExecuteTryBlock;
            _dispatchTable[(int)InstructionCode.CatchBlock] = ExecuteCatchBlock;

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

            _dispatchTable[(int)InstructionCode.PushTwoParams] = ExecutePushTwoParam;
            _dispatchTable[(int)InstructionCode.PushThreeParams] = ExecutePushThreeParam;
            _dispatchTable[(int)InstructionCode.PushFourParams] = ExecutePushFourParam;

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
        internal bool TryGetGlobalVariable(string name, out RuntimeValue val) => _globals.TryGetValue(name.GetHashCode(), out val);

        /// <summary>
        /// Returns a reusable CallFrame insitance from the CallFrame pool or creates one if non are available.
        /// </summary>
        /// <returns></returns>
        internal CallFrame GetCallframe() => _callFramePool.Get();

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

                string stringVal => ResolveStringObjectRuntimeValue(stringVal),
                char charVal => ResolveCharObjectRuntimeValue(charVal),

                // TO DO, lists.

                _ => SignalError<RuntimeValue>(
                    $"Unsupported type '{value.GetType().FullName}' for SetGlobal. " +
                    "Supported types are null, bool, int, long, float, double, string, char.")
            };

            AssignVariable(name, name.GetHashCode(), runtimeValue);
        }

        /// <summary>
        /// Sets the whitelist and the blacklist of standard libraries.
        /// </summary>
        internal void SetIntrinsicLibraryWhiteAndBlackLists(HashSet<string> whiteList, HashSet<string> blackList)
        {
            _allowedIntrinsicLibraries = whiteList;
            _disallowedIntrinsicLibraries = blackList;
        }

        /// <summary>
        /// Runs the loaded bytecode for a specified duration.
        /// The main execution loop of the virtual machine.
        /// </summary>
        /// <param name="duration">The maximum time to run before pausing.</param>
        internal void RunFor(TimeSpan duration)
        {
            if (State is FluenceVMState.Finished or FluenceVMState.Error) return;

            _stopRequested = false;
            State = FluenceVMState.Running;
            Stopwatch stopwatch = Stopwatch.StartNew();
            int instructionsUntilNextCheck = _timeCheckInterval;
            bool willRunUntilDone = duration == TimeSpan.MaxValue;

            if (willRunUntilDone)
            {
                stopwatch.Stop();
            }

            while (_ip < _byteCode.Count)
            {
                if (_stopRequested)
                {
                    State = FluenceVMState.Paused;
                    return;
                }

                // If the VM is set to run until completion, we can save a lot of time on just not doing time elapsed checks.
                if (!willRunUntilDone)
                {
                    instructionsUntilNextCheck--;

                    if (instructionsUntilNextCheck == 0)
                    {
                        instructionsUntilNextCheck = _timeCheckInterval;

                        if (stopwatch.Elapsed >= duration)
                        {
                            State = FluenceVMState.Paused;
                            return;
                        }
                    }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        private void ExecuteIncrementIntUnrestricted(InstructionLine instruction)
        {
            if (instruction.Lhs is TempValue temp)
            {
                SetRegister(temp, new RuntimeValue(GetRuntimeValue(temp).IntValue + 1));
                return;
            }

            VariableValue var = (VariableValue)instruction.Lhs;
            SetVariable(var, new RuntimeValue(GetRuntimeValue(var).IntValue + 1));
        }

        private void ExecuteLoadAddress(InstructionLine instruction)
        {
            ReferenceValue reference = (ReferenceValue)instruction.Lhs;
            _operandStack.Push(new RuntimeValue(reference));
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
                SpecializedOpcodeHandler? handler = InlineCacheManager.CreateSpecializedAddHandler(instruction, this, left, right);
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
                ListObject concatenatedList = new ListObject();

                concatenatedList.Elements.AddRange(leftList.Elements);
                concatenatedList.Elements.AddRange(rightList.Elements);

                SetVariableOrRegister(instruction.Lhs, new RuntimeValue(concatenatedList));
                return;
            }

            SignalError($"Runtime Error: Cannot apply operator '+' to types {GetDetailedTypeName(left)} and {GetDetailedTypeName(right)}.");
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
                SpecializedOpcodeHandler? handler = InlineCacheManager.CreateSpecializedSubtractionHandler(instruction, this, left, right);
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
                ListObject concatenatedList = new ListObject();
                // This performs a set difference, which is the intuitive meaning of list subtraction.
                concatenatedList.Elements.AddRange(leftList.Elements.Except(rightList.Elements));
                SetRegister((TempValue)instruction.Lhs, new RuntimeValue(concatenatedList));
                return;
            }

            SignalError($"Runtime Error: Cannot apply operator '-' to types {GetDetailedTypeName(left)} and {GetDetailedTypeName(right)}.");
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
                SpecializedOpcodeHandler? handler = InlineCacheManager.CreateSpecializedMulHandler(instruction, this, left, right);
                if (handler != null)
                {
                    instruction.SpecializedHandler = handler;
                    handler(instruction, this);
                    return;
                }
            }

            if (left.ObjectReference is StringObject strLeft && right.Type == RuntimeValueType.Number)
            {
                SetVariableOrRegister(instruction.Lhs, HandleStringRepetition(strLeft, right));
                return;
            }
            if (left.Type == RuntimeValueType.Number && right.ObjectReference is StringObject strRight)
            {
                SetVariableOrRegister(instruction.Lhs, HandleStringRepetition(strRight, left));
                return;
            }

            if (left.ObjectReference is CharObject charLeft && right.Type == RuntimeValueType.Number)
            {
                SetVariableOrRegister(instruction.Lhs, HandleStringRepetition(new StringObject(charLeft.Value.ToString()), right));
                return;
            }
            if (left.Type == RuntimeValueType.Number && right.ObjectReference is CharObject charRight)
            {
                SetVariableOrRegister(instruction.Lhs, HandleStringRepetition(new StringObject(charRight.Value.ToString()), left));
                return;
            }

            if (left.ObjectReference is ListObject listLeft && right.Type == RuntimeValueType.Number)
            {
                SetVariableOrRegister(instruction.Lhs, HandleListRepetition(listLeft, right));
                return;
            }
            if (left.Type == RuntimeValueType.Number && right.ObjectReference is ListObject listRight)
            {
                SetVariableOrRegister(instruction.Lhs, HandleListRepetition(listRight, left));
                return;
            }

            SignalError($"Runtime Error: Cannot apply operator '*' to types {GetDetailedTypeName(left)} and {GetDetailedTypeName(right)}.");
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

            SpecializedOpcodeHandler? handler = InlineCacheManager.CreateSpecializedDivHandler(instruction, this, left, right);
            if (handler != null)
            {
                instruction.SpecializedHandler = handler;
                handler(instruction, this);
                return;
            }

            SignalError($"Runtime Error: Cannot apply operator '/' to types {GetDetailedTypeName(left)} and {GetDetailedTypeName(right)}.");
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

            SpecializedOpcodeHandler? handler = InlineCacheManager.CreateSpecializedModuloHandler(instruction, this, left, right);
            if (handler != null)
            {
                instruction.SpecializedHandler = handler;
                handler(instruction, this);
                return;
            }

            SignalError($"Runtime Error: Cannot apply operator '%' to types {GetDetailedTypeName(left)} and {GetDetailedTypeName(right)}.");
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

            SpecializedOpcodeHandler? handler = InlineCacheManager.CreateSpecializedPowerHandler(instruction, this, left, right);
            if (handler != null)
            {
                instruction.SpecializedHandler = handler;
                handler(instruction, this);
                return;
            }

            SignalError($"Runtime Error: Cannot apply operator '**' to types {GetDetailedTypeName(left)} and {GetDetailedTypeName(right)}.");
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    SignalError($"Runtime Error: Range bounds must be numbers, not {GetDetailedTypeName(range.Start)} and {GetDetailedTypeName(range.End)}.");
                    return;
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
                AssignVariable(destVar.Name, destVar.Hash, new RuntimeValue(list));
                return;
            }
            // Standard assignment.
            else if (left is VariableValue destVar2)
            {
                AssignVariable(destVar2.Name, destVar2.Hash, sourceValue, destVar2.IsReadOnly);
            }
            else if (left is TempValue destTemp) SetRegister(destTemp, sourceValue);
            else throw ConstructRuntimeException("Internal VM Error: Destination of 'Assign' must be a variable or temporary.");
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
                SignalError($"Internal VM Error: ExecuteNumericBinaryOperation called with non-numeric types.");
            }

            RuntimeValue result = left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => intOp(left.IntValue, right.IntValue),
                    RuntimeNumberType.Long => longOp(left.IntValue, right.LongValue),
                    RuntimeNumberType.Float => floatOp(left.IntValue, right.FloatValue),
                    RuntimeNumberType.Double => doubleOp(left.IntValue, right.DoubleValue),
                    _ => SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right-hand number type."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => longOp(left.LongValue, right.IntValue),
                    RuntimeNumberType.Long => longOp(left.LongValue, right.LongValue),
                    RuntimeNumberType.Float => floatOp(left.LongValue, right.FloatValue),
                    RuntimeNumberType.Double => doubleOp(left.LongValue, right.DoubleValue),
                    _ => SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right-hand number type."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => floatOp(left.FloatValue, right.IntValue),
                    RuntimeNumberType.Long => floatOp(left.FloatValue, right.LongValue),
                    RuntimeNumberType.Float => floatOp(left.FloatValue, right.FloatValue),
                    RuntimeNumberType.Double => doubleOp(left.FloatValue, right.DoubleValue),
                    _ => SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right-hand number type."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => doubleOp(left.DoubleValue, right.IntValue),
                    RuntimeNumberType.Long => doubleOp(left.DoubleValue, right.LongValue),
                    RuntimeNumberType.Float => doubleOp(left.DoubleValue, right.FloatValue),
                    RuntimeNumberType.Double => doubleOp(left.DoubleValue, right.DoubleValue),
                    _ => SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right-hand number type."),
                },
                _ => SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported left-hand number type."),
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
                _ => SignalError<long>("Internal VM Error: Unhandled bitwise operation routed to ExecuteBitwiseOperation.")!,
            };

            if (instruction.Lhs is VariableValue var)
            {
                AssignVariable(var.Name, var.Hash, new RuntimeValue(result), var.IsReadOnly);
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
                SignalError($"Runtime Error: The unary minus operator '-' cannot be applied to a value of type '{GetDetailedTypeName(value)}'.");
                return;
            }

            RuntimeValue result = value.NumberType switch
            {
                RuntimeNumberType.Int => new RuntimeValue(-value.IntValue),
                RuntimeNumberType.Double => new RuntimeValue(-value.DoubleValue),
                RuntimeNumberType.Float => new RuntimeValue(-value.FloatValue),
                RuntimeNumberType.Long => new RuntimeValue(-value.LongValue),
                _ => SignalRecoverableErrorAndReturnNil("Internal VM Error: Invalid number type for negate operation.")
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
                throw ConstructRuntimeException("Internal VM Error: The target of a jump instruction must be a NumberValue.");
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
                throw ConstructRuntimeException("Internal VM Error: The target of a jump instruction must be a NumberValue.");
            }

            RuntimeValue left = GetRuntimeValue(instruction.Rhs);
            RuntimeValue right = GetRuntimeValue(instruction.Rhs2);

            instruction.SpecializedHandler = InlineCacheManager.CreateSpecializedBranchHandler(instruction, right, target);
            bool result = left.Equals(right);

            if (result == target)
            {
                _ip = (int)jmp.Value;
            }
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

            RuntimeValue right = GetRuntimeValue(instruction.Rhs2);
            SetRegister((TempValue)instruction.Lhs, new RuntimeValue(right.IsTruthy));
        }

        /// <summary>
        /// Handles the TO_STRING instruction, which explicitly converts any runtime value to a string object.
        /// </summary>
        private void ExecuteToString(InstructionLine instruction)
        {
            RuntimeValue valueToConvert = GetRuntimeValue(instruction.Rhs);

            SetRegister((TempValue)instruction.Lhs, new RuntimeValue(new StringObject(IntrinsicHelpers.ConvertRuntimeValueToString(this, valueToConvert))));
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
                    _ => SignalError<bool>("Internal VM Error: Invalid comparison instruction for strings.")
                };
                SetRegister(destination, new RuntimeValue(stringResult));
            }

            if (left.Type != RuntimeValueType.Number || right.Type != RuntimeValueType.Number)
            {
                SignalError($"Internal VM Error: Cannot perform numeric comparison on non-number types: ({left.Type}, {right.Type}).");
                return;
            }

            bool result = left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => intOp(left.IntValue, right.IntValue),
                    RuntimeNumberType.Long => longOp(left.IntValue, right.LongValue),
                    RuntimeNumberType.Float => floatOp(left.IntValue, right.FloatValue),
                    RuntimeNumberType.Double => doubleOp(left.IntValue, right.DoubleValue),
                    _ => SignalError<bool>("Internal VM Error: Unsupported right-hand number type."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => longOp(left.LongValue, right.IntValue),
                    RuntimeNumberType.Long => longOp(left.LongValue, right.LongValue),
                    RuntimeNumberType.Float => floatOp(left.LongValue, right.FloatValue),
                    RuntimeNumberType.Double => doubleOp(left.LongValue, right.DoubleValue),
                    _ => SignalError<bool>("Internal VM Error: Unsupported right-hand number type."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => floatOp(left.FloatValue, right.IntValue),
                    RuntimeNumberType.Long => floatOp(left.FloatValue, right.LongValue),
                    RuntimeNumberType.Float => floatOp(left.FloatValue, right.FloatValue),
                    RuntimeNumberType.Double => doubleOp(left.FloatValue, right.DoubleValue),
                    _ => SignalError<bool>("Internal VM Error: Unsupported right-hand number type."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => doubleOp(left.DoubleValue, right.IntValue),
                    RuntimeNumberType.Long => doubleOp(left.DoubleValue, right.LongValue),
                    RuntimeNumberType.Float => doubleOp(left.DoubleValue, right.FloatValue),
                    RuntimeNumberType.Double => doubleOp(left.DoubleValue, right.DoubleValue),
                    _ => SignalError<bool>("Internal VM Error: Unsupported right-hand number type."),
                },
                _ => SignalError<bool>("Internal VM Error: Unsupported right-hand number type."),
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
                throw ConstructRuntimeException("Internal VM Error: NewRange opcode requires a non-null RangeValue operand.");
            }

            TryReturnRegisterReferenceToPool((TempValue)instruction.Lhs);

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
                        length = string.IsNullOrEmpty(str.Value) ? 0 : str.Value.Length;
                        break;
                    case ListObject list:
                        length = list.Elements.Count;
                        break;
                    case RangeObject range:
                        if (range.Start.Type != RuntimeValueType.Number || range.End.Type != RuntimeValueType.Number)
                        {
                            SignalError($"Runtime Error: Cannot get length of a range with non-numeric bounds ({GetDetailedTypeName(range.Start)}, {GetDetailedTypeName(range.End)}).");
                            return;
                        }
                        int start = Convert.ToInt32(range.Start.IntValue);
                        int end = Convert.ToInt32(range.End.IntValue);
                        length = (end < start) ? 0 : (end - start + 1);
                        break;
                    default:
                        SignalError($"Runtime Error: Cannot get the length of a value of type '{GetDetailedTypeName(collection)}'.");
                        return;
                }
            }
            else
            {
                SignalError($"Cannot get length of a non-object type '{collection.Type}'.");
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
                SignalError($"Runtime Error: Cannot push an element to a non-list value (got type '{GetDetailedTypeName(listVal)}').");
                return;
            }

            RuntimeValue elementToAdd = GetRuntimeValue(instruction.Rhs);

            if (elementToAdd.ObjectReference is RangeObject range)
            {
                RuntimeValue startValue = range.Start;
                RuntimeValue endValue = range.End;

                if (startValue.Type != RuntimeValueType.Number || endValue.Type != RuntimeValueType.Number)
                {
                    SignalError($"Runtime Error: Range bounds must be numbers, not {GetDetailedTypeName(range.Start)} and {GetDetailedTypeName(range.End)}.");
                    return;
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
                throw ConstructRuntimeException("Internal VM Error: NewInstance requires a StructSymbol as its operand.");
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
                throw ConstructRuntimeException("Internal VM Error: GetField requires a string literal for the field name.");
            }

            RuntimeValue instanceValue = GetRuntimeValue(instruction.Rhs);
            if (instanceValue.ObjectReference is not InstanceObject instance)
            {
                throw ConstructRuntimeException($"Runtime Error: Cannot access property '{fieldName.Value}' on a non-instance value (got type '{GetDetailedTypeName(instanceValue)}').");
            }

            SetRegister((TempValue)instruction.Lhs, instance.GetField(fieldName.Value, this));
        }

        /// <summary>
        /// Handles the GET_STATIC instruction, retrieving a static field's value from a struct symbol.
        /// </summary>
        private void ExecuteGetStatic(InstructionLine instruction)
        {
            if (instruction.Rhs is not StructSymbol structSymbol ||
                instruction.Rhs2 is not StringValue fieldName)
            {
                throw ConstructRuntimeException("Internal VM Error: Invalid operands for GET_STATIC. Expected StructSymbol and StringValue.");
            }

            if (structSymbol.StaticFields.TryGetValue(fieldName.Value, out RuntimeValue value))
            {
                SetRegister((TempValue)instruction.Lhs, value);
                return;
            }

            SignalError($"Internal VM Erorr: Attempt to retrieve a non-existant static struct field: {structSymbol}__Field:{fieldName.Value}.");
        }

        /// <summary>
        /// Handles the SET_STATIC instruction, assigning a value to a struct's static field.
        /// </summary>
        private void ExecuteSetStatic(InstructionLine instruction)
        {
            if (instruction.Lhs is not StructSymbol structSymbol || instruction.Rhs is not StringValue fieldName)
            {
                throw ConstructRuntimeException("Internal VM Error: Invalid operands for SET_STATIC. Expected StructSymbol and StringValue.");
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
                throw ConstructRuntimeException("Internal VM Error: SetField requires a string literal for the field name.");
            }

            RuntimeValue instanceValue = GetRuntimeValue(instruction.Lhs);
            if (instanceValue.ObjectReference is not InstanceObject instance)
            {
                throw ConstructRuntimeException($"Runtime Error: Cannot set property '{fieldName.Value}' on a non-instance value (got type '{GetDetailedTypeName(instanceValue)}').");
            }

            if (instance.Class.StaticFields.ContainsKey(fieldName.Value))
            {
                throw ConstructRuntimeException($"Runtime Error: Cannot set solid ( static ) property '{fieldName.Value}' of a struct.");
            }

            RuntimeValue valueToSet = GetRuntimeValue(instruction.Rhs2);
            instance.SetField(fieldName.Value, valueToSet);
        }

        /// <summary>
        /// Handles the GET_ELEMENT instruction for retrieving an element from a list by its index.
        /// </summary>
        private void ExecuteGetElement(InstructionLine instruction)
        {
            if (instruction.SpecializedHandler != null)
            {
                instruction.SpecializedHandler(instruction, this);
                return;
            }

            ExecuteGenericGetElement(instruction);
        }

        internal void ExecuteGenericGetElement(InstructionLine instruction)
        {
            RuntimeValue collection = GetRuntimeValue(instruction.Rhs);
            RuntimeValue indexVal = GetRuntimeValue(instruction.Rhs2);

            switch (collection.ObjectReference)
            {
                case ListObject list:
                    {
                        if (indexVal.Type != RuntimeValueType.Number)
                        {
                            SignalError($"Runtime Error: List index must be a number, not '{GetDetailedTypeName(indexVal)}'.");
                            return;
                        }

                        int index = indexVal.IntValue;

                        if (index < 0 || index >= list.Elements.Count)
                        {
                            SignalError($"Runtime Error: Index out of range. Index was {index}, but list size is {list.Elements.Count}.");
                            return;
                        }

                        SetRegister((TempValue)instruction.Lhs, list.Elements[index]);
                        break;
                    }
                case StringObject str:
                    {
                        if (indexVal.Type != RuntimeValueType.Number)
                        {
                            SignalError($"Runtime Error: String index must be a number, not '{GetDetailedTypeName(indexVal)}'.");
                            return;
                        }

                        int index = indexVal.IntValue;

                        if (index < 0 || string.IsNullOrEmpty(str.Value) || index >= str.Value.Length)
                        {
                            SignalError($"Runtime Error: Index out of range. Index was {index}, but string length is '{(str.Value is null ? "The string was empty" : str.Value.Length)}'.");
                            return;
                        }

                        char charAsString = str.Value![index];
                        CharObject resultChar = _charObjectPool.Get();
                        resultChar.Initialize(charAsString);
                        SetRegister((TempValue)instruction.Lhs, new RuntimeValue(resultChar));
                        break;
                    }
                // Not an indexable type.
                default:
                    SignalError($"Runtime Error: Cannot apply index operator [...] to a non-indexable type '{GetDetailedTypeName(collection)}'.");
                    return;
            }

            instruction.SpecializedHandler = InlineCacheManager.CreateSpecializedGetElementHandler(instruction, this, collection, indexVal);
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
                SignalError($"Internal VM Error: Cannot apply index operator [...] to a non-list value (got type '{GetDetailedTypeName(collection)}').");
                return;
            }

            if (indexVal.Type != RuntimeValueType.Number)
            {
                SignalError($"Runtime Error: List index must be a number, not '{GetDetailedTypeName(indexVal)}'.");
                return;
            }

            int index = indexVal.IntValue;

            if (index < 0 || index >= list.Elements.Count)
            {
                SignalError($"Runtime Error: Index out of range. Index was {index}, but list size is {list.Elements.Count}.");
                return;
            }

            list.Elements[index] = valueToSet;
        }

        private void ExecuteTryBlock(InstructionLine instruction)
        {
            TryCatchValue context = (TryCatchValue)instruction.Lhs;

            _tryCatchBlocks.Push(context);
        }

        private void ExecuteCatchBlock(InstructionLine instruction)
        {
            TryCatchValue context = _tryCatchBlocks.Pop();

            if (!context.CaughtException)
            {
                _ip = context.CatchGoToIndex;
            }

            context.CaughtException = false;
        }

        /// <summary>
        /// Handles the NEW_ITERATOR instruction, creating an iterator object for a for-in loop.
        /// </summary>
        private void ExecuteNewIterator(InstructionLine instruction)
        {
            RuntimeValue iterable = GetRuntimeValue(instruction.Rhs);

            if (iterable.ObjectReference is ListObject or RangeObject)
            {
                TryReturnRegisterReferenceToPool((TempValue)instruction.Lhs);

                IteratorObject iterator = _iteratorObjectPool.Get();
                iterator.Initialize(iterable.ObjectReference);
                SetRegister((TempValue)instruction.Lhs, new RuntimeValue(iterator));
                return;
            }

            throw ConstructRuntimeException($"Runtime Error: Cannot create an iterator from a non-iterable type '{GetDetailedTypeName(iterable)}'.");
        }

        /// <summary>
        /// Handles the ITER_NEXT instruction, which advances an iterator and retrieves the next value.
        /// </summary>
        private void ExecuteIterNext(InstructionLine instruction)
        {
            if (instruction.SpecializedHandler != null)
            {
                instruction.SpecializedHandler(instruction, this);
                return;
            }

            ExecuteGenericIterNext(instruction);
        }

        /// <summary>
        /// Handles the ITER_NEXT instruction, which advances an iterator and retrieves the next value.
        /// </summary>
        internal void ExecuteGenericIterNext(InstructionLine instruction)
        {
            // Lhs:  The source iterator register.
            // Rhs:  The destination register for the value.
            // Rhs2: The destination register for the continue flag.

            if (instruction.Lhs is not TempValue iteratorReg ||
                instruction.Rhs is not TempValue valueReg ||
                instruction.Rhs2 is not TempValue continueFlagReg)
            {
                throw ConstructRuntimeException("Internal VM Error: Invalid operands for IterNext. Expected (Source Iterator, Dest Value, Dest Flag).");
            }

            RuntimeValue iteratorVal = _cachedRegisters[iteratorReg.Hash];

            if (iteratorVal.As<IteratorObject>() is not IteratorObject iterator)
            {
                throw ConstructRuntimeException("Internal VM Error: Attempted to iterate over a non-iterator value.");
            }

            SpecializedOpcodeHandler? handler = InlineCacheManager.CreateSpecializedIterNextHandler(instruction, iterator);
            if (handler != null)
            {
                instruction.SpecializedHandler = handler;
                handler(instruction, this);
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
        /// Handles the PUSH_TWO_PARAMS instruction, which pushes two values onto the operand stack in preparation for a function call.
        /// </summary>
        private void ExecutePushTwoParam(InstructionLine instruction)
        {
            _operandStack.Push(GetRuntimeValue(instruction.Lhs));
            _operandStack.Push(GetRuntimeValue(instruction.Rhs));
        }

        /// <summary>
        /// Handles the PUSH_THREE_PARAMS instruction, which pushes three values onto the operand stack in preparation for a function call.
        /// </summary>
        private void ExecutePushThreeParam(InstructionLine instruction)
        {
            _operandStack.Push(GetRuntimeValue(instruction.Lhs));
            _operandStack.Push(GetRuntimeValue(instruction.Rhs));
            _operandStack.Push(GetRuntimeValue(instruction.Rhs2));
        }

        /// <summary>
        /// Handles the PUSH_FOUR_PARAMS instruction, which pushes four values onto the operand stack in preparation for a function call.
        /// This is the most we can push in one instruction since an instruction allows up to four values: Lhs, Rhs, Rhs2, Rhs3.
        /// </summary>
        private void ExecutePushFourParam(InstructionLine instruction)
        {
            _operandStack.Push(GetRuntimeValue(instruction.Lhs));
            _operandStack.Push(GetRuntimeValue(instruction.Rhs));
            _operandStack.Push(GetRuntimeValue(instruction.Rhs2));
            _operandStack.Push(GetRuntimeValue(instruction.Rhs3));
        }

        /// <summary>
        /// Handles the GET_TYPE instruction, which creates a wrapper around a <see cref="TypeMetadata"/> object.
        /// </summary>
        private void ExecuteGetType(InstructionLine instruction)
        {
            TempValue destRegister = (TempValue)instruction.Lhs;
            Value operand = instruction.Rhs;

            TypeMetadata metadata;

            // Raw type name.
            if (operand is StringValue typeNameValue)
            {
                string typeName = typeNameValue.Value;

                if (TryFindSymbol(typeName, out Symbol symbol, out FluenceScope symbolScope))
                {
                    metadata = symbol switch
                    {
                        StructSymbol s => CreateMetadataFromStructSymbol(s, symbolScope),
                        EnumSymbol e => new TypeMetadata(e.Name, $"{symbolScope.Name}.{e.Name}", TypeCategory.Enum, 0, false, enumMembers: e.Members.Keys.ToList()),
                        _ => new TypeMetadata(typeName, typeName, TypeCategory.Unknown, 0, false)
                    };
                }
                else
                {
                    throw ConstructRuntimeException($"Runtime Error: Unknown type or symbol '{typeName}'.");
                }
            }
            // Variable or expression.
            else
            {
                RuntimeValue value = GetRuntimeValue(operand);

                string name = IntrinsicHelpers.GetRuntimeTypeName(value);

                switch (value.ObjectReference)
                {
                    case InstanceObject instance:
                        metadata = CreateMetadataFromStructSymbol(instance.Class, instance.Class.Scope);
                        break;
                    // TO DO
                    //case EnumMemberObject enumMember:
                    //    var e = enumMember.EnumType;
                    //    metadata = new TypeMetadata(e.Name, $"{e.DefiningScope.Name}.{e.Name}", TypeCategory.Enum, enumMembers: e.Members.Keys.ToList());
                    //    break;
                    case ListObject:
                        metadata = new TypeMetadata("List", "List", TypeCategory.BuiltIn, 0, false);
                        break;
                    case FunctionObject func:
                        bool isLambda = operand is LambdaValue;

                        MethodMetadata methodMeta = new MethodMetadata(func.Name, func.Arity, false, func.Parameters, func.ParametersByRef);
                        metadata = new TypeMetadata("function", "function", TypeCategory.Function, func.Arity, isLambda, null, null, null, [methodMeta], null, func.Parameters, func.ParametersByRef);
                        break;
                    default:
                        metadata = new TypeMetadata(name, name, TypeCategory.Primitive, 0, false);
                        break;
                }
            }

            RuntimeValue typeObject = TypeMetadataWrapper.Create(metadata);
            SetRegister(destRegister, typeObject);
        }

        /// <summary>
        /// A helper to create a TypeMetadata object from a StructSymbol.
        /// </summary>
        private static TypeMetadata CreateMetadataFromStructSymbol(StructSymbol s, FluenceScope scope)
        {
            return new TypeMetadata(
                name: s.Name,
                fullName: $"{scope.Name}.{s.Name}",
                category: TypeCategory.Struct,
                0,
                false,
                instanceFields: s.Fields.Select(f => new FieldMetadata(f, IsStatic: false, IsSolid: false)).ToList(),
                staticFields: s.StaticFields.Keys.Select(f => new FieldMetadata(f, IsStatic: true, IsSolid: true)).ToList(),
                constructors: s.Constructors.Values.Select(c => new MethodMetadata(c.Name, c.Arity, true, c.Arguments!, c.ArgumentsByRef!)).ToList(),
                instanceMethods: s.Functions.Values.Select(m => new MethodMetadata(m.Name, m.Arity, false, m.Arguments!, m.ArgumentsByRef!)).ToList()
            );
        }

        /// <summary>
        /// A helper to search all relevant scopes for a named symbol.
        /// </summary>
        private bool TryFindSymbol(string name, out Symbol symbol, out FluenceScope foundScope)
        {
            if (CurrentFrame.Function.DefiningScope?.TryResolve(name, out symbol) ?? false)
            {
                foundScope = CurrentFrame.Function.DefiningScope;
                return true;
            }

            foreach (FluenceScope ns in Namespaces.Values)
            {
                if (ns.TryResolve(name, out symbol))
                {
                    foundScope = ns;
                    return true;
                }
            }

            if (_globalScope.TryResolve(name, out symbol))
            {
                foundScope = _globalScope;
                return true;
            }

            foundScope = null!;
            return false;
        }

        internal void PrepareFunctionCall(CallFrame frame, FunctionObject function)
        {
            _callStack.Push(frame);
            _cachedRegisters = frame.Registers;
            _ip = function.StartAddress;
        }

        internal RuntimeValue PopStack()
        {
            if (_operandStack.Count > 0)
            {
                return _operandStack.Pop();
            }

            throw ConstructRuntimeException("Internal VM Error: Attempt to pop value from the operand stack, the operand stack was empty.");
        }

        /// <summary>
        /// Handles the CALL_FUNCTION instruction, which invokes a standalone function.
        /// </summary>
        private void ExecuteCallFunction(InstructionLine instruction)
        {
            if (instruction.SpecializedHandler != null)
            {
                instruction.SpecializedHandler(instruction, this);
                return;
            }

            RuntimeValue functionVal = GetRuntimeValue(instruction.Rhs);

            if (functionVal.ObjectReference is not FunctionObject function)
            {
                throw ConstructRuntimeException($"Internal VM Error: Attempted to call a value that is not a function (got type '{GetDetailedTypeName(functionVal)}').");
            }

            if (!function.DefiningScope.IsTheGlobalScope)
            {
                string scopeName = function.DefiningScope.Name;
                if (!IsLibraryAllowed(scopeName))
                {
                    throw ConstructRuntimeException($"Security Error: Use of the library '{scopeName}' is disallowed by the host application due to library whiteList and or blackList rules.");
                }
            }

            int argCount = GetRuntimeValue(instruction.Rhs2).IntValue;

            if (function.Arity != argCount && function.Arity != -100)
            {
                throw ConstructRuntimeException($"Internal VM Error: Mismatched arguments for function '{function.Name}'. Expected {function.Arity}, but got {argCount}.");
            }

            SpecializedOpcodeHandler? handler = InlineCacheManager.CreateSpecializedCallFunctionHandler(instruction, function);
            if (handler != null)
            {
                instruction.SpecializedHandler = handler;
                handler(instruction, this);
                return;
            }

            // Intrinsic and normal function calls are handled by the SpecializedHandler, because their function blueprint is not null.
            // If we get here then we are dealing with a lambda function.

            CallFrame newFrame = _callFramePool.Get();
            newFrame.Initialize(function, _ip, (TempValue)instruction.Lhs);

            for (int i = function.Parameters.Count - 1; i >= 0; i--)
            {
                int paramName = function.Parameters[i].GetHashCode();
                bool isRefParam = function.ParametersByRef.Contains(function.Parameters[i]);
                RuntimeValue argFromStack = _operandStack.Pop();

                if (isRefParam)
                {
                    if (argFromStack.ObjectReference is not ReferenceValue)
                    {
                        throw ConstructRuntimeException($"Internal VM Error: Argument '{paramName}' in function: \"{function.ToCodeLikeString()}\" must be passed by reference ('ref').");
                    }

                    VariableValue refVar = ((ReferenceValue)argFromStack.ObjectReference).Reference;
                    newFrame.RefParameterMap[paramName] = refVar.Hash;
                    ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(newFrame.Registers, paramName, out _);
                    valueRef = GetRuntimeValue(refVar);
                }
                else // Pass by value.
                {
                    ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(newFrame.Registers, paramName, out _);
                    valueRef = argFromStack.ObjectReference is ReferenceValue refVal ? GetRuntimeValue(refVal.Reference) : argFromStack;
                }
            }

            _callStack.Push(newFrame);
            _cachedRegisters = newFrame.Registers;
            _ip = function.StartAddress;
        }

        private void ExecuteNewLambda(InstructionLine instruction)
        {
            VariableValue destination = (VariableValue)instruction.Lhs;
            LambdaValue lambdaValue = (LambdaValue)instruction.Rhs;

            string baseName = destination.Name;
            int arity = lambdaValue.Function.Arity;
            string mangledName = Mangler.Mangle(baseName, arity);

            lambdaValue.Function.SetName(mangledName);
            FunctionObject lambdaObject = CreateFunctionObject(lambdaValue.Function);
            lambdaObject.IsLambda = true;

            AssignVariable(baseName, destination.Hash, new RuntimeValue(lambdaObject), destination.IsReadOnly);
            AssignVariable(mangledName, destination.Hash, new RuntimeValue(lambdaObject), destination.IsReadOnly);
        }

        /// <summary>
        /// Handles the CALL_METHOD instruction code, which invokes a method on an object instance.
        /// </summary>
        private void ExecuteCallMethod(InstructionLine instruction)
        {
            if (instruction.Rhs2 is not StringValue methodNameVal)
            {
                throw ConstructRuntimeException("Internal VM Error: Invalid operands for CallMethod. Expected a method name as a string.");
            }

            string methodName = methodNameVal.Value;
            RuntimeValue instanceVal = GetRuntimeValue(instruction.Rhs);

            if (instanceVal.ObjectReference is IFluenceObject fluenceObject)
            {
                if (fluenceObject.TryGetIntrinsicMethod(methodName, out IntrinsicRuntimeMethod? intrinsicMethod))
                {
                    SetRegister((TempValue)instruction.Lhs, intrinsicMethod(this, instanceVal));
                    return;
                }
            }

            if (instanceVal.ObjectReference is not InstanceObject instance)
            {
                throw ConstructRuntimeException($"Internal VM Error: Cannot call method '{methodName}' on a non-instance object of type '{GetDetailedTypeName(instanceVal)}'.");
            }

            FunctionValue methodBlueprint = null!;
            FunctionObject functionToExecute = null;

            // <script> frame.
            if (CurrentFrame.ReturnAddress == _byteCode.Count)
            {
                methodBlueprint = instance.Class.Constructors[methodNameVal.Value];
                if (methodBlueprint == null)
                {
                    SetRegister((TempValue)instruction.Lhs, instanceVal);
                    return;
                }
            }
            else
            {
                // A class field that is a function, that is currently a lambda.
                if (instance.Class.Fields.Contains(Mangler.Demangle(methodName)))
                {
                    functionToExecute = (FunctionObject)instance.GetField(Mangler.Demangle(methodName), this).ObjectReference;
                    functionToExecute!.IsLambda = true;
                }
                else if (!instance.Class.Functions.TryGetValue(methodName, out methodBlueprint) && !instance.Class.Constructors.TryGetValue(methodName, out methodBlueprint))
                {
                    throw ConstructRuntimeException($"Internal VM Error: Undefined method or lambda '{methodName}' on struct '{instance.Class.Name}'.");
                }
            }

            if (functionToExecute == null)
            {
                functionToExecute = new FunctionObject(
                    methodBlueprint.Name,
                    methodBlueprint.Arity,
                    methodBlueprint.Arguments,
                    methodBlueprint.StartAddress,
                    methodBlueprint.FunctionScope
                )
                {
                    ParametersByRef = methodBlueprint.ArgumentsByRef
                };
            }

            if (!functionToExecute.DefiningScope.IsTheGlobalScope)
            {
                string scopeName = functionToExecute.DefiningScope.Name;
                if (!IsLibraryAllowed(scopeName))
                {
                    throw ConstructRuntimeException($"Security Error: Use of the library '{scopeName}' is disallowed by the host application due to library whiteList and or blackList rules.");
                }
            }

            int argCountOnStack = _operandStack.Count;
            if (functionToExecute.Arity != argCountOnStack)
            {
                throw ConstructRuntimeException($"Internal VM Error: Mismatched arity for method '{functionToExecute.Name}'. Expected {functionToExecute.Arity}, but got {argCountOnStack}.");
            }

            CallFrame newFrame = _callFramePool.Get();
            newFrame.Initialize(functionToExecute, _ip, (TempValue)instruction.Lhs);

            // Implicitly pass 'self'.
            newFrame.Registers["self".GetHashCode()] = instanceVal;

            for (int i = functionToExecute.Parameters.Count - 1; i >= 0; i--)
            {
                string paramName = functionToExecute.Parameters[i];
                bool isRefParam = functionToExecute.ParametersByRef.Contains(paramName);
                int paramHash = paramName.GetHashCode();
                RuntimeValue argFromStack = _operandStack.Pop();

                if (isRefParam)
                {
                    if (argFromStack.ObjectReference is not ReferenceValue)
                    {
                        throw ConstructRuntimeException($"Internal VM Error: Argument '{paramName}' in function: \"{functionToExecute.ToCodeLikeString()}\" must be passed by reference ('ref').");
                    }

                    VariableValue refVar = ((ReferenceValue)argFromStack.ObjectReference).Reference;
                    newFrame.RefParameterMap[paramHash] = refVar.Hash;
                    ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(newFrame.Registers, paramHash, out _);
                    valueRef = GetRuntimeValue(refVar);
                }
                else // Pass by value.
                {
                    ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(newFrame.Registers, paramHash, out _);
                    valueRef = argFromStack.ObjectReference is ReferenceValue refVal ? GetRuntimeValue(refVal.Reference) : argFromStack;
                }
            }

            _callStack.Push(newFrame);
            _cachedRegisters = newFrame.Registers;
            _ip = functionToExecute.StartAddress;
        }

        /// <summary>
        /// Executes a manual method call from outside the virtual machine.
        /// </summary>
        /// <param name="instance">The instance of a struct to call the method on.</param>
        /// <param name="func">The function of the instance to call.</param>
        /// <returns>The result of the function's return.</returns>
        internal RuntimeValue ExecuteManualMethodCall(InstanceObject instance, FunctionValue func)
        {
            int savedIp = _ip;
            Dictionary<int, RuntimeValue> savedRegisters = _cachedRegisters;

            FunctionObject functionToExecute = CreateFunctionObject(func);
            CallFrame newFrame = _callFramePool.Get();

            newFrame.Initialize(functionToExecute, -1, null!);

            newFrame.Registers["self".GetHashCode()] = new RuntimeValue(instance);

            _callStack.Push(newFrame);
            _cachedRegisters = newFrame.Registers;
            _ip = functionToExecute.StartAddress;

            RuntimeValue returnValue = RuntimeValue.Nil;

            while (true)
            {
                if (_callStack.Peek() != newFrame)
                {
                    break;
                }

                InstructionLine instruction = _byteCode[_ip];
                _ip++;

                if (instruction.Instruction == InstructionCode.Return)
                {
                    returnValue = GetRuntimeValue(instruction.Lhs);

                    _callStack.Pop();
                    _callFramePool.Return(newFrame);
                    break;
                }

                if (instruction.SpecializedHandler != null)
                {
                    instruction.SpecializedHandler(instruction, this);
                }
                else
                {
                    if (instruction.Instruction is InstructionCode.Goto)
                    {
                        _ip = (int)((NumberValue)instruction.Lhs).Value;
                        continue;
                    }
                    _dispatchTable[(int)instruction.Instruction](instruction);
                }
            }

            _ip = savedIp;
            _cachedRegisters = savedRegisters;

            return returnValue;
        }

        private void ExecuteCallStatic(InstructionLine instruction)
        {
            if (instruction.Rhs is not StructSymbol structSymbol ||
                instruction.Rhs2 is not StringValue methodName)
            {
                throw CreateRuntimeException("Internal VM Error: Invalid operands for CALL_STATIC.");
            }

            if (structSymbol.StaticIntrinsics.TryGetValue(methodName.Value, out FunctionSymbol intrinsicSymbol))
            {
                int argCount = _operandStack.Count;
                if (intrinsicSymbol.Arity != argCount)
                {
                    CreateAndThrowRuntimeException($"Runtime Error: Mismatched arity for static intrinsic struct function '{intrinsicSymbol.Name}'. Expected {intrinsicSymbol.Arity}, but got {argCount}.");
                }

                RuntimeValue resultValue = intrinsicSymbol.IntrinsicBody!(this, argCount);
                SetRegister((TempValue)instruction.Lhs, resultValue);
                return;
            }

            if (!structSymbol.Functions.TryGetValue(methodName.Value, out FunctionValue? methodBlueprint))
            {
                throw ConstructRuntimeException($"Runtime Error: Static function '{methodName.Value}' not found on struct '{structSymbol.Name}'.");
            }

            FunctionObject functionToExecute = new FunctionObject(
                methodBlueprint.Name,
                methodBlueprint.Arity,
                methodBlueprint.Arguments,
                methodBlueprint.StartAddress,
                methodBlueprint.FunctionScope
            )
            {
                ParametersByRef = methodBlueprint.ArgumentsByRef
            };

            int argCountOnStack = _operandStack.Count;
            if (functionToExecute.Arity != argCountOnStack)
            {
                CreateAndThrowRuntimeException($"Runtime Error: Mismatched arity for static function '{functionToExecute.Name}'. Expected {functionToExecute.Arity}, but got {argCountOnStack}.");
            }

            if (!functionToExecute.DefiningScope.IsTheGlobalScope)
            {
                string scopeName = functionToExecute.DefiningScope.Name;
                if (!IsLibraryAllowed(scopeName))
                {
                    CreateAndThrowRuntimeException($"Security Error: Use of the library '{scopeName}' is disallowed by the host application due to library whiteList and or blackList rules.");
                }
            }

            CallFrame newFrame = _callFramePool.Get();
            newFrame.Initialize(functionToExecute, _ip, (TempValue)instruction.Lhs);

            for (int i = functionToExecute.Parameters.Count - 1; i >= 0; i--)
            {
                string paramName = functionToExecute.Parameters[i];
                int paramHash = paramName.GetHashCode();
                bool isRefParam = functionToExecute.ParametersByRef.Contains(paramName);
                RuntimeValue argFromStack = _operandStack.Pop();

                if (isRefParam)
                {
                    if (argFromStack.ObjectReference is not ReferenceValue)
                    {
                        throw CreateRuntimeException($"Internal VM Error: Argument '{paramName}' in function: \"{functionToExecute.ToCodeLikeString()}\" must be passed by reference ('ref').");
                    }

                    VariableValue refVar = ((ReferenceValue)argFromStack.ObjectReference).Reference;
                    newFrame.RefParameterMap[paramHash] = refVar.Hash;
                    ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(newFrame.Registers, paramHash, out _);
                    valueRef = GetRuntimeValue(refVar);
                }
                else // Pass by value.
                {
                    ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(newFrame.Registers, paramHash, out _);
                    valueRef = argFromStack.ObjectReference is ReferenceValue refVal ? GetRuntimeValue(refVal.Reference) : argFromStack;
                }
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
                _callFramePool.Return(finishedFrame);
                return;
            }

            if (finishedFrame.DestinationRegister != null)
            {
                ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_cachedRegisters, finishedFrame.DestinationRegister.Hash, out _);
                valueRef = returnValue;
            }

            foreach (KeyValuePair<int, int> mapping in finishedFrame.RefParameterMap)
            {
                int paramName = mapping.Key;
                int originalVarName = mapping.Value;

                RuntimeValue finalValue = finishedFrame.Registers[paramName];
                _cachedRegisters[originalVarName] = finalValue;
            }

            _ip = finishedFrame.ReturnAddress;

            // Lambdas don't return thier function object until their' parent call frame returns.
            if (!finishedFrame.Function.IsLambda)
            {
                _functionObjectPool.Return(finishedFrame.Function);
            }
            _callFramePool.Return(finishedFrame);
        }

        internal void SetVariableOrRegister(Value target, RuntimeValue value)
        {
            if (target is VariableValue var)
            {
                AssignVariable(var.Name, var.Hash, value, var.IsReadOnly);
                return;
            }

            SetRegister((TempValue)target, value);
        }

        /// <summary>
        /// Sets the value of a variable directly, avoiding extra checks.
        /// </summary>
        /// <param name="var">The Variable.</param>
        /// <param name="val">The value to assign to.</param>
        internal void SetVariable(VariableValue var, RuntimeValue val)
        {
            if (CurrentFrame.ReturnAddress != _byteCode.Count)
            {
                ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_cachedRegisters, var.Hash, out _);
                valueRef = val;
                return;
            }

            ref RuntimeValue valueRef2 = ref CollectionsMarshal.GetValueRefOrAddDefault(_globals, var.Hash, out _);
            valueRef2 = val;
        }

        /// <summary>
        /// Converts a compile-time <see cref="Value"/> from bytecode into a runtime <see cref="RuntimeValue"/>.
        /// </summary>
        internal RuntimeValue GetRuntimeValue(Value val)
        {
            if (val is TempValue temp)
            {
                ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(_cachedRegisters, temp.Hash);

                return !Unsafe.IsNullRef(ref valueRef) ? valueRef : RuntimeValue.Nil;
            }

            if (val is VariableValue variable)
            {
                return ResolveVariable(variable.Name, variable.Hash, val);
            }

            if (val is FunctionValue func)
            {
                // A FunctionValue from the bytecode is just a blueprint.
                // We must convert it into a live, runtime FunctionObject.
                return new RuntimeValue(CreateFunctionObject(func));
            }

            return val switch
            {
                CharValue ch => ResolveCharObjectRuntimeValue(ch.Value),
                EnumValue enumVal => new RuntimeValue(enumVal.Value),
                NumberValue num => num.Type switch
                {
                    NumberValue.NumberType.Integer => new RuntimeValue((int)num.Value),
                    NumberValue.NumberType.Float => new RuntimeValue((float)num.Value),
                    NumberValue.NumberType.Double => new RuntimeValue((double)num.Value),
                    NumberValue.NumberType.Long => new RuntimeValue((long)num.Value),
                    _ => SignalRecoverableErrorAndReturnNil($"Internal VM Error: Unrecognized NumberType '{num.Type}' in bytecode.")
                },
                BooleanValue boolean => new RuntimeValue(boolean.Value),
                NilValue => RuntimeValue.Nil,
                StringValue str => ResolveStringObjectRuntimeValue(str.Value),
                RangeValue range => ResolveRangeObjectRuntimeValue(GetRuntimeValue(range.Start), GetRuntimeValue(range.End)),
                LambdaValue lambda => new RuntimeValue(CreateFunctionObject(lambda.Function)),
                _ => SignalRecoverableErrorAndReturnNil($"Internal VM Error: Unrecognized Value type '{val.GetType().Name}' during conversion.")
            };
        }

        internal RuntimeValue ResolveCharObjectRuntimeValue(char ch)
        {
            CharObject chr = _charObjectPool.Get();
            chr.Initialize(ch);
            return new RuntimeValue(chr);
        }

        internal RuntimeValue ResolveStringObjectRuntimeValue(string strv)
        {
            StringObject str = _stringObjectPool.Get();
            str.Initialize(strv);
            return new RuntimeValue(str);
        }

        private RuntimeValue ResolveRangeObjectRuntimeValue(RuntimeValue start, RuntimeValue end)
        {
            RangeObject range = _rangeObjectPool.Get();
            range.Initialize(start, end);
            return new RuntimeValue(range);
        }

        /// <summary>
        /// Resolves a variable name to its runtime value by searching the current scope hierarchy.
        /// </summary>
        /// <param name="name">The name of the variable to resolve.</param>
        /// <returns>The <see cref="RuntimeValue"/> associated with the variable name.</returns>
        /// <exception cref="FluenceRuntimeException">Thrown if the variable is not defined in any accessible scope.</exception>
        private RuntimeValue ResolveVariable(string name, int hash, Value val = null!)
        {
            ref RuntimeValue localValue = ref CollectionsMarshal.GetValueRefOrNullRef(_cachedRegisters, hash);
            if (!Unsafe.IsNullRef(ref localValue))
            {
                return localValue;
            }

            FluenceScope lexicalScope = CurrentFrame.Function.DefiningScope;
            if (lexicalScope.TryResolve(name, out Symbol symbol))
            {
                return ResolveVariableFromScopeSymbol(symbol, lexicalScope);
            }

            if (CurrentFrame.ReturnAddress == _byteCode.Count)
            {
                foreach (FluenceScope item in Namespaces.Values)
                {
                    FluenceScope lexicalScope2 = item;

                    if (lexicalScope2.TryResolve(name, out Symbol symb))
                    {
                        return ResolveVariableFromScopeSymbol(symb, lexicalScope2);
                    }
                }
            }

            foreach (Symbol valSymbol in _globalScope.Symbols.Values)
            {
                if (valSymbol is VariableSymbol varSymbol && varSymbol.Hash == hash)
                {
                    return GetRuntimeValue(varSymbol.Value);
                }
            }

            // Last case, a Lambda, and if not undefined.
            if (val is not null and VariableValue)
            {
                // TO DO, hash lambda names.
                ref RuntimeValue lambda = ref CollectionsMarshal.GetValueRefOrNullRef(_cachedRegisters, Mangler.Demangle(name).GetHashCode());
                if (!Unsafe.IsNullRef(ref lambda))
                {
                    return lambda;
                }
            }

            throw ConstructRuntimeException($"Runtime Error: Undefined variable '{name}'.", RuntimeExceptionType.UnknownVariable);
        }

        /// <summary>
        /// Resolves complex symbols into RuntimeValues.
        /// </summary>
        /// <param name="symbol">The symbol to resolve from.</param>
        /// <param name="scope">The scope of the symbol</param>
        /// <returns>The <see cref="RuntimeValue"/> resolved from the symbol.</returns>
        private RuntimeValue ResolveVariableFromScopeSymbol(Symbol symbol, FluenceScope scope)
        {
            if (symbol is FunctionSymbol funcSymbol2)
            {
                return new RuntimeValue(CreateFunctionObject(funcSymbol2));
            }
            else if (symbol is VariableSymbol variable)
            {
                // Current scopt also contains all symbols from namespaces it uses.
                ref RuntimeValue namespaceRuntimeValue = ref CollectionsMarshal.GetValueRefOrNullRef(scope.RuntimeStorage, variable.Hash);
                if (!Unsafe.IsNullRef(ref namespaceRuntimeValue))
                {
                    return namespaceRuntimeValue;
                }

                ref RuntimeValue namespaceRuntimeValue2 = ref CollectionsMarshal.GetValueRefOrNullRef(_globals, variable.Hash);
                if (!Unsafe.IsNullRef(ref namespaceRuntimeValue2))
                {
                    return namespaceRuntimeValue2;
                }

                foreach (FluenceScope item in Namespaces.Values)
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
        /// Retrieves or creates a FunctionObject and initializes it from a given <see cref="FunctionSymbol"/> object.
        /// </summary>
        /// <param name="funcSymbol">The blueprint for the <see cref="FunctionSymbol"/> to create.</param>
        /// <returns>The initialized <see cref="FunctionObject"/>.</returns>
        internal FunctionObject CreateFunctionObject(FunctionSymbol funcSymbol)
        {
            FunctionObject func = _functionObjectPool.Get();

            if (funcSymbol.IsIntrinsic)
            {
                func.Initialize(funcSymbol.Name, funcSymbol.Arity, funcSymbol.IntrinsicBody!, funcSymbol.DefiningScope, funcSymbol);
                return func;
            }

            func.Initialize(funcSymbol.Name, funcSymbol.Arity, funcSymbol.Arguments, funcSymbol.StartAddress, funcSymbol.DefiningScope, funcSymbol, funcSymbol.StartAddressInSource);
            func.ParametersByRef = funcSymbol.ArgumentsByRef;
            return func;
        }

        /// <summary>
        /// Retrieves or creates a FunctionObject and initializes it from a given <see cref="FunctionValue"/> object.
        /// </summary>
        /// <param name="funcSymbol">The blueprint for the <see cref="FunctionObject"/> to create.</param>
        /// <returns>The initialized <see cref="FunctionObject"/>.</returns>
        private FunctionObject CreateFunctionObject(FunctionValue funcSymbol)
        {
            FunctionObject func = _functionObjectPool.Get();

            func.Initialize(funcSymbol.Name, funcSymbol.Arity, funcSymbol.Arguments, funcSymbol.StartAddress, funcSymbol.FunctionScope, null!, funcSymbol.StartAddressInSource);
            func.ParametersByRef = funcSymbol.ArgumentsByRef;
            return func;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void TryReturnRegisterReferenceToPool(TempValue register)
        {
            RuntimeValue registerValue = GetRegisterValue(register.Hash);

            switch (registerValue.ObjectReference)
            {
                case RangeObject range:
                    _rangeObjectPool.Return(range);
                    break;
                case IteratorObject iter:
                    _iteratorObjectPool.Return(iter);
                    break;
                case CharObject chr:
                    _charObjectPool.Return(chr);
                    break;
            }
        }

        /// <summary>
        /// Writes a value to a specified temporary register in the current call frame.
        /// </summary>
        internal void SetRegister(TempValue destination, RuntimeValue value)
        {
            ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_cachedRegisters, destination.Hash, out _);
            valueRef = value;
        }

        /// <summary>
        /// Returns the current <see cref="RuntimeValue"/> of the given register of the local CallFrame.
        /// </summary>
        /// <param name="name">The name of the temporary register.</param>
        internal RuntimeValue GetRegisterValue(int hash)
        {
            ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(CurrentFrame.Registers, hash, out _);
            return valueRef;
        }

        internal void ReturnCharObjectToPool(CharObject chr) => _charObjectPool.Return(chr);
        internal void ReturnIteratorObjectToPool(IteratorObject iter) => _iteratorObjectPool.Return(iter);
        internal void ReturnRangeObjectToPool(RangeObject range) => _rangeObjectPool.Return(range);
        internal void ReturnFunctionObjectToPool(FunctionObject func) => _functionObjectPool.Return(func);

        /// <summary>
        /// Assigns a value to a variable.
        /// </summary>
        private void AssignVariable(string name, int hash, RuntimeValue value, bool readOnly = false)
        {
            ref bool isReadonlyRef = ref CollectionsMarshal.GetValueRefOrNullRef(CurrentFrame.WritableCache, hash);
            ref bool isReadOnlyGlobalRef = ref CollectionsMarshal.GetValueRefOrNullRef(GlobalWritableCache, hash);

            if (!Unsafe.IsNullRef(ref isReadonlyRef))
            {
                if (isReadonlyRef)
                {
                    CreateAndThrowRuntimeException($"Runtime Error: Cannot assign to the readonly variable '{name}'.");
                }
            }
            else if (!Unsafe.IsNullRef(ref isReadOnlyGlobalRef))
            {
                if (isReadOnlyGlobalRef)
                {
                    CreateAndThrowRuntimeException($"Runtime Error: Cannot assign to the readonly global variable '{name}'.");
                }
            }
            else
            {
                bool isVariableReadonly = false;
                if (readOnly)
                {
                    isVariableReadonly = true;
                }
                else if (CurrentFrame.Function.DefiningScope.TryResolve(name, out Symbol symbol) && symbol is VariableSymbol { IsReadonly: true })
                {
                    CreateAndThrowRuntimeException($"Runtime Error: Cannot assign to the readonly or solid variable '{name}'.");
                }
                else if (_globalScope.TryResolve(name, out symbol) && symbol is VariableSymbol symb && symb.IsReadonly)
                {
                    CreateAndThrowRuntimeException($"Runtime Error: Cannot assign to the readonly or solid variable '{name}'.");
                }

                ref bool cacheSlot = ref CollectionsMarshal.GetValueRefOrAddDefault(CurrentFrame.WritableCache, hash, out _);
                cacheSlot = isVariableReadonly;

                if (CurrentFrame.ReturnAddress == _byteCode.Count)
                {
                    ref bool globalCacheSlot = ref CollectionsMarshal.GetValueRefOrAddDefault(GlobalWritableCache, hash, out _);
                    globalCacheSlot = isVariableReadonly;
                }
            }

            // If we are not in the top-level script, assign to the current frame's local registers.
            if (CurrentFrame.ReturnAddress != _byteCode.Count)
            {
                ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_cachedRegisters, hash, out _);
                valueRef = value;
                return;
            }

            ref RuntimeValue valueRef2 = ref CollectionsMarshal.GetValueRefOrAddDefault(_globals, hash, out _);
            valueRef2 = value;
        }

        /// <summary>
        /// Helper to safely convert any numeric RuntimeValue to a long for bitwise operations.
        /// </summary>
        private long ToLong(RuntimeValue value)
        {
            if (value.Type != RuntimeValueType.Number)
            {
                return SignalError<long>($"Internal VM Error: Bitwise operations require integer numbers, but got a {value.Type}.");
            }

            return value.NumberType switch
            {
                RuntimeNumberType.Int => value.IntValue,
                RuntimeNumberType.Long => value.LongValue,
                // Floats and doubles are truncated (decimal part is cut off).
                RuntimeNumberType.Float => (long)value.FloatValue,
                RuntimeNumberType.Double => (long)value.DoubleValue,
                _ => SignalError<long>("Internal VM Error: Unhandled number type in bitwise op."),
            };
        }

        /// <summary>
        /// Helper to safely convert any numeric RuntimeValue to an int for bit shift amounts.
        /// </summary>
        private int ToInt(RuntimeValue value)
        {
            if (value.Type != RuntimeValueType.Number)
            {
                return SignalError<int>($"Internal VM Error: Left or Right Bit Shift amount must be an integer number, but got a {value.Type}.");
            }

            return (int)ToLong(value);
        }

        /// <summary>
        /// Handles the logic for repeating a list's elements N times.
        /// </summary>
        private RuntimeValue HandleListRepetition(ListObject list, RuntimeValue num)
        {
            if (num.NumberType is not RuntimeNumberType.Int and not RuntimeNumberType.Long)
            {
                return SignalRecoverableErrorAndReturnNil($"Internal VM Error: Cannot multiply a list by a non-integer number ({num.NumberType}).");
            }

            int count = Convert.ToInt32(num.IntValue);
            ListObject repeatedList = new ListObject();

            if (count > 0)
            {
                // Pre-allocate capacity for efficiency.
                repeatedList.Elements.Capacity = list.Elements.Count * count;
                for (int i = 0; i < count; i++)
                {
                    foreach (RuntimeValue element in list.Elements)
                    {
                        if (element.ObjectReference is ICloneableFluenceObject cloneable)
                        {
                            repeatedList.Elements.Add(new RuntimeValue(cloneable.CloneObject()));
                        }
                        else
                        {
                            repeatedList.Elements.Add(element);
                        }
                    }
                }
            }

            // Multiplying by 0 or a negative number results in an empty list.
            return new RuntimeValue(repeatedList);
        }

        /// <summary>
        /// Determines if a library is allowed to be loaded based on the current
        /// whitelist and blacklist rules.
        /// </summary>
        /// <param name="libraryName">The name of the library being checked.</param>
        /// <returns>True if the library is permitted to be used.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsLibraryAllowed(string libraryName)
        {
            if (_disallowedIntrinsicLibraries.Contains(libraryName))
            {
                return false;
            }

            if (_allowedIntrinsicLibraries.Count > 0 && !_allowedIntrinsicLibraries.Contains(libraryName))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Helper for string/list repetition. Throws a user-friendly runtime exception for non-integer multipliers.
        /// </summary>
        private RuntimeValue HandleStringRepetition(StringObject str, RuntimeValue num)
        {
            if (num.NumberType is not RuntimeNumberType.Int and not RuntimeNumberType.Long)
            {
                return SignalRecoverableErrorAndReturnNil($"Internal VM Error: Cannot multiply a string by a non-integer number (got {num.NumberType}).");
            }

            int count = Convert.ToInt32(num.IntValue);

            // Multiplying by 0 or a negative number results in an empty string.
            if (count <= 0)
            {
                return ResolveStringObjectRuntimeValue("");
            }

            StringBuilder sb = new StringBuilder((str.Value?.Length ?? 0) * count);
            for (int i = 0; i < count; i++)
            {
                sb.Append(str.Value);
            }

            return ResolveStringObjectRuntimeValue(sb.ToString());
        }

        /// <summary>
        /// Gets a detailed, user-friendly type name for a runtime value.
        /// </summary>
        internal static string GetDetailedTypeName(RuntimeValue value)
        {
            if (value.Type == RuntimeValueType.Object && value.ObjectReference != null)
            {
                return value.ObjectReference.GetType().Name;
            }

            // For primitives.
            return value.Type.ToString();
        }

        internal readonly record struct StackFrameInfo
        {
            internal readonly string FunctionName;
            internal readonly string FileName;
            internal readonly int LineNumber;

            internal StackFrameInfo(string name, string fileName, int line)
            {
                FunctionName = name;
                FileName = fileName;
                LineNumber = line;
            }

            public override string ToString()
            {
                return $"StackFrameInfo: Line:{LineNumber}, Function:{FunctionName}, File:{FileName}";
            }
        }

        internal enum RuntimeExceptionType
        {
            NonSpecific,
            UnknownVariable
        }

        /// <summary>
        /// Handles a runtime error that is allowed to be catched. If a try-catch block is active, it redirects the instruction pointer
        /// to the catch block. Otherwise, it throws an unhandled exception, terminating the VM.
        /// </summary>
        /// <param name="exception">The error message.</param>
        [DoesNotReturn]
        internal void SignalError(string exception, RuntimeExceptionType exceptionType = RuntimeExceptionType.NonSpecific)
        {
            if (_tryCatchBlocks.Count > 0)
            {
                TryCatchValue tryCatchContext = _tryCatchBlocks.Pop();

                // Error in try block.
                if (_ip < tryCatchContext.TryGoToIndex)
                {
                    if (tryCatchContext.HasExceptionVar && !string.IsNullOrEmpty(tryCatchContext.ExceptionAsVar))
                    {
                        _cachedRegisters[tryCatchContext.ExceptionAsVar.GetHashCode()] = new RuntimeValue(new ExceptionObject(exception));
                    }

                    // Jumps to catch block.
                    _ip = tryCatchContext.TryGoToIndex;
                    tryCatchContext.CaughtException = true;

                    // We empty any and all pushed arguments.
                    // This way arguments pushed for whatever reason in the try block won't be left behind.
                    // Although this might be a bad decision.
                    // TO DO: Needs testing.
                    while (_operandStack.Count > 0)
                    {
                        _operandStack.Pop();
                    }

                    _tryCatchBlocks.Push(tryCatchContext);
                }
                // Error in catch block.
                else if (_ip > tryCatchContext.TryGoToIndex && _ip < tryCatchContext.CatchGoToIndex)
                {
                    throw CreateRuntimeException(exception, exceptionType);
                }
            }
            else
            {
                throw CreateRuntimeException(exception, exceptionType);
            }
        }

        /// <summary>
        /// Handles a runtime error. If a try-catch block is active, it redirects the instruction pointer
        /// to the catch block. Otherwise, it throws an unhandled exception, terminating the VM.
        /// Accepts a <see cref="T"/> type to satisfy the compiler.
        /// </summary>
        /// <param name="exception">The error message.</param>
        internal T SignalError<T>(string exception, RuntimeExceptionType exceptionType = RuntimeExceptionType.NonSpecific)
        {
            SignalError(exception, exceptionType);
            return default;
        }

        /// <summary>
        /// Handles a runtime error. If a try-catch block is active, it redirects the instruction pointer
        /// to the catch block. Otherwise, it throws an unhandled exception, terminating the VM.
        /// Returns Nil if the exception has been caught.
        /// </summary>
        internal RuntimeValue SignalRecoverableErrorAndReturnNil(string message, RuntimeExceptionType exceptionType = RuntimeExceptionType.NonSpecific)
        {
            SignalError(message, exceptionType);
            return RuntimeValue.Nil;
        }

        /// <summary>
        /// Creates and throws a runtime error that can not be caught.
        /// </summary>
        /// <param name="exception">The exception message.</param>
        /// <param name="exceptionType">The type of the exception.</param>
        internal void CreateAndThrowRuntimeException(string exception, RuntimeExceptionType exceptionType = RuntimeExceptionType.NonSpecific) => throw CreateRuntimeException(exception, exceptionType);

        /// <summary>
        /// Creates a runtime error that can not be caught.
        /// </summary>
        /// <param name="exception">The exception message.</param>
        /// <param name="exceptionType">The type of the exception.</param>
        internal FluenceRuntimeException ConstructRuntimeException(string exception, RuntimeExceptionType exceptionType = RuntimeExceptionType.NonSpecific) => CreateRuntimeException(exception, exceptionType);

        //  These exceptions should not be catchable
        //
        //  1. Readonly assignment.
        //  2. wrong argument count in function call.
        //  3. undefined variable.
        //  4. undefined function.
        //  5. calling non function.
        //  6. wrong struct field/function.
        //  7. break/continue outside loop.
        //  8. invalid return.
        //  9. ???

        /// <summary>
        /// Creates and logs to the console a highly detailed exception with the current state of the VM.
        /// </summary>
        /// <param name="exception">The exception message.</param>
        private FluenceRuntimeException CreateRuntimeException(string exception, RuntimeExceptionType excType = RuntimeExceptionType.NonSpecific)
        {
            VMDebugContext debugCtx = new VMDebugContext(this);
            List<StackFrameInfo> stackFrames = new List<StackFrameInfo>();

            while (_callStack.Count != 0)
            {
                CallFrame frame = _callStack.Pop();
                InstructionLine insn = _byteCode[_ip];
                string fileName = _parser.IsMultiFileProject ? _parser.CurrentParseState.ProjectFilePaths[insn.ProjectFileIndex] : "Script";

                stackFrames.Add(new StackFrameInfo(frame.Function.Name, fileName, frame.Function.StartAddressInSource));
            }

            RuntimeExceptionContext context = new RuntimeExceptionContext()
            {
                DebugContext = debugCtx,
                ExceptionMessage = exception,
                InstructionLine = debugCtx.CurrentInstruction,
                StackTraces = stackFrames,
                Parser = _parser,
                ExceptionType = excType,
            };

            return new FluenceRuntimeException(exception, context);
        }
    }
}