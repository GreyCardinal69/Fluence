using static Fluence.Token;

namespace Fluence
{
    internal static class FluenceKeywords
    {
        private static readonly HashSet<string> _keywords =
        [
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
        ];

        private static readonly Dictionary<string, TokenType> _keywordTypes = new()
        {
            { "if", TokenType.IF },
            { "else", TokenType.ELSE },
            { "break", TokenType.BREAK },
            { "continue", TokenType.CONTINUE },
            { "for", TokenType.FOR },
            { "while", TokenType.WHILE },
            { "loop", TokenType.LOOP },
            { "in", TokenType.IN },
            { "func", TokenType.FUNC },
            { "nil", TokenType.NIL },
            { "return", TokenType.RETURN },
            { "true", TokenType.TRUE },
            { "false", TokenType.FALSE },
            { "is", TokenType.IS },
            { "not", TokenType.NOT },
            { "space", TokenType.SPACE },
            { "use", TokenType.USE },
            { "type", TokenType.TYPE },
            { "struct", TokenType.STRUCT },
            { "enum", TokenType.ENUM },
            { "match", TokenType.MATCH },
            { "self", TokenType.SELF },
            { "rest", TokenType.REST },
        };

        internal static bool TokenTypeIsAKeywordType(TokenType type) =>
            type switch
            {
                TokenType.BREAK     or
                TokenType.CONTINUE  or
                TokenType.IF        or
                TokenType.ELSE      or
                TokenType.WHILE     or
                TokenType.LOOP      or
                TokenType.FOR       or
                TokenType.IN        or
                TokenType.FUNC      or
                TokenType.NIL       or
                TokenType.RETURN    or
                TokenType.TRUE      or
                TokenType.FALSE     or
                TokenType.IS        or
                TokenType.NOT       or
                TokenType.SPACE     or
                TokenType.USE       or
                TokenType.TYPE      or
                TokenType.STRUCT    or
                TokenType.ENUM      or
                TokenType.MATCH     or
                TokenType.SELF      or
                TokenType.REST => true,
                _ => false,
            };

        internal static bool IsAKeyword(string key) => _keywords.Contains(key);

        internal static TokenType GetTokenTypeFromKeyword(string key) =>
            _keywordTypes.TryGetValue(key, out var type) ? type : TokenType.IDENTIFIER;
    }
}