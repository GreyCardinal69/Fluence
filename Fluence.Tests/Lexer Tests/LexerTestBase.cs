using Xunit.Abstractions;

namespace Fluence.LexerTests
{
    public abstract class LexerTestBase(ITestOutputHelper output)
    {
        protected readonly ITestOutputHelper _output = output;

        internal static List<Token.TokenType> LexAllTypes(string source)
        {
            FluenceLexer lexer = new FluenceLexer(source);
            List<Token.TokenType> types = new List<Token.TokenType>();
            Token token;
            do
            {
                token = lexer.ConsumeToken();
                types.Add(token.Type);
            } while (!lexer.HasReachedEnd);
            return types;
        }

        internal static Token LexFirstToken(string source)
        {
            FluenceLexer lexer = new FluenceLexer(source);
            return lexer.ConsumeToken();
        }
    }
}