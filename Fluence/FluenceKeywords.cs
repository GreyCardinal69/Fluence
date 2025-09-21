﻿using static Fluence.Token;

namespace Fluence
{
    /// <summary>
    /// A static utility class for managing and identifying Fluence language keywords.
    /// </summary>
    internal static class FluenceKeywords
    {
        internal static bool TokenTypeIsAKeywordType(TokenType type) =>
            type switch
            {
                TokenType.BREAK or
                TokenType.CONTINUE or
                TokenType.IF or
                TokenType.ELSE or
                TokenType.WHILE or
                TokenType.LOOP or
                TokenType.FOR or
                TokenType.IN or
                TokenType.FUNC or
                TokenType.NIL or
                TokenType.RETURN or
                TokenType.TRUE or
                TokenType.FALSE or
                TokenType.IS or
                TokenType.NOT or
                TokenType.OR or
                TokenType.AND or
                TokenType.SPACE or
                TokenType.USE or
                TokenType.TYPE or
                TokenType.STRUCT or
                TokenType.ENUM or
                TokenType.MATCH or
                TokenType.SELF or
                TokenType.SOLID or
                TokenType.TIMES or
                TokenType.UNLESS or
                TokenType.AS or
                TokenType.REST => true,
                _ => false,
            };

        /// <summary>
        /// Gets the corresponding <see cref="TokenType"/> for a given keyword string.
        /// If the text is not a keyword, it returns <see cref="TokenType.IDENTIFIER"/>.
        /// </summary>
        /// <param name="text">A ReadOnlySpan of characters representing the potential keyword.</param>
        /// <returns>The associated <see cref="TokenType"/> or <see cref="TokenType.IDENTIFIER"/>.</returns>
        internal static TokenType GetKeywordType(ReadOnlySpan<char> text)
        {
            switch (text.Length)
            {
                case 2:
                    if (text[0] == 'i')
                    {
                        if (text.SequenceEqual("if")) return TokenType.IF;
                        if (text.SequenceEqual("in")) return TokenType.IN;
                        if (text.SequenceEqual("is")) return TokenType.EQUAL_EQUAL;
                    }
                    if (text.SequenceEqual("or")) return TokenType.OR;
                    if (text.SequenceEqual("as")) return TokenType.AS;
                    break;
                case 3:
                    if (text[0] == 'n')
                    {
                        if (text.SequenceEqual("nil")) return TokenType.NIL;
                        if (text.SequenceEqual("not")) return TokenType.BANG_EQUAL;
                    }
                    if (text.SequenceEqual("for")) return TokenType.FOR;
                    if (text.SequenceEqual("and")) return TokenType.AND;
                    if (text.SequenceEqual("use")) return TokenType.USE;
                    break;
                case 4:
                    if (text[0] == 'e')
                    {
                        if (text[1] == 'l') return TokenType.ELSE;
                        if (text[1] == 'n') return TokenType.ENUM;
                    }
                    if (text[0] == 't')
                    {
                        if (text[1] == 'r') return TokenType.TRUE;
                        if (text[1] == 'y') return TokenType.TYPE;
                    }
                    if (text.SequenceEqual("func")) return TokenType.FUNC;
                    if (text.SequenceEqual("loop")) return TokenType.LOOP;
                    if (text.SequenceEqual("rest")) return TokenType.REST;
                    if (text.SequenceEqual("self")) return TokenType.SELF;
                    break;
                case 5:
                    if (text.SequenceEqual("break")) return TokenType.BREAK;
                    if (text.SequenceEqual("false")) return TokenType.FALSE;
                    if (text.SequenceEqual("match")) return TokenType.MATCH;
                    if (text.SequenceEqual("space")) return TokenType.SPACE;
                    if (text.SequenceEqual("while")) return TokenType.WHILE;
                    if (text.SequenceEqual("solid")) return TokenType.SOLID;
                    if (text.SequenceEqual("times")) return TokenType.TIMES;
                    break;
                case 6:
                    if (text.SequenceEqual("return")) return TokenType.RETURN;
                    if (text.SequenceEqual("struct")) return TokenType.STRUCT;
                    if (text.SequenceEqual("unless")) return TokenType.UNLESS;
                    break;
                case 8:
                    if (text.SequenceEqual("continue")) return TokenType.CONTINUE;
                    break;
            }

            // If we fall through the switch, it's not a keyword.
            return TokenType.IDENTIFIER;
        }
    }
}