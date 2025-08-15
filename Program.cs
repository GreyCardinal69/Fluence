namespace Fluence
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string source = File.ReadAllText($@"{Directory.GetCurrentDirectory()}\Full Lexer Test.fl");

            FluenceLexer lexer = new(source);
            FluenceParser parser = new FluenceParser(lexer);

            try
            {
                parser.Parse();
                Console.WriteLine("\nParsing completed successfully.\n");

                parser.DumpSymbolTables();

                FluenceByteCode.DumpByteCodeInstructions(parser.CompiledCode);
            }
            catch (FluenceException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("A parsing error occurred:");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }
        }
    }
}