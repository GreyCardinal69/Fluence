using Fluence;
using Newtonsoft.Json.Linq;
using Xunit.Abstractions;

public class LexerTests
{
    private readonly ITestOutputHelper _output;

    public LexerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SkipWhiteSpace_Should_Handle_Spaces_And_Tabs()
    {
        string source = "   \t  hello";
        var lexer = new FluenceLexer(source);

        lexer.SkipWhiteSpaceAndComments();
        _output.WriteLine(lexer.CharAtCurrentPosition.ToString());

        // Should end on 'h'.
        Assert.Equal(6, lexer.CurrentPosition);
    }

    [Fact]
    public void SkipWhiteSpace_Should_Handle_SingleLine_Comment()
    {
        string source = "# this is a comment\nworld";
        var lexer = new FluenceLexer(source);

        lexer.SkipWhiteSpaceAndComments();

        // Should end on \n.
        Assert.Equal(19, lexer.CurrentPosition);
    }

    [Fact]
    public void SkipWhiteSpace_Should_Handle_MultiLine_Comment()
    {
        string source = "#* this is a \n multi-line comment *# after";
        var lexer = new FluenceLexer(source);

        lexer.SkipWhiteSpaceAndComments();
        _output.WriteLine(lexer.CharAtCurrentPosition.ToString());

        // Should end on 'a'.
        Assert.Equal(37, lexer.CurrentPosition);
    }

    [Fact]
    public void SkipWhiteSpace_Should_Handle_Mixed_Whitespace_And_Comments()
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
    public void SkipWhiteSpace_Should_Do_Nothing_If_No_Whitespace()
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

public class LexerLessThanTests
{
    private readonly ITestOutputHelper _output;

    public LexerLessThanTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private Token LexFirstToken(string source)
    {
        var lexer = new FluenceLexer(source);
        return lexer.GetNextToken();
    }

    // --- 6-Character Operator Tests ---
    [Fact]
    public void Scans_CollectiveOrEqual_Operator()
    {
        string source = "<||==|";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.COLLECTIVE_OR_EQUAL, token.Type);
    }

    [Fact]
    public void Scans_CollectiveOrNotEqual_Operator()
    {
        string source = "<||!=|";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.COLLECTIVE_OR_NOT_EQUAL, token.Type);
    }

    [Fact]
    public void Scans_OrGuardChain_Operator()
    {
        string source = "<||??|";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.OR_GUARD_CHAIN, token.Type);
    }

    // --- 4-Character Operator Tests ---
    [Fact]
    public void Scans_CollectiveEqual_Operator()
    {
        string source = "<==|";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.COLLECTIVE_EQUAL, token.Type);
    }

    [Fact]
    public void Scans_GuardChain_Operator()
    {
        string source = "<??|";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.GUARD_CHAIN, token.Type);
    }

    // --- <n| and <n|? Family Tests ---
    [Fact]
    public void Scans_ChainAssignN_With_SingleDigit()
    {
        string source = "<1|";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.CHAIN_ASSIGN_N, token.Type);
    }

    [Fact]
    public void Scans_ChainAssignN_With_MultipleDigits()
    {
        string source = "<123| remaining code";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.CHAIN_ASSIGN_N, token.Type);
    }

    [Fact]
    public void Scans_OptionalAssignN()
    {
        string source = "<5|? code after";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.OPTIONAL_ASSIGN_N, token.Type);
    }
    
    [Fact]
    public void Scans_LessThanOrEqual_Operator()
    {
        string source = "<=";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.LESS_EQUAL, token.Type);
    }

    [Fact]
    public void Scans_RestAssign_Operator()
    {
        string source = "<|";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.REST_ASSIGN, token.Type);
    }
    
    [Fact]
    public void Scans_Single_LessThan_As_Fallback()
    {
        string source = "< variable";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.LESS, token.Type);
    }

    [Fact]
    public void Scans_Operator_Correctly_At_EndOfFile()
    {
        string source = "  <||!=|";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.COLLECTIVE_OR_NOT_EQUAL, token.Type);

        var lexer = new FluenceLexer(source);
        lexer.GetNextToken();
        Assert.True(lexer.HasReachedEnd);
    }

    [Fact]
    public void Scans_Single_LessThan_At_EndOfFile()
    {
        string source = "<";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.LESS, token.Type);
    }
}

public class LexerPipeTests
{
    private readonly ITestOutputHelper _output;

    public LexerPipeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private Token LexFirstToken(string source)
    {
        var lexer = new FluenceLexer(source);
        return lexer.GetNextToken();
    }

    // --- 4-Character Operator Test ---
    [Fact]
    public void Scans_ReducerPipe_Operator()
    {
        string source = "|>>=";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.REDUCER_PIPE, token.Type);
    }

    // --- 3-Character Operator Test ---
    [Fact]
    public void Scans_MapPipe_Operator()
    {
        string source = "|>>";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.MAP_PIPE, token.Type);
    }

    // --- 2-Character Operator Tests ---
    [Fact]
    public void Scans_GuardPipe_Operator()
    {
        string source = "|??";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.GUARD_PIPE, token.Type);
    }

    [Fact]
    public void Scans_ScanPipe_Operator()
    {
        string source = "|~>";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.SCAN_PIPE, token.Type);
    }

    [Fact]
    public void Scans_LogicalOr_Operator()
    {
        string source = "||";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.OR, token.Type);
    }

    [Fact]
    public void Scans_Pipe_Operator()
    {
        string source = "|>";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.PIPE, token.Type);
    }

    [Fact]
    public void Scans_OptionalPipe_Operator()
    {
        string source = "|?";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.OPTIONAL_PIPE, token.Type);
    }

    // --- Fallback and Edge Case Tests ---

    [Fact]
    public void Scans_Single_BitwiseOr_As_Fallback()
    {
        string source = "| variable";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.PIPE_CHAR, token.Type);
    }

    [Fact]
    public void Scans_Longest_Operator_Correctly_At_EndOfFile()
    {
        string source = "|>>=";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.REDUCER_PIPE, token.Type);

        var lexer = new FluenceLexer(source);
        lexer.GetNextToken();
        Assert.True(lexer.HasReachedEnd);
    }

    [Fact]
    public void Scans_Single_BitwiseOr_At_EndOfFile()
    {
        string source = "|";
        var token = LexFirstToken(source);
        Assert.Equal(Token.TokenType.PIPE_CHAR, token.Type);
    }

    [Fact]
    public void Distinguishes_From_Non_Operator_Characters()
    {
        var lexer = new FluenceLexer("|a");
        var token1 = lexer.GetNextToken();

        Assert.Equal(Token.TokenType.PIPE_CHAR, token1.Type);
    }
}