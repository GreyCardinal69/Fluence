using static Fluence.FluenceByteCode;
using static Fluence.FluenceParser;

namespace Fluence
{
    public sealed class FluenceInterpreter
    {
        private ParseState _parseState;
        private List<InstructionLine> _byteCode;

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
                System.Console.WriteLine("Compilation Error:");
                System.Console.WriteLine(ex);
                return false;
            }
        }

        public void Run()
        {
            if (_byteCode == null)
            {
                throw new InvalidOperationException("Code must be compiled successfully before it can be run.");
            }

            try
            {
                var vm = new FluenceVirtualMachine(_byteCode, _parseState);
                vm.Run();
            }
            catch (FluenceRuntimeException ex)
            {
                Console.WriteLine("Runtime Error:");
                Console.WriteLine(ex.Message);
            }
        }
    }
}