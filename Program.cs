using Fluence;

namespace Fluence
{
    public class Program
    {
        public static void Main( string[] args )
        {
            string testSourceCode = File.ReadAllText($@"{Directory.GetCurrentDirectory()}\Full Lexer Test.fl");

            Console.WriteLine(CanLookAhead(7));

            return;
            Console.WriteLine(testSourceCode);

            FluenceLexer lexer = new FluenceLexer(testSourceCode);

            while ( !lexer.HasReachedEnd )
            {
                lexer.GetNextToken();
            }
        }

        static int _currentPosition = 0;

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