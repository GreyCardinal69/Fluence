using static Fluence.Token;

namespace Fluence
{
    // Needs error handling.
    internal class FluenceLexer
    {
        private readonly string _sourceCode;
        private readonly int _sourceLength;
        private int _currentPosition;
        private int _currentLine;
        private int _currentColumn;
        private readonly TokenBuffer _tokenBuffer;

        internal int CurrentLine => _currentLine;
        internal int CurrentColumn => _currentColumn;
        internal bool HasReachedEnd => _currentPosition >= _sourceLength;
        internal int CurrentPosition => _currentPosition;
        internal char CharAtCurrentPosition => _sourceCode[_currentPosition];

        internal FluenceLexer(string source)
        {
            _tokenBuffer = new TokenBuffer(4, this);
            _sourceCode = source;
            _sourceLength = source.Length;
            _currentPosition = 0;
            _currentLine = 0;
            _currentColumn = 0;
        }

        private class TokenBuffer
        {
            private readonly Token[] _buffer;
            private readonly int _size;
            private readonly FluenceLexer _lexer;

            private int _head = 0;
            private int _tail = 0;
            private int _count = 0;

            internal TokenBuffer(int size, FluenceLexer lexer)
            {
                _size = size;
                _buffer = new Token[size];
                _lexer = lexer;
            }

            internal Token Consume()
            {
                EnsureFilled(1);

                Token token = _buffer[_head];
                _head = (_head + 1) % _size;
                _count--;

                return token;
            }

            internal Token Peek(int lookahead = 1)
            {
                EnsureFilled(lookahead);

                int index = (_head + lookahead - 1) % _size;
                return _buffer[index];
            }

            private void EnsureFilled(int requiredCount)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(requiredCount, _size);

                while (_count < requiredCount)
                {
                    Token nextToken = _lexer.GetNextToken();

                    _buffer[_tail] = nextToken;
                    _tail = (_tail + 1) % _size;
                    _count++;
                }
            }
        }

        internal Token PeekNextToken() => _tokenBuffer.Peek();
        internal Token PeekNextTokens(int n) => _tokenBuffer.Peek(n);
        internal Token ConsumeToken() => _tokenBuffer.Consume();

        internal void TrySkipEOLToken()
        {
            if (_tokenBuffer.Peek().Type == TokenType.EOL) _ = _tokenBuffer.Consume();
        }

        private Token GetNextToken()
        {
            SkipWhiteSpaceAndComments();

            if (HasReachedEnd) return Token.EOL;

            char currChar = _sourceCode[_currentPosition];
            int startPos = _currentPosition;

            Token newToken = new Token();
            newToken.Type = TokenType.UNKNOWN;

            /* The operator suite is quite large.
             * 1 Char: + - * / % < > = ! ^ ~ | &
             * 2 Char: == != <= => || && ** is >> << |> |? <| ~> .. ++ --
             * 3 Char: |?? |>> |~> <<| <>| <n| <?| not
             * 4 Char: |>>= <==| <!=| <<=| <>=| <??| <n|?
             * 5 Char: Surprisingly none yet.
             * 6 Char: <||!=| <||==| <||??|
             */

            /* Starting With _char_
             * <        <, <=, <<, <|, <<|, <>|, <n|, <?|, <==|, <!=|, <<=|, <>=|, <??|, <n|? and all of 6 Char
             * |        |>>=, |??, |>>, |~>, ||, |>, |?, |
             */

            /* For others we have:
             * [, ], {, }, (, )
             * . , ; :
             * Keywords
             * .. for ranges
             * =>, -> for functions and lambdas
             * Literals
             * _ for pipes
             * EOL
             */

            switch (currChar)
            {
                case '<': return ScanLessThanOperator();
                case '|': return ScanPipe();
                case '+':
                    if (CanLookAheadStartInclusive(2) && PeekString(2) == "++") return MakeTokenAndTryAdvance(TokenType.INCREMENT, 2);
                    else return MakeTokenAndTryAdvance(TokenType.PLUS, 1);
                case '-':
                    if (CanLookAheadStartInclusive(2) && PeekString(2) == "--") return MakeTokenAndTryAdvance(TokenType.DECREMENT, 2);
                    else if (CanLookAheadStartInclusive(2) && PeekString(2) == "->") return MakeTokenAndTryAdvance(TokenType.THIN_ARROW, 2);
                    else return MakeTokenAndTryAdvance(TokenType.MINUS, 1);
                case '/': return MakeTokenAndTryAdvance(TokenType.SLASH, 1); ;
                case '%': return MakeTokenAndTryAdvance(TokenType.PERCENT, 1);
                case '^': return MakeTokenAndTryAdvance(TokenType.CARET, 1);
                case '*':
                    if (CanLookAheadStartInclusive(2) && PeekString(2) == "**") return MakeTokenAndTryAdvance(TokenType.EXPONENT, 2);
                    else return MakeTokenAndTryAdvance(TokenType.STAR, 1);
                case '&':
                    if (CanLookAheadStartInclusive(2) && PeekString(2) == "&&") return MakeTokenAndTryAdvance(TokenType.AND, 2);
                    else return MakeTokenAndTryAdvance(TokenType.AMPERSAND, 1);
                case '>':
                    if (CanLookAheadStartInclusive(2) && PeekString(2) == ">>") return MakeTokenAndTryAdvance(TokenType.BITWISE_RIGHT_SHIFT, 2);
                    else return MakeTokenAndTryAdvance(TokenType.GREATER, 1);
                case '~':
                    if (CanLookAheadStartInclusive(2) && PeekString(2) == "~>") return MakeTokenAndTryAdvance(TokenType.COMPOSITION_PIPE, 2);
                    else return MakeTokenAndTryAdvance(TokenType.TILDE, 1);
                case '!':
                    if (CanLookAheadStartInclusive(2) && PeekString(2) == "!=") return MakeTokenAndTryAdvance(TokenType.BANG_EQUAL, 2);
                    else return MakeTokenAndTryAdvance(TokenType.BANG, 1);
                case '=':
                    if (CanLookAheadStartInclusive(2) && PeekString(2) == "==") return MakeTokenAndTryAdvance(TokenType.EQUAL_EQUAL, 2);
                    if (CanLookAheadStartInclusive(2) && PeekString(2) == ">=") return MakeTokenAndTryAdvance(TokenType.GREATER_EQUAL, 2);
                    else if (CanLookAheadStartInclusive(2) && PeekString(2) == "=>") return MakeTokenAndTryAdvance(TokenType.ARROW, 2);
                    else return MakeTokenAndTryAdvance(TokenType.EQUAL, 1);
                case '[': return MakeTokenAndTryAdvance(TokenType.L_BRACKET, 1);
                case ']': return MakeTokenAndTryAdvance(TokenType.R_BRACKET, 1);
                case '{': return MakeTokenAndTryAdvance(TokenType.L_BRACE, 1);
                case '}': return MakeTokenAndTryAdvance(TokenType.R_BRACE, 1);
                case '(': return MakeTokenAndTryAdvance(TokenType.L_PAREN, 1);
                case ')': return MakeTokenAndTryAdvance(TokenType.R_PAREN, 1);
                case ';':
                    string result;
                    // Check for cases like ;\r\n
                    if (CanLookAheadStartInclusive(3))
                    {
                        if (PeekString(3) == ";\r\n")
                        {
                            result = ";\r\n";
                            AdvancePosition(3);
                            AdvanceCurrentLine();
                        }
                        else if (PeekString(2) == ";\n")
                        {
                            result = ";\n";
                            AdvancePosition(2);
                            AdvanceCurrentLine();
                        }
                        else
                        {
                            AdvancePosition();
                            result = ";";
                        }
                        // Remove all EOLS that follow, they are redundant.
                        RemoveRedundantEOLS();
                        return MakeTokenAndTryAdvance(TokenType.EOL, 0, result);
                    }
                    return MakeTokenAndTryAdvance(TokenType.EOL, 1, ";");
                case ',': return MakeTokenAndTryAdvance(TokenType.COMMA, 1);
                case '?': return MakeTokenAndTryAdvance(TokenType.QUESTION, 1);
                case ':': return MakeTokenAndTryAdvance(TokenType.COLON, 1);
                case '\n':
                    AdvanceCurrentLine();
                    return MakeTokenAndTryAdvance(TokenType.EOL, 1);
                case '\r':
                    string text;
                    if (CanLookAheadStartInclusive(2) && PeekNext() == '\n')
                    {
                        AdvancePosition(2);
                        text = "\r\n";
                        // Remove all EOLS that follow, they are redundant.
                        RemoveRedundantEOLS();
                    }
                    else
                    {
                        text = "\r";
                    }
                    AdvanceCurrentLine();
                    return MakeTokenAndTryAdvance(TokenType.EOL, 1, text, text);
            }

            if (newToken.Type != TokenType.UNKNOWN) return newToken;

            // Other cases done individually.

            if (currChar == '.' || IsNumeric(currChar))
            {
                // Check for a range first, then a number.
                if (CanLookAheadStartInclusive(2))
                {
                    string peek = PeekString(2);
                    if (peek == "..")
                    {
                        // A range.
                        // Advance by 2 since "..".
                        return MakeTokenAndTryAdvance(TokenType.DOT_DOT, 2);
                    }
                }

                // Several cases here.
                // Just '.' -> Token.Dot
                // 1.000 Decimal
                // 1.000f Float
                startPos = _currentPosition;

                // Can be 0.5 or just .5 
                bool dotOnlyFraction = currChar == '.' && IsNumeric(PeekNext());

                if (IsNumeric(Peek()) || dotOnlyFraction)
                {
                    newToken.Type = TokenType.NUMBER;
                    while (_currentPosition < _sourceLength)
                    {
                        char lastc = currChar;
                        currChar = _sourceCode[_currentPosition];
                        if (IsNumeric(currChar) ||
                            currChar == '.' ||
                            currChar == 'E' ||
                            currChar == 'e' ||
                            ((currChar == '-' || currChar == '+') && (lastc == 'E' || lastc == 'e')))
                        {
                            AdvancePosition();
                        }
                        else break;
                    }

                    // Float here.
                    if (Peek() == 'f')
                    {
                        char previousChar = _sourceCode[_currentPosition - 1];
                        if (char.IsDigit(previousChar))
                        {
                            AdvancePosition();
                        }
                    }

                    string lexeme = _sourceCode[startPos.._currentPosition];

                    if (dotOnlyFraction) lexeme = lexeme.Insert(0, "0");

                    // We already advanced in the loop and "f" check.
                    return MakeTokenAndTryAdvance(TokenType.NUMBER, 0, lexeme, lexeme);
                }

                // Can't look ahead, just a random dot?
                return MakeTokenAndTryAdvance(TokenType.DOT, 1);
            }

            // _ is used for pipes, next char must not be an identifier if it is for a pipe.
            if (currChar == '_' && !IsIdentifier(PeekNext())) return MakeTokenAndTryAdvance(TokenType.UNDERSCORE, 1);

            bool isFString = currChar == 'f' && PeekNext() == '"';

            // Lex an identifier, unless an F string.
            if (IsIdentifier(currChar) && !isFString)
            {
                startPos = _currentPosition;
                while (!HasReachedEnd)
                {
                    if (IsIdentifier(Peek())) AdvancePosition();
                    else break;
                }

                string text = _sourceCode[startPos.._currentPosition];
                if (FluenceKeywords.IsAKeyword(text)) return MakeTokenAndTryAdvance(FluenceKeywords.GetTokenTypeFromKeyword(text));

                return MakeTokenAndTryAdvance(TokenType.IDENTIFIER, 0, text, text);
            }

            // Unless it is something alien, this should be the last match, otherwise
            // TokenType.Unknown will be thrown.
            if (CanLookAheadStartInclusive(2) && isFString)
            {
                AdvancePosition(2); // consume 'f' and '"'.
                return ScanString(startPos, true);
            }
            else if (currChar == '"')
            {
                AdvancePosition(); // consume '"'.
                return ScanString(startPos, false);
            }

            newToken.Type = TokenType.UNKNOWN;
            newToken.Text = _sourceCode[startPos.._currentPosition];
            return newToken;
        }

        private Token ScanString(int startPos, bool isFString = false)
        {
            int stringOpenColumn = _currentColumn - 1;
            while (Peek() != '"' && !HasReachedEnd)
            {
                char peek = Peek();
                if (peek == '\n') AdvanceCurrentLine();

                if (peek == '\\')
                {
                    AdvancePosition(); // Consume the '\'.
                }
                AdvancePosition();
            }

            if (HasReachedEnd)
            {
                // We ran out of code before finding the closing quote.
                // Error here.
                FluenceExceptionContext context = new FluenceExceptionContext()
                {
                    Column = stringOpenColumn,
                    FaultyLine = GetCodeLineFromSource(_sourceCode, _currentLine).TrimStart(),
                    LineNum = _currentLine,
                    Token = _tokenBuffer.Peek(),
                };

                // Interpreter will handle this more properly, as of now this is just a test.
                var exception = new FluenceLexerException("\nMissing closing string quote (\").", context);
                Console.WriteLine(exception);
                //throw exception;
            }

            AdvancePosition(); // Consume the closing quote "

            // Extract the full text (including quotes) and the inner value.
            string lexeme = _sourceCode[startPos.._currentPosition];

            string literalValue = _sourceCode.Substring(startPos + (isFString ? 2 : 1), _currentPosition - startPos - (isFString ? 3 : 2));

            TokenType type = isFString ? TokenType.F_STRING : TokenType.STRING;

            return new Token(type, lexeme, literalValue);
        }

        internal static string GetCodeLineFromSource(string source, int lineNumber)
        {
            if (lineNumber <= 0)
                return string.Empty;

            ReadOnlySpan<char> span = source.AsSpan();
            int currentLine = 1;
            int lineStart = 0;

            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == '\r' || span[i] == '\n')
                {
                    if (currentLine == lineNumber)
                        return span[lineStart..i].ToString();

                    // Handle \r\n as a single newline
                    if (span[i] == '\r' && i + 1 < span.Length && span[i + 1] == '\n')
                        i++;

                    currentLine++;
                    lineStart = i + 1;
                }
            }

            if (currentLine == lineNumber && lineStart < span.Length)
                return span[lineStart..].ToString();

            return string.Empty;
        }

        private void RemoveRedundantEOLS()
        {
            while (!HasReachedEnd)
            {
                if (CanLookAheadStartInclusive(2) && PeekString(2) == "\r\n")
                {
                    AdvanceCurrentLine();
                    AdvancePosition(2);
                }
                else break;
            }
        }

        private Token ScanPipe()
        {
            // |>>=
            // |??, |>>, |~>,
            // ||, |>, |?
            // |

            if (CanLookAheadStartInclusive(4))
                if (PeekString(4) == "|>>=") return MakeTokenAndTryAdvance(TokenType.REDUCER_PIPE, 4);

            if (CanLookAheadStartInclusive(3))
            {
                string threeChar = PeekString(3);

                switch (threeChar)
                {
                    case "|??": return MakeTokenAndTryAdvance(TokenType.GUARD_PIPE, 3);
                    case "|>>": return MakeTokenAndTryAdvance(TokenType.MAP_PIPE, 3);
                    case "|~>": return MakeTokenAndTryAdvance(TokenType.SCAN_PIPE, 3);
                }
            }

            if (CanLookAheadStartInclusive(2))
            {
                string twoChar = PeekString(2);

                switch (twoChar)
                {
                    case "||": return MakeTokenAndTryAdvance(TokenType.OR, 2);
                    case "|>": return MakeTokenAndTryAdvance(TokenType.PIPE, 2);
                    case "|?": return MakeTokenAndTryAdvance(TokenType.OPTIONAL_PIPE, 2);
                }
            }

            return MakeTokenAndTryAdvance(TokenType.PIPE_CHAR, 1);
        }

        private Token ScanLessThanOperator()
        {
            // <||!=| <||==| <||??|
            // <==|, <!=|, <<=|, <>=|, <??|, <n?| 
            // <<|, <>|, <n|, <?|
            // <=, <<, <|, 
            // <
            // First we check 6 Char operators.
            if (CanLookAheadStartInclusive(6))
            {
                string sixChar = PeekString(6);

                switch (sixChar)
                {
                    case "<||!=|":
                        return MakeTokenAndTryAdvance(TokenType.COLLECTIVE_OR_NOT_EQUAL, 6);
                    case "<||==|":
                        return MakeTokenAndTryAdvance(TokenType.COLLECTIVE_OR_EQUAL, 6);
                    case "<||??|":
                        return MakeTokenAndTryAdvance(TokenType.OR_GUARD_CHAIN, 6);
                }
            }

            // Now we check 4 Char operators.
            if (CanLookAheadStartInclusive(4))
            {
                string fourChar = PeekString(4);

                switch (fourChar)
                {
                    case "<==|":
                        return MakeTokenAndTryAdvance(TokenType.COLLECTIVE_EQUAL, 4);
                    case "<!=|":
                        return MakeTokenAndTryAdvance(TokenType.COLLECTIVE_NOT_EQUAL, 4);
                    case "<<=|":
                        return MakeTokenAndTryAdvance(TokenType.COLLECTIVE_LESS_EQUAL, 4);
                    case "<>=|":
                        return MakeTokenAndTryAdvance(TokenType.COLLECTIVE_GREATER_EQUAL, 4);
                    case "<??|":
                        return MakeTokenAndTryAdvance(TokenType.GUARD_CHAIN, 4);
                }
            }

            // <n?| and <n|
            if (CanLookAheadStartInclusive(2) && char.IsDigit(_sourceCode[_currentPosition + 1]))
            {
                AdvancePosition();
                // Store the number for the Token in GetNextToken().
                string n = ReadNumber();

                if (Match("|?"))
                {
                    // We matched <n|?
                    // Only assign the number as text/literal, the rest of the operator is in the TokenType.
                    return new Token(TokenType.OPTIONAL_ASSIGN_N, n, n);
                }
                if (Match("|"))
                {
                    return new Token(TokenType.CHAIN_ASSIGN_N, n, n);
                }
                // If we get here then we have an error, backtrack or throw.
                // incomplete chain assignment.
            }

            // Now we check 3 Char operators.
            if (CanLookAheadStartInclusive(3))
            {
                string threeChar = PeekString(3);

                switch (threeChar)
                {
                    case "<<|": return MakeTokenAndTryAdvance(TokenType.COLLECTIVE_LESS, 3);
                    case "<>|": return MakeTokenAndTryAdvance(TokenType.COLLECTIVE_GREATER, 3);
                    case "<?|": return MakeTokenAndTryAdvance(TokenType.OPTIONAL_ASSIGN, 3);
                }
            }

            // Two Char operators.
            if (CanLookAheadStartInclusive(2))
            {
                string twoChar = PeekString(2);

                switch (twoChar)
                {
                    case "<=": return MakeTokenAndTryAdvance(TokenType.LESS_EQUAL, 2);
                    case "<<": return MakeTokenAndTryAdvance(TokenType.BITWISE_LEFT_SHIFT, 2);
                    case "<|": return MakeTokenAndTryAdvance(TokenType.REST_ASSIGN, 2);
                }
            }

            // One character here.
            return MakeTokenAndTryAdvance(TokenType.LESS, 1);
        }

        private void AdvanceCurrentLine()
        {
            _currentLine++;
            _currentColumn = 1;
        }

        private void AdvancePosition(int n = 1)
        {
            _currentColumn += n;
            _currentPosition += n;
        }

        private bool Match(string expected)
        {
            if (!CanLookAheadStartInclusive(expected.Length)) return false;
            if (_sourceCode.Substring(_currentPosition, expected.Length) != expected) return false;
            AdvancePosition(expected.Length);
            return true;
        }

        public static bool IsIdentifier(char c)
        {
            return c == '\u009F'
                || (c >= 'a' && c <= 'z')
                || (c >= 'A' && c <= 'Z')
                || (c == '_') // Unless by itself, should be an identifier.
                || (c >= '0' && c <= '9');
        }

        private char Peek() => _currentPosition >= _sourceLength ? '\0' : _sourceCode[_currentPosition];
        private char PeekNext() => _currentPosition + 1 >= _sourceLength ? '\0' : _sourceCode[_currentPosition + 1];

        private string ReadNumber()
        {
            int start = _currentPosition;
            while (char.IsDigit(Peek())) AdvancePosition();
            return _sourceCode[start.._currentPosition];
        }

        private static bool IsNumeric(char c)
        {
            return c >= '0' && c <= '9';
        }

        private string PeekString(int length) => _sourceCode.Substring(_currentPosition, length);

        private bool CanLookAheadStartInclusive(int numberOfChars = 1) => _currentPosition + numberOfChars <= _sourceLength;

        internal void SkipWhiteSpaceAndComments()
        {
            while (!HasReachedEnd)
            {
                char currentChar = _sourceCode[_currentPosition];

                if (IsWhiteSpace(currentChar))
                {
                    while (!HasReachedEnd && IsWhiteSpace(_sourceCode[_currentPosition]))
                    {
                        AdvancePosition();
                    }
                    continue;
                }

                if (IsMultiLineComment())
                {
                    AdvancePosition(2);
                    while (!HasReachedEnd)
                    {
                        if (!CanLookAheadStartInclusive(2))
                        {
                            // error here, we are in multiline comment, yet can't look ahead two chars, so it ends as "#* .... *".
                        }
                        if (_sourceCode[_currentPosition] != '*' && _sourceCode[_currentPosition + 1] != '#') _currentPosition++;
                        else
                        {
                            AdvancePosition(2);
                            break;
                        }
                    }
                    continue;
                }

                if (currentChar == '#')
                {
                    while (_sourceCode[_currentPosition] != '\n') AdvancePosition();
                    continue;
                }

                break;
            }
        }

        private Token MakeTokenAndTryAdvance(TokenType type, int len = 0, string text = null, object lieteral = null)
        {
            AdvancePosition(len);
            return new Token(type, text, lieteral);
        }

        private bool IsMultiLineComment()
        {
            if (_currentPosition > _sourceLength - 1) return false;

            return _sourceCode[_currentPosition] == '#' && _sourceCode[_currentPosition + 1] == '*';
        }

        private static bool IsWhiteSpace(char c) => c == ' ' || c == '\t';
    }
}