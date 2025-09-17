using Xunit.Abstractions;
using static Fluence.Token;

namespace Fluence.LexerTests
{
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
            Assert.Equal(TokenType.COLLECTIVE_OR_EQUAL, token.Type);
        }

        [Fact]
        public void TestsCollectiveOrNotEqualOperator()
        {
            string source = "<||!=|";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.COLLECTIVE_OR_NOT_EQUAL, token.Type);
        }

        [Fact]
        public void TestsOrGuardChainOperator()
        {
            string source = "<||??|";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.OR_GUARD_CHAIN, token.Type);
        }

        // --- 4-Character Operator Tests ---
        [Fact]
        public void TestsCollectiveEqualOperator()
        {
            string source = "<==|";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.COLLECTIVE_EQUAL, token.Type);
        }

        [Fact]
        public void TestGuardChainOperator()
        {
            string source = "<??|";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.GUARD_CHAIN, token.Type);
        }

        // --- <n| and <n|? Family Tests ---
        [Fact]
        public void TestChainAssignNWithSingleDigit()
        {
            string source = "<1|";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.CHAIN_ASSIGN_N, token.Type);
        }

        [Fact]
        public void TestChainAssignNWithMultipleDigits()
        {
            string source = "<123| remaining code";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.CHAIN_ASSIGN_N, token.Type);
        }

        [Fact]
        public void TestOptionalAssignN()
        {
            string source = "<5?| code after";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.OPTIONAL_ASSIGN_N, token.Type);
            Assert.Equal("5", token.Literal);
        }

        [Fact]
        public void TestLessThanOrEqualOperator()
        {
            string source = "<=";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.LESS_EQUAL, token.Type);
        }

        [Fact]
        public void TestRestAssignOperator()
        {
            string source = "<|";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.REST_ASSIGN, token.Type);
        }

        [Fact]
        public void TestSingleLessThanAsFallback()
        {
            string source = "< variable";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.LESS, token.Type);
        }

        [Fact]
        public void TestOperatorCorrectlyAtEndOfFile()
        {
            string source = "  <||!=|";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.COLLECTIVE_OR_NOT_EQUAL, token.Type);

            var lexer = new FluenceLexer(source);
            _ = lexer.ConsumeToken();
            Assert.True(lexer.HasReachedEnd);
        }

        [Fact]
        public void TestSingleLessThanAtEndOfFile()
        {
            string source = "<";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.LESS, token.Type);
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
            Assert.Equal(TokenType.REDUCER_PIPE, token.Type);
        }

        [Fact]
        public void TestMapPipeOperator()
        {
            string source = "|>>";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.MAP_PIPE, token.Type);
        }

        // --- 2-Character Operator Tests ---
        [Fact]
        public void TestGuardPipeOperator()
        {
            string source = "|??";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.GUARD_PIPE, token.Type);
        }

        [Fact]
        public void TestScanPipeOperator()
        {
            string source = "|~>";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.SCAN_PIPE, token.Type);
        }

        [Fact]
        public void TestLogicalOrOperator()
        {
            string source = "||";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.OR, token.Type);
        }

        [Fact]
        public void TestPipeOperator()
        {
            string source = "|>";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.PIPE, token.Type);
        }

        [Fact]
        public void TestOptionalPipeOperator()
        {
            string source = "|?";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.OPTIONAL_PIPE, token.Type);
        }

        // --- Fallback and Edge Case Tests ---

        [Fact]
        public void TestSingleBitwiseOrAsFallback()
        {
            string source = "| variable";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.PIPE_CHAR, token.Type);
        }

        [Fact]
        public void TestLongestOperatorAtEndOfFile()
        {
            string source = "|>>=";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.REDUCER_PIPE, token.Type);

            var lexer = new FluenceLexer(source);
            _ = lexer.ConsumeToken();
            Assert.True(lexer.HasReachedEnd);
        }

        [Fact]
        public void TestSingleBitwiseOrAtEndOfFile()
        {
            string source = "|";
            var token = LexFirstToken(source);
            Assert.Equal(TokenType.PIPE_CHAR, token.Type);
        }

        [Fact]
        public void DistinguishesFromNonOperatorCharacters()
        {
            var lexer = new FluenceLexer("|a");
            var token1 = lexer.ConsumeToken();

            Assert.Equal(TokenType.PIPE_CHAR, token1.Type);
        }
    }
}