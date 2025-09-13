using static Fluence.FluenceByteCode;
using static Fluence.FluenceParser;

namespace Fluence
{
    /// <summary>
    /// Provides the main public API for compiling and executing Fluence scripts.
    /// This class is the primary entry point for embedding the Fluence language into a host application.
    /// It manages the lifecycle of the parser, optimizer, and virtual machine.
    /// </summary>
    public sealed class FluenceInterpreter
    {
        private ParseState _parseState;
        private FluenceIntrinsics _intrinsicsInstance;
        private List<InstructionLine> _byteCode;
        private FluenceVirtualMachine _vm;

        /// <summary>
        /// Defines the signature for a method that can receive output text from the Fluence VM.
        /// </summary>
        /// <param name="text">The text to be written.</param>
        public delegate void TextOutputMethod(string text);

        /// <summary>
        /// Defines the signature for a method that can provide input text to the Fluence VM.
        /// </summary>
        /// <returns>A line of text read from the input source.</returns>
        public delegate string TextInputMethod();

        /// <summary>
        /// Gets or sets the method used by the 'print' family of functions to write text.
        /// Defaults to Console.WriteLine.
        /// </summary>
        public TextOutputMethod OnOutputLine { get; set; } = Console.WriteLine;

        /// <summary>
        /// Gets or sets the method used by the 'print' family of functions for non-newline output.
        /// Defaults to Console.Write.
        /// </summary>
        public TextOutputMethod OnOutput { get; set; } = Console.Write;

        /// <summary>
        /// Gets or sets the method used by the 'input()' function to read text.
        /// Defaults to Console.ReadLine.
        /// </summary>
        public TextInputMethod OnInput { get; set; } = Console.ReadLine!;

        /// <summary>
        /// A collection of the standard library names that are permitted to be loaded by a script.
        /// If this set is empty, all standard libraries are allowed. If it is populated, only the
        /// libraries whose names are in this set can be imported via the 'use' statement.
        /// This acts as a security whitelist for sandboxing script execution.
        /// </summary>
        public HashSet<string> AllowedLibraries { get; private set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets the output method to report errors and exceptions.
        /// </summary>
        public TextOutputMethod OnErrorOutput { get; set; } = Console.WriteLine;

        /// <summary>
        /// Gets the current execution state of the virtual machine.
        /// </summary>
        public FluenceVMState State => _vm?.State ?? FluenceVMState.NotStarted;

        /// <summary>
        /// Gets a value indicating whether the script has finished execution.
        /// </summary>
        public bool IsDone => _vm.State == FluenceVMState.Finished;

        /// <summary>
        /// Initializes a new instance of the <see cref="FluenceInterpreter"/>.
        /// </summary>
        public FluenceInterpreter()
        {
        }

        /// <summary>
        /// Adds one or more standard library names to the whitelist of allowed libraries.
        /// </summary>
        /// <param name="libs">A collection of library names to allow (e.g., "FluenceIO", "FluenceMath").</param>
        public void AddAllowedIntrinsicLibraries(IEnumerable<string> libs)
        {
            foreach (var lib in libs)
            {
                AllowedLibraries.Add(lib);
            }
        }

        /// <summary>
        /// Removes one or more standard library names from the whitelist of allowed libraries.
        /// </summary>
        /// <param name="libs">A collection of library names to disallow.</param>
        public void RemoveAllowedIntrinsicLibraries(IEnumerable<string> libs)
        {
            foreach (var lib in libs)
            {
                AllowedLibraries.Remove(lib);
            }
        }

        /// <summary>
        /// Clears the whitelist of allowed intrinsic libraries, effectively allowing all standard libraries to be used.
        /// </summary>
        public void ClearAllowedIntrinsicLibraries() => AllowedLibraries.Clear();

        /// <summary>
        /// Compiles a Fluence source code script into executable bytecode.
        /// This must be called before any of the Run methods.
        /// </summary>
        /// <param name="sourceCode">The Fluence script to compile.</param>
        /// <param name="partialCode">Whether to allow compilation of partial code, that is code without functions, or Main entry point.</param>
        /// <returns>True if compilation was successful, false otherwise.</returns>
        public bool Compile(string sourceCode, bool partialCode = false)
        {
            ArgumentException.ThrowIfNullOrEmpty(sourceCode);

            try
            {
                FluenceLexer lexer = new FluenceLexer(sourceCode);
                FluenceParser parser = new FluenceParser(lexer, OnOutputLine, OnOutput, OnInput);
                _intrinsicsInstance = parser.Intrinsics;
                parser.Parse(partialCode);
#if DEBUG
                parser.DumpSymbolTables();
                DumpByteCodeInstructions(parser.CompiledCode);
#endif

                _byteCode = parser.CompiledCode;
                _parseState = parser.CurrentParseState;
                _vm = null!;
                return true;
            }
            catch (FluenceException ex)
            {
                ConstructAndThrowException(ex);
                return false;
            }
        }

        /// <summary>
        /// Compiles a Fluence project from a directory with .fl code scripts into executable bytecode.
        /// This must be called before any of the Run methods.
        /// </summary>
        /// <param name="sourceCode">The Fluence script to compile.</param>
        /// <param name="partialCode">Whether to allow compilation of partial code, that is code without functions, or Main entry point.</param>
        /// <returns>True if compilation was successful, false otherwise.</returns>
        public bool CompileProject(string rootDir, bool partialCode = false)
        {
            ArgumentException.ThrowIfNullOrEmpty(rootDir);

            try
            {
                FluenceParser parser = new FluenceParser(rootDir, OnOutputLine, OnOutput, OnInput);
                _intrinsicsInstance = parser.Intrinsics;
                parser.Parse(partialCode);
#if DEBUG
                parser.DumpSymbolTables();
                DumpByteCodeInstructions(parser.CompiledCode);
#endif

                _byteCode = parser.CompiledCode;
                _parseState = parser.CurrentParseState;
                _vm = null!;
                return true;
            }
            catch (FluenceException ex)
            {
                ConstructAndThrowException(ex);
                return false;
            }
        }

        /// <summary>
        /// Runs the compiled script to completion in a single blocking call.
        /// If the script was previously paused, execution will resume and run to completion.
        /// If the script was finished, it will be reset and run again from the beginning.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if no code has been compiled.</exception>
        public void RunUntilDone()
        {
            RunFor(TimeSpan.MaxValue);
        }

        /// <summary>
        /// Runs or resumes the compiled script for a specified maximum duration.
        /// If the duration is reached before the script finishes, the VM state will be 'Paused'.
        /// </summary>
        /// <param name="duration">The maximum time to run before pausing.</param>
        /// <exception cref="InvalidOperationException">Thrown if no code has been compiled.</exception>
        public void RunFor(TimeSpan duration)
        {
            if (_byteCode == null)
            {
                throw new InvalidOperationException("Code must be compiled successfully before it can be run.");
            }

            try
            {
                if (_vm == null || _vm.State == FluenceVMState.NotStarted)
                {
                    _vm = new FluenceVirtualMachine(_byteCode, _parseState, OnOutput, OnOutputLine, OnInput);
                }

                if (_vm.State == FluenceVMState.Finished || _vm.State == FluenceVMState.Error)
                {
                    _vm = new FluenceVirtualMachine(_byteCode, _parseState, OnOutput, OnOutputLine, OnInput);
                }
                _vm.SetAllowedIntrinsicLibraries(AllowedLibraries);
                _vm.RunFor(duration);
#if DEBUG
                _vm.DumpPerformanceProfile();
#endif
            }
            catch (FluenceException ex)
            {
                ConstructAndThrowException(ex);
                _vm.Stop();
            }
        }

        /// <summary>
        /// Resets the interpreter, clearing the compiled bytecode and the virtual machine instance.
        /// The interpreter must be re-initialized with a call to <see cref="Compile(string, bool)"/> before it can be run again.
        /// </summary>
        public void Reset()
        {
            _vm = null!;
            _parseState = null!;
            _byteCode = null!;
        }

        /// <summary>
        /// Signals the running script to pause execution at the next available opportunity (before the next instruction).
        /// This method returns immediately and does not block.
        /// </summary>
        public void Stop() => _vm.Stop();

        /// <summary>
        /// Gets the value of a global variable from the VM.
        /// This is how the script can pass data back to the host application.
        /// Returns Null if no such variable is found.
        /// </summary>
        /// <param name="name">The name of the global variable.</param>
        /// <returns>The value of the variable, or null if not found.</returns>
        public object? GetGlobal(string name)
        {
            if (_vm.TryGetGlobalVariable(name, out RuntimeValue val))
            {
                switch (val.Type)
                {
                    case RuntimeValueType.Nil:
                        return null;
                    case RuntimeValueType.Boolean:
                        return Convert.ToBoolean(val.IntValue);
                    case RuntimeValueType.Number:
                        return val.NumberType switch
                        {
                            RuntimeNumberType.Long => Convert.ToInt64(val.LongValue),
                            RuntimeNumberType.Int => Convert.ToInt32(val.IntValue),
                            RuntimeNumberType.Float => val.FloatValue,
                            RuntimeNumberType.Double => Convert.ToDouble(val.DoubleValue),
                            _ => throw new NotImplementedException(),
                        };
                    case RuntimeValueType.Object:
                        return val.ObjectReference;
                }
            }

            return null;
        }

        /// <summary>
        /// Sets a global variable in the VM's global scope.
        /// This is how the host application can pass data into the script.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The value to set (can be a primitive like int, double, string, or bool).</param>
        public void SetGlobal(string name, object value)
        {
            _vm.SetGlobal(name, value);
        }

        /// <summary>
        /// Handles the formatting and display of runtime exceptions.
        /// </summary>
        private void ConstructAndThrowException(FluenceException ex)
        {
            OnErrorOutput("Error:");
            OnErrorOutput(ex.ToString());
        }
    }
}