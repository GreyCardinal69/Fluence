using static Fluence.FluenceByteCode;
using static Fluence.FluenceParser;

namespace Fluence
{
    public sealed class FluenceInterpreter
    {
        private ParseState _parseState;
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
        /// Gets the current state of the virtual machine.
        /// </summary>
        public FluenceVMState State => _vm?.State ?? FluenceVMState.NotStarted;

        public bool IsDone => _vm.State == FluenceVMState.Finished;

        public FluenceInterpreter()
        {
        }

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
                Console.WriteLine("Compilation Error:");
                Console.WriteLine(ex);
                return false;
            }
        }

        public void RunUntilDone()
        {
            if (_byteCode == null)
            {
                throw new InvalidOperationException("Code must be compiled successfully before it can be run.");
            }

            try
            {
                if (_vm == null|| _vm.State == FluenceVMState.NotStarted)
                {
                    _vm = new FluenceVirtualMachine(_byteCode, _parseState, OnOutput, OnOutputLine, OnInput);
                }

                if (_vm.State == FluenceVMState.Finished || _vm.State == FluenceVMState.Error)
                {
                    _vm = new FluenceVirtualMachine(_byteCode, _parseState, OnOutput, OnOutputLine, OnInput);
                }

                _vm.RunFor(TimeSpan.MaxValue);
#if DEBUG
                _vm.DumpPerformanceProfile();
#endif
            }
            catch (FluenceRuntimeException ex)
            {
                ConstructAndThrowException(ex);
                _vm.Stop();
            }
        }

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

                _vm.RunFor(duration);
#if DEBUG
                _vm.DumpPerformanceProfile();
#endif
            }
            catch (FluenceRuntimeException ex)
            {
                ConstructAndThrowException(ex);
                _vm.Stop();
            }
        }

        /// <summary>
        /// Resets the Virtual Machine, the parsed bytecode.
        /// </summary>
        public void Reset()
        {
            _vm = null!;
            _parseState = null!;
            _byteCode = null!;
        }

        /// <summary>
        /// Requests the running script to pause execution at the next instruction.
        /// This method is non-blocking.
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
                            RuntimeNumberType.Float => (float)val.FloatValue,
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

        private static void ConstructAndThrowException(FluenceException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Runtime Error:");
            Console.WriteLine(ex);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}