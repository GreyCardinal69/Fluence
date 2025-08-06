using Xunit.Abstractions;

namespace Fluence.Tests
{
    public class LexerLiteralTests(ITestOutputHelper output) : LexerTestBase(output)
    {
        [Fact]
        public void ScanIdentifier()
        {
            Token token = LexFirstToken("my_variable");
            Assert.Equal(Token.TokenType.IDENTIFIER, token.Type);
            Assert.Equal("my_variable", token.Text);
        }

        [Fact]
        public void ScanIdentifierWithUnderScoreAndNumbers()
        {
            Token token = LexFirstToken("my_variable_123");
            Assert.Equal(Token.TokenType.IDENTIFIER, token.Type);
            Assert.Equal("my_variable_123", token.Text);
        }

        [Fact]
        public void ScanPlaceholderAsSeparateToken()
        {
            var types = LexAllTypes("x |> _");
            Assert.Equal(Token.TokenType.IDENTIFIER, types[0]);
            Assert.Equal(Token.TokenType.PIPE, types[1]);
            Assert.Equal(Token.TokenType.UNDERSCORE, types[2]);
        }

        [Theory]
        [InlineData("\"hello world\"", "hello world")]
        [InlineData("\"with \\\"escapes\\\"\"", "with \\\"escapes\\\"")]
        public void ScanStringLiterals(string source, string expectedValue)
        {
            Token token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.STRING, token.Type);
            Assert.Equal(expectedValue, token.Literal);
        }

        [Fact]
        public void ScanFStringLiteral()
        {
            Token token = LexFirstToken("f\"value is {x}\"");
            Assert.Equal(Token.TokenType.F_STRING, token.Type);
        }

        [Fact]
        public void ScanFloats()
        {
            Token token = LexFirstToken("10.5f");
            Assert.Equal(Token.TokenType.NUMBER, token.Type);
            Assert.Equal("10.5f", token.Literal);

            Token token2 = LexFirstToken("10f");
            Assert.Equal(Token.TokenType.NUMBER, token2.Type);
            Assert.Equal("10f", token2.Literal);
        }

        [Fact]
        public void ScanPartialFraction()
        {
            Token token = LexFirstToken(".5");
            Assert.Equal(Token.TokenType.NUMBER, token.Type);
            Assert.Equal("0.5", token.Literal);
        }

        [Fact]
        public void ScanNumberLiterals()
        {
            FluenceLexer lexer = new FluenceLexer("123 ; 3.14 ; 1.23E2");

            Token nextToken = lexer.GetNextToken();
            Assert.Equal("123", nextToken.Text);
            Assert.Equal(Token.TokenType.NUMBER, nextToken.Type);

            nextToken = lexer.GetNextToken();
            Assert.Equal(";", nextToken.Text);
            Assert.Equal(Token.TokenType.EOL, nextToken.Type);

            nextToken = lexer.GetNextToken();
            Assert.Equal("3.14", nextToken.Text);
            Assert.Equal(Token.TokenType.NUMBER, nextToken.Type);

            nextToken = lexer.GetNextToken();
            Assert.Equal(";", nextToken.Text);
            Assert.Equal(Token.TokenType.EOL, nextToken.Type);

            nextToken = lexer.GetNextToken();
            Assert.Equal("1.23E2", nextToken.Text);
            Assert.Equal(Token.TokenType.NUMBER, nextToken.Type);
        }
    }
}