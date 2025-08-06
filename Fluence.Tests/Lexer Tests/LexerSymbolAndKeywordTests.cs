using Xunit.Abstractions;

namespace Fluence.Tests
{
    public class LexerSymbolAndKeywordTests(ITestOutputHelper output) : LexerTestBase(output)
    {
        [Theory]
        [InlineData("(", Token.TokenType.L_PAREN)]
        [InlineData(")", Token.TokenType.R_PAREN)]
        [InlineData("{", Token.TokenType.L_BRACE)]
        [InlineData("}", Token.TokenType.R_BRACE)]
        [InlineData("[", Token.TokenType.L_BRACKET)]
        [InlineData("]", Token.TokenType.R_BRACKET)]
        [InlineData(",", Token.TokenType.COMMA)]
        [InlineData("+", Token.TokenType.PLUS)]
        [InlineData("-", Token.TokenType.MINUS)]
        [InlineData("*", Token.TokenType.STAR)]
        [InlineData("/", Token.TokenType.SLASH)]
        [InlineData("%", Token.TokenType.PERCENT)]
        [InlineData("=", Token.TokenType.EQUAL)]
        [InlineData("~>", Token.TokenType.COMPOSITION_PIPE)]
        [InlineData("->", Token.TokenType.THIN_ARROW)]
        [InlineData("=>", Token.TokenType.ARROW)]
        [InlineData("==", Token.TokenType.EQUAL_EQUAL)]
        [InlineData("!=", Token.TokenType.BANG_EQUAL)]
        [InlineData("**", Token.TokenType.EXPONENT)]
        [InlineData("++", Token.TokenType.INCREMENT)]
        [InlineData("--", Token.TokenType.DECREMENT)]
        [InlineData("..", Token.TokenType.DOT_DOT)]
        [InlineData("func", Token.TokenType.FUNC)]
        [InlineData("if", Token.TokenType.IF)]
        [InlineData("else", Token.TokenType.ELSE)]
        [InlineData("return", Token.TokenType.RETURN)]
        [InlineData("true", Token.TokenType.TRUE)]
        [InlineData("false", Token.TokenType.FALSE)]
        [InlineData("nil", Token.TokenType.NIL)]
        [InlineData("is", Token.TokenType.IS)]
        [InlineData("not", Token.TokenType.NOT)]
        [InlineData("struct", Token.TokenType.STRUCT)]
        internal void ScanSingleTokens(string source, Token.TokenType expectedType)
        {
            var types = LexAllTypes(source);
            Assert.Equal(expectedType, types[0]);
            Assert.Equal(Token.TokenType.EOL, types[1]);
        }

        [Fact]
        public void ScanSemicolonEOL()
        {
            var lexer = new FluenceLexer(";");
            Assert.Equal(Token.TokenType.EOL, lexer.GetNextToken().Type);
        }

        [Fact]
        public void ScanSequenceOfSymbolsAnd_Keywords()
        {
            string source = "func main() => { return nil; }";
            var expected = new List<Token.TokenType> {
            Token.TokenType.FUNC, Token.TokenType.IDENTIFIER, Token.TokenType.L_PAREN, Token.TokenType.R_PAREN,
            Token.TokenType.ARROW, Token.TokenType.L_BRACE, Token.TokenType.RETURN, Token.TokenType.NIL,
            Token.TokenType.EOL
        };
            var actual = LexAllTypes(source);
            Assert.Equal(expected, actual);
        }
    }
}