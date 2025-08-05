namespace Fluence
{
    public class Program
    {
        public static void Main( string[] args )
        {
            string testSourceCode = File.ReadAllText($@"{Directory.GetCurrentDirectory()}\Full Lexer Test.fl");

            Console.WriteLine(testSourceCode);

            FluenceLexer lexer = new FluenceLexer(testSourceCode);

            while ( !lexer.HasReachedEnd )
            {
                lexer.GetNextToken();
            }
        }
    }
}