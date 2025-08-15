namespace Fluence
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string source = File.ReadAllText($@"{Directory.GetCurrentDirectory()}\Full Lexer Test.fl");

            FluenceLexer l = new(source);

            Console.WriteLine("--- Lexer Token Stream ---");
            Console.WriteLine();

            string header = string.Format("{0,-35} {1,-40} {2,-30}", "TYPE", "TEXT", "LITERAL");
            Console.WriteLine(header);
            Console.WriteLine(new string('-', header.Length));

            while (!l.HasReachedEnd)
            {
                Token token = l.ConsumeToken();

                if (token.Type == Token.TokenType.EOL && l.HasReachedEnd && string.IsNullOrWhiteSpace(token.Text))
                {
                    break;
                }

                string textToDisplay = (token.Text ?? "")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n");

                string literalToDisplay = token.Literal?.ToString() ?? "";
                literalToDisplay = (literalToDisplay == "\r\n" || literalToDisplay == "\n") ? "NewLine" : literalToDisplay;

                Console.WriteLine("{0,-35} {1,-40} {2,-30}",
                    token.Type,
                    textToDisplay,
                    literalToDisplay);
            }

            Console.WriteLine();
            Console.WriteLine("--- End of Stream ---");

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