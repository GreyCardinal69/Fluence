using Xunit.Abstractions;
using static Fluence.Token;

namespace Fluence.LexerTests
{
    public class LexerSymbolAndKeywordTests(ITestOutputHelper output) : LexerTestBase(output)
    {
        [Theory]
        [InlineData("(", TokenType.L_PAREN)]
        [InlineData(")", TokenType.R_PAREN)]
        [InlineData("{", TokenType.L_BRACE)]
        [InlineData("}", TokenType.R_BRACE)]
        [InlineData("[", TokenType.L_BRACKET)]
        [InlineData("]", TokenType.R_BRACKET)]
        [InlineData(",", TokenType.COMMA)]
        [InlineData("+", TokenType.PLUS)]
        [InlineData("-", TokenType.MINUS)]
        [InlineData("*", TokenType.STAR)]
        [InlineData("/", TokenType.SLASH)]
        [InlineData("%", TokenType.PERCENT)]
        [InlineData("=", TokenType.EQUAL)]
        [InlineData("~>", TokenType.COMPOSITION_PIPE)]
        [InlineData("->", TokenType.THIN_ARROW)]
        [InlineData("=>", TokenType.ARROW)]
        [InlineData("==", TokenType.EQUAL_EQUAL)]
        [InlineData("!=", TokenType.BANG_EQUAL)]
        [InlineData("**", TokenType.EXPONENT)]
        [InlineData("++", TokenType.INCREMENT)]
        [InlineData("--", TokenType.DECREMENT)]
        [InlineData("..", TokenType.DOT_DOT)]
        [InlineData("func", TokenType.FUNC)]
        [InlineData("if", TokenType.IF)]
        [InlineData("else", TokenType.ELSE)]
        [InlineData("return", TokenType.RETURN)]
        [InlineData("true", TokenType.TRUE)]
        [InlineData("false", TokenType.FALSE)]
        [InlineData("nil", TokenType.NIL)]
        [InlineData("is", TokenType.IS)]
        [InlineData("not", TokenType.NOT)]
        [InlineData("struct", TokenType.STRUCT)]
        internal void TestSingleTokens(string source, TokenType expectedType)
        {
            var types = LexFirstToken(source);
            Assert.Equal(expectedType, types.Type);
        }

        [Fact]
        public void TestSemicolonEOL()
        {
            var lexer = new FluenceLexer(";");
            Assert.Equal(TokenType.EOL, lexer.ConsumeToken().Type);
        }

        [Fact]
        public void TestSequenceOfSymbolsAndKeywords()
        {
            string source = "func main() => { return nil; }";
            var expected = new List<TokenType> {
            TokenType.FUNC, TokenType.IDENTIFIER, TokenType.L_PAREN, TokenType.R_PAREN,
            TokenType.ARROW, TokenType.L_BRACE, TokenType.RETURN, TokenType.NIL,
            TokenType.EOL, TokenType.R_BRACE
        };
            var actual = LexAllTypes(source);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestSequenceOfRange()
        {
            string source = "func main() => { x = 5..10; }";
            var expected = new List<TokenType> {
            TokenType.FUNC, TokenType.IDENTIFIER, TokenType.L_PAREN, TokenType.R_PAREN,
            TokenType.ARROW, TokenType.L_BRACE, TokenType.IDENTIFIER, TokenType.EQUAL,
            TokenType.NUMBER, TokenType.DOT_DOT, TokenType.NUMBER, TokenType.EOL, TokenType.R_BRACE
        };
            var actual = LexAllTypes(source);
            Assert.Equal(expected, actual);
        }
    }
}