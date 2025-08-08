namespace Fluence
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var source = File.ReadAllText($@"{Directory.GetCurrentDirectory()}\Full Lexer Test.fl");

            FluenceLexer l = new(source);

            Console.WriteLine("--- Lexer Token Stream ---");
            Console.WriteLine();

            string header = string.Format("{0,-25} {1,-30} {2,-30}", "TYPE", "TEXT", "LITERAL");
            Console.WriteLine(header);
            Console.WriteLine(new string('-', header.Length));

            while (!l.HasReachedEnd)
            {
                var token = l.GetNextToken();

                if (token.Type == Token.TokenType.EOL && l.HasReachedEnd && string.IsNullOrWhiteSpace(token.Text))
                {
                    break;
                }

                var textToDisplay = (token.Text ?? "")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n");

                var literalToDisplay = token.Literal?.ToString() ?? "";

                Console.WriteLine("{0,-25} {1,-30} {2,-30}",
                    token.Type,
                    textToDisplay,
                    literalToDisplay);
            }

            Console.WriteLine();
            Console.WriteLine("--- End of Stream ---");

            FluenceLexer lexer = new(source);
            FluenceParser parser = new FluenceParser(lexer);

            parser.Parse();
        }
    }
}