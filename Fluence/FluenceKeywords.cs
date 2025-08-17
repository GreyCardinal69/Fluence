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
                "or",
                "not",
                "and",
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
            { "is", TokenType.EQUAL_EQUAL },
            { "not", TokenType.BANG_EQUAL },
            { "or", TokenType.OR },
            { "and", TokenType.AND },
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
                TokenType.OR        or
                TokenType.AND       or
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

        internal static TokenType GetKeywordType(ReadOnlySpan<char> text)
        {
            switch (text.Length)
            {
                case 2:
                    if (text.SequenceEqual("if")) return TokenType.IF;
                    if (text.SequenceEqual("in")) return TokenType.IN;
                    if (text.SequenceEqual("is")) return TokenType.EQUAL_EQUAL;
                    if (text.SequenceEqual("or")) return TokenType.OR;
                    break;
                case 3:
                    if (text.SequenceEqual("for")) return TokenType.FOR;
                    if (text.SequenceEqual("nil")) return TokenType.NIL;
                    if (text.SequenceEqual("not")) return TokenType.BANG_EQUAL;
                    if (text.SequenceEqual("and")) return TokenType.AND;
                    if (text.SequenceEqual("not")) return TokenType.BANG_EQUAL;
                    if (text.SequenceEqual("use")) return TokenType.USE;
                    break;
                case 4:
                    if (text.SequenceEqual("else")) return TokenType.ELSE;
                    if (text.SequenceEqual("enum")) return TokenType.ENUM;
                    if (text.SequenceEqual("func")) return TokenType.FUNC;
                    if (text.SequenceEqual("loop")) return TokenType.LOOP;
                    if (text.SequenceEqual("rest")) return TokenType.REST;
                    if (text.SequenceEqual("self")) return TokenType.SELF;
                    if (text.SequenceEqual("true")) return TokenType.TRUE;
                    if (text.SequenceEqual("type")) return TokenType.TYPE;
                    break;
                case 5:
                    if (text.SequenceEqual("break")) return TokenType.BREAK;
                    if (text.SequenceEqual("false")) return TokenType.FALSE;
                    if (text.SequenceEqual("match")) return TokenType.MATCH;
                    if (text.SequenceEqual("space")) return TokenType.SPACE;
                    if (text.SequenceEqual("while")) return TokenType.WHILE;
                    break;
                case 6:
                    if (text.SequenceEqual("return")) return TokenType.RETURN;
                    if (text.SequenceEqual("struct")) return TokenType.STRUCT;
                    break;
                case 8:
                    if (text.SequenceEqual("continue")) return TokenType.CONTINUE;
                    break;
            }

            // If we fall through the switch, it's not a keyword.
            return TokenType.IDENTIFIER;
        }

        internal static bool IsAKeyword(string key) => _keywords.Contains(key);

        internal static TokenType GetTokenTypeFromKeyword(string key) =>
            _keywordTypes.TryGetValue(key, out var type) ? type : TokenType.IDENTIFIER;
    }
}