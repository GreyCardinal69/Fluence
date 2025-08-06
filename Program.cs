namespace Fluence
{
    public class Program
    {
        public static void Main(string[] args)
        {
            _ = File.ReadAllText($@"{Directory.GetCurrentDirectory()}\Full Lexer Test.fl");

            Console.WriteLine(CanLookAhead(7));

            return;
            string testSourceCode;
            Console.WriteLine(testSourceCode);

            FluenceLexer lexer = new FluenceLexer(testSourceCode);

            while (!lexer.HasReachedEnd)
            {
                _ = lexer.GetNextToken();
            }
        }

        private static int _currentPosition = 0;

        private static bool CanLookAhead(int numberOfChars = 1)
        {
            if (_currentPosition + numberOfChars > "<=====".Length)
            {
                return false;
            }
            return true;
        }

        private static bool StringHasOnlyOperatorChars(int to)
        {
            for (int i = _currentPosition; i < _currentPosition + to; i++)
            {
                if (!IsOperatorChar("<||!=|"[i])) return false;
            }
            return true;
        }

        private static bool IsOperatorChar(char c) =>
    c == '!' ||
    c == '%' ||
    c == '^' ||
    c == '&' ||
    c == '*' ||
    c == '-' ||
    c == '+' ||
    c == '=' ||
    c == '|' ||
    c == '\\' ||
    c == '<' ||
    c == '>' ||
    c == '~' ||
    c == '?';
    }
}