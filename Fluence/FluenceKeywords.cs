namespace Fluence
{
    internal static class FluenceKeywords
    {
        private static readonly string[] _keyWords =
        {
            "break",
            "continue",
            "if",
            "else",
            "for",
            "while",
            "loop",
            "in",
            "func",
            "nil",
            "return",
            "true",
            "false",
            "is",
            "not",
            "space",
            "use",
            "type",
            "struct",
            "enum",
            "match",
            "self",
            "rest"
        };

        internal static bool IsAKeyword(string key) => _keyWords.Contains(key);

        internal static Token.TokenType GetTokenTypeFromKeyword(string word) => word switch
        {
            "break" => Token.TokenType.BREAK,
            "continue" => Token.TokenType.CONTINUE,
            "if" => Token.TokenType.IF,
            "else" => Token.TokenType.ELSE,
            "for" => Token.TokenType.FOR,
            "while" => Token.TokenType.WHILE,
            "loop" => Token.TokenType.LOOP,
            "in" => Token.TokenType.IN,
            "func" => Token.TokenType.FUNC,
            "nil" => Token.TokenType.NIL,
            "return" => Token.TokenType.RETURN,
            "true" => Token.TokenType.TRUE,
            "false" => Token.TokenType.FALSE,
            "is" => Token.TokenType.IS,
            "not" => Token.TokenType.NOT,
            "space" => Token.TokenType.SPACE,
            "use" => Token.TokenType.USE,
            "type" => Token.TokenType.TYPE,
            "struct" => Token.TokenType.STRUCT,
            "enum" => Token.TokenType.ENUM,
            "match" => Token.TokenType.MATCH,
            "self" => Token.TokenType.SELF,
            "rest" => Token.TokenType.REST,
            _ => Token.TokenType.UNKNOWN,
        };
    }
}