using Xunit.Abstractions;

namespace Fluence.Tests
{
    public class BasicLexerTests(ITestOutputHelper output)
    {
        private readonly ITestOutputHelper _output = output;

        [Fact]
        public void SkipWhiteSpace()
        {
            string source = "   \t  hello";
            var lexer = new FluenceLexer(source);

            lexer.SkipWhiteSpaceAndComments();

            // Should end on 'h'.
            Assert.Equal(6, lexer.CurrentPosition);
        }

        [Fact]
        public void SkipSingleLineComment()
        {
            string source = "# this is a comment\nworld";
            var lexer = new FluenceLexer(source);

            lexer.SkipWhiteSpaceAndComments();

            // Should end on \n.
            Assert.Equal(19, lexer.CurrentPosition);
        }

        [Fact]
        public void SkipMultiLineComment()
        {
            string source = "#* this is a \n multi-line comment *# after";
            var lexer = new FluenceLexer(source);

            lexer.SkipWhiteSpaceAndComments();
            _output.WriteLine(lexer.CharAtCurrentPosition.ToString());

            // Should end on 'a'.
            Assert.Equal(37, lexer.CurrentPosition);
        }

        [Fact]
        public void SkipWhiteSpaceAndComments()
        {
            string source = "  \t # comment \n #* another *# final_word";
            var lexer = new FluenceLexer(source);

            lexer.SkipWhiteSpaceAndComments();

            // Should end on '\n'.
            Assert.Equal(14, lexer.CurrentPosition);
        }

        [Fact]
        public void SkipWhiteSpaceMixedWithCode()
        {
            string source = "  \t # comment \n func input_int() => to_int(input()); #* another *# final_word";
            var lexer = new FluenceLexer(source);

            lexer.SkipWhiteSpaceAndComments();
            _output.WriteLine(lexer.CharAtCurrentPosition.ToString());

            // Should end on '\n'.
            Assert.Equal(14, lexer.CurrentPosition);
        }

        [Fact]
        public void SkipEmptyWhiteSpace()
        {
            // Arrange
            string source = "start";
            var lexer = new FluenceLexer(source);

            // Act
            lexer.SkipWhiteSpaceAndComments();

            // Assert
            // The position should not have moved.
            Assert.Equal(0, lexer.CurrentPosition);
        }
    }

    public class LexerLessThanTests(ITestOutputHelper output)
    {
        private readonly ITestOutputHelper _output = output;

        private static Token LexFirstToken(string source)
        {
            var lexer = new FluenceLexer(source);
            return lexer.ConsumeToken();
        }

        // --- 6-Character Operator Tests ---
        [Fact]
        public void TestCollectiveOrEqualOperator()
        {
            string source = "<||==|";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.COLLECTIVE_OR_EQUAL, token.Type);
        }

        [Fact]
        public void TestsCollectiveOrNotEqualOperator()
        {
            string source = "<||!=|";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.COLLECTIVE_OR_NOT_EQUAL, token.Type);
        }

        [Fact]
        public void TestsOrGuardChainOperator()
        {
            string source = "<||??|";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.OR_GUARD_CHAIN, token.Type);
        }

        // --- 4-Character Operator Tests ---
        [Fact]
        public void TestsCollectiveEqualOperator()
        {
            string source = "<==|";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.COLLECTIVE_EQUAL, token.Type);
        }

        [Fact]
        public void TestGuardChainOperator()
        {
            string source = "<??|";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.GUARD_CHAIN, token.Type);
        }

        // --- <n| and <n|? Family Tests ---
        [Fact]
        public void TestChainAssignNWithSingleDigit()
        {
            string source = "<1|";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.CHAIN_ASSIGN_N, token.Type);
        }

        [Fact]
        public void TestChainAssignNWithMultipleDigits()
        {
            string source = "<123| remaining code";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.CHAIN_ASSIGN_N, token.Type);
        }

        [Fact]
        public void TestOptionalAssignN()
        {
            string source = "<5|? code after";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.OPTIONAL_ASSIGN_N, token.Type);
            Assert.Equal("5", token.Text);
        }

        [Fact]
        public void TestLessThanOrEqualOperator()
        {
            string source = "<=";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.LESS_EQUAL, token.Type);
        }

        [Fact]
        public void TestRestAssignOperator()
        {
            string source = "<|";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.REST_ASSIGN, token.Type);
        }

        [Fact]
        public void TestSingleLessThanAsFallback()
        {
            string source = "< variable";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.LESS, token.Type);
        }

        [Fact]
        public void TestOperatorCorrectlyAtEndOfFile()
        {
            string source = "  <||!=|";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.COLLECTIVE_OR_NOT_EQUAL, token.Type);

            var lexer = new FluenceLexer(source);
            _ = lexer.ConsumeToken();
            Assert.True(lexer.HasReachedEnd);
        }

        [Fact]
        public void TestSingleLessThanAtEndOfFile()
        {
            string source = "<";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.LESS, token.Type);
        }
    }

    public class LexerPipeTests(ITestOutputHelper output)
    {
        private readonly ITestOutputHelper _output = output;

        private static Token LexFirstToken(string source)
        {
            var lexer = new FluenceLexer(source);
            return lexer.ConsumeToken();
        }

        [Fact]
        public void TestReducerPipeOperator()
        {
            string source = "|>>=";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.REDUCER_PIPE, token.Type);
        }

        [Fact]
        public void TestMapPipeOperator()
        {
            string source = "|>>";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.MAP_PIPE, token.Type);
        }

        // --- 2-Character Operator Tests ---
        [Fact]
        public void TestGuardPipeOperator()
        {
            string source = "|??";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.GUARD_PIPE, token.Type);
        }

        [Fact]
        public void TestScanPipeOperator()
        {
            string source = "|~>";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.SCAN_PIPE, token.Type);
        }

        [Fact]
        public void TestLogicalOrOperator()
        {
            string source = "||";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.OR, token.Type);
        }

        [Fact]
        public void TestPipeOperator()
        {
            string source = "|>";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.PIPE, token.Type);
        }

        [Fact]
        public void TestOptionalPipeOperator()
        {
            string source = "|?";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.OPTIONAL_PIPE, token.Type);
        }

        // --- Fallback and Edge Case Tests ---

        [Fact]
        public void TestSingleBitwiseOrAsFallback()
        {
            string source = "| variable";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.PIPE_CHAR, token.Type);
        }

        [Fact]
        public void TestLongestOperatorAtEndOfFile()
        {
            string source = "|>>=";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.REDUCER_PIPE, token.Type);

            var lexer = new FluenceLexer(source);
            _ = lexer.ConsumeToken();
            Assert.True(lexer.HasReachedEnd);
        }

        [Fact]
        public void TestSingleBitwiseOrAtEndOfFile()
        {
            string source = "|";
            var token = LexFirstToken(source);
            Assert.Equal(Token.TokenType.PIPE_CHAR, token.Type);
        }

        [Fact]
        public void DistinguishesFromNonOperatorCharacters()
        {
            var lexer = new FluenceLexer("|a");
            var token1 = lexer.ConsumeToken();

            Assert.Equal(Token.TokenType.PIPE_CHAR, token1.Type);
        }
    }
}