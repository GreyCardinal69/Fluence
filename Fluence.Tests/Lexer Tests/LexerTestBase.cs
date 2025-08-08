using Xunit.Abstractions;

namespace Fluence.Tests
{
    public abstract class LexerTestBase(ITestOutputHelper output)
    {
        protected readonly ITestOutputHelper _output = output;

        internal static List<Token.TokenType> LexAllTypes(string source)
        {
            var lexer = new FluenceLexer(source);
            var types = new List<Token.TokenType>();
            Token token;
            do
            {
                token = lexer.ConsumeToken();
                types.Add(token.Type);
            } while (token.Type != Token.TokenType.EOL);
            return types;
        }

        internal static Token LexFirstToken(string source)
        {
            var lexer = new FluenceLexer(source);
            return lexer.ConsumeToken();
        }
    }
}