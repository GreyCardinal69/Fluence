using static Fluence.FluenceByteCode;
using static Fluence.FluenceParser;

namespace Fluence
{
    public sealed class FluenceInterpreter
    {
        private ParseState _parseState;
        private List<InstructionLine> _byteCode;

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

        public FluenceInterpreter()
        {
        }

        public bool Compile(string source, bool partialCode = false)
        {
            ArgumentException.ThrowIfNullOrEmpty(source);

            try
            {
                FluenceLexer lexer = new FluenceLexer(source);
                FluenceParser parser = new FluenceParser(lexer);
                FluenceIntrinsics intrinsics = new FluenceIntrinsics(parser);
                intrinsics.Register();
                parser.Parse(partialCode);
#if DEBUG
                parser.DumpSymbolTables();
                DumpByteCodeInstructions(parser.CompiledCode);
#endif

                _byteCode = parser.CompiledCode;
                _parseState = parser.CurrentParseState;
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
                var vm = new FluenceVirtualMachine(_byteCode, _parseState, OnOutput, OnOutputLine, OnInput);
                vm.RunFor(TimeSpan.MaxValue);
#if DEBUG
                vm.DumpPerformanceProfile();
#endif
            }
            catch (FluenceRuntimeException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Runtime Error:");
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = ConsoleColor.White;
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
                var vm = new FluenceVirtualMachine(_byteCode, _parseState, OnOutput, OnOutputLine, OnInput);
                vm.RunFor(duration);
#if DEBUG
                vm.DumpPerformanceProfile();
#endif
            }
            catch (FluenceRuntimeException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Runtime Error:");
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public void Reset()
        {

        }
        public void Stop()
        {

        }

        public void Restart()
        {

        }

        private void ConstructAndThrowError(string error)
        {

        }
    }
}