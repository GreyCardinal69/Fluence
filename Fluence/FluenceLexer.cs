using static Fluence.Token;

namespace Fluence
{
    internal sealed class FluenceLexer
    {
        private readonly string _sourceCode;
        private readonly int _sourceLength;
        private int _currentPosition;
        private int _currentLine;
        private int _currentColumn;
        private readonly TokenBuffer _tokenBuffer;

        internal int CurrentLine => _currentLine;
        internal int CurrentColumn => _currentColumn;
        internal bool HasReachedEnd => _currentPosition >= _sourceLength & _tokenBuffer.HasReachedEnd;

        private bool _hasReachedEndInternal
        {
            get
            {
                return _currentPosition >= _sourceLength;
            }
        }

        internal int CurrentPosition => _currentPosition;
        internal int SourceLength => _sourceLength;
        internal char CharAtCurrentPosition => _sourceCode[_currentPosition];

        internal FluenceLexer(string source)
        {
            _tokenBuffer = new TokenBuffer(this);
            _sourceCode = source;
            _sourceLength = source.Length;
            _currentPosition = 0;
            _currentLine = 0;
            _currentColumn = 0;
        }

        private sealed class TokenBuffer
        {
            private readonly List<Token> _buffer = new List<Token>();
            private readonly FluenceLexer _lexer;
            private int _head = 0;
            private bool _lexerFinished = false;

            // A reasonable threshold for trimming the buffer.
            private const int _trimThreshold = 256;

            public bool HasReachedEnd
            {
                get
                {
                    if (!_lexerFinished)
                    {
                        EnsureFilled(1);
                    }

                    if (_head >= _buffer.Count)
                    {
                        return _lexerFinished;
                    }

                    return _buffer[_head].Type == TokenType.EOF;
                }
            }


            internal TokenBuffer(FluenceLexer lexer)
            {
                _lexer = lexer;
            }

            /// <summary>
            /// Consumes the next token from the buffer and advances the head.
            /// </summary>
            internal Token Consume()
            {
                EnsureFilled(1);

                Token token = _buffer[_head];

                if (token.Type != TokenType.EOF)
                {
                    _head++;
                }

                // Periodically trim the buffer to conserve memory.
                if (_head >= _trimThreshold)
                {
                    Compact();
                }

                return token;
            }

            /// <summary>
            /// Removes consumed tokens from the beginning of the list to save memory.
            /// </summary>
            private void Compact()
            {
                if (_head > 0)
                {
                    _buffer.RemoveRange(0, _head);
                    _head = 0;
                }
            }

            /// <summary>
            /// Peeks ahead a given number of tokens from the current position.
            /// lookahead=1 is the very next token.
            /// </summary>
            internal Token Peek(int lookahead = 1)
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lookahead);

                EnsureFilled(lookahead);

                int index = _head + lookahead - 1;

                if (index >= _buffer.Count)
                {
                    return _buffer[^1];
                }

                return _buffer[index];
            }

            private void EnsureFilled(int requiredCount)
            {
                // If the lexer is already known to be finished, we cannot generate more tokens.
                if (_lexerFinished)
                {
                    return;
                }

                while ((_buffer.Count - _head) < requiredCount)
                {
                    Token nextToken = _lexer.GetNextToken();
                    _buffer.Add(nextToken);

                    if (nextToken.Type == TokenType.EOF)
                    {
                        _lexerFinished = true;
                        break;
                    }
                }
            }
        }

        internal Token PeekNextToken() => _tokenBuffer.Peek();
        internal Token PeekAheadByN(int n) => _tokenBuffer.Peek(n);
        internal Token ConsumeToken() => _tokenBuffer.Consume();

        internal void TrySkipEOLToken()
        {
            if (_tokenBuffer.Peek().Type == TokenType.EOL) _ = _tokenBuffer.Consume();
        }

        private Token GetNextToken()
        {
            SkipWhiteSpaceAndComments();

            if (_hasReachedEndInternal) return EOF;

            char currChar = _sourceCode[_currentPosition];
            int startPos = _currentPosition;

            // Line of code where an error has been detected.
            string faultyCodeLine;

            /* The operator suite is quite large.
             * 1 Char: + - * / % < > = ! ^ ~ | &
             * 2 Char: == != <= => || && ** is >> << |> |? <| ~> .. ++ --
             * +=. -=. *=, /=
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
            char nextChar;

            switch (currChar)
            {
                case '<': return ScanLessThanOperator();
                case '|': return ScanPipe();
                case '+':
                    if (CanLookAheadStartInclusive(2))
                    {
                        nextChar = PeekNext();
                        if (nextChar == '+') return MakeTokenAndTryAdvance(TokenType.INCREMENT, 2);
                        if (nextChar == '=') return MakeTokenAndTryAdvance(TokenType.EQUAL_PLUS, 2);
                    }
                    return MakeTokenAndTryAdvance(TokenType.PLUS, 1);
                case '-':
                    if (CanLookAheadStartInclusive(2))
                    {
                        nextChar = PeekNext();
                        if (nextChar == '-') return MakeTokenAndTryAdvance(TokenType.DECREMENT, 2);
                        if (nextChar == '=') return MakeTokenAndTryAdvance(TokenType.EQUAL_MINUS, 2);
                        if (nextChar == '>') return MakeTokenAndTryAdvance(TokenType.THIN_ARROW, 2);
                    }
                    return MakeTokenAndTryAdvance(TokenType.MINUS, 1);
                case '/':
                    if (CanLookAheadStartInclusive(2) && PeekNext() == '=') return MakeTokenAndTryAdvance(TokenType.EQUAL_DIV, 2);
                    return MakeTokenAndTryAdvance(TokenType.SLASH, 1);
                case '%':
                    if (CanLookAheadStartInclusive(2) && PeekNext() == '=') return MakeTokenAndTryAdvance(TokenType.EQUAL_PERCENT, 2);
                    return MakeTokenAndTryAdvance(TokenType.PERCENT, 1);
                case '^': return MakeTokenAndTryAdvance(TokenType.CARET, 1);
                case '*':
                    if (CanLookAheadStartInclusive(2))
                    {
                        nextChar = PeekNext();
                        if (nextChar == '*') return MakeTokenAndTryAdvance(TokenType.EXPONENT, 2);
                        if (nextChar == '=') return MakeTokenAndTryAdvance(TokenType.EQUAL_MUL, 2);
                    }
                    return MakeTokenAndTryAdvance(TokenType.STAR, 1);
                case '&':
                    if (CanLookAheadStartInclusive(2))
                    {
                        nextChar = PeekNext();
                        if (nextChar == '&') return MakeTokenAndTryAdvance(TokenType.AND, 2);
                        if (nextChar == '=') return MakeTokenAndTryAdvance(TokenType.EQUAL_AMPERSAND, 2);
                    }
                    return MakeTokenAndTryAdvance(TokenType.AMPERSAND, 1);
                case '>':
                    if (CanLookAheadStartInclusive(2))
                    {
                        nextChar = PeekNext();
                        if (nextChar == '<') return MakeTokenAndTryAdvance(TokenType.SWAP, 2);
                        if (nextChar == '>') return MakeTokenAndTryAdvance(TokenType.BITWISE_RIGHT_SHIFT, 2);
                        if (nextChar == '=') return MakeTokenAndTryAdvance(TokenType.GREATER_EQUAL, 2);
                    }
                    return MakeTokenAndTryAdvance(TokenType.GREATER, 1);
                case '~':
                    if (CanLookAheadStartInclusive(2) && PeekNext() == '>') return MakeTokenAndTryAdvance(TokenType.COMPOSITION_PIPE, 2);
                    return MakeTokenAndTryAdvance(TokenType.TILDE, 1);
                case '!':
                    if (CanLookAheadStartInclusive(2))
                    {
                        nextChar = PeekNext();
                        if (nextChar == '!') return MakeTokenAndTryAdvance(TokenType.BOOLEAN_FLIP, 2);
                        if (nextChar == '=') return MakeTokenAndTryAdvance(TokenType.BANG_EQUAL, 2);
                    }
                    return MakeTokenAndTryAdvance(TokenType.BANG, 1);
                case '=':
                    if (CanLookAheadStartInclusive(2))
                    {
                        nextChar = PeekNext();
                        if (nextChar == '=') return MakeTokenAndTryAdvance(TokenType.EQUAL_EQUAL, 2);
                        if (nextChar == '>') return MakeTokenAndTryAdvance(TokenType.ARROW, 2);
                    }
                    return MakeTokenAndTryAdvance(TokenType.EQUAL, 1);
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
                        if (PeekString(3).SequenceEqual(";\r\n"))
                        {
                            result = ";\r\n";
                            AdvancePosition(3);
                            AdvanceCurrentLine();
                        }
                        else if (PeekString(2).SequenceEqual(";\n"))
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
                case '?':
                    if (CanLookAheadStartInclusive(2))
                    {
                        if (PeekString(2).SequenceEqual("?:")) return MakeTokenAndTryAdvance(TokenType.TERNARY_JOINT, 2);
                    }
                    return MakeTokenAndTryAdvance(TokenType.QUESTION, 1);
                case ':': return MakeTokenAndTryAdvance(TokenType.COLON, 1);
                case '\'':
                    _currentPosition++;
                    return MakeTokenAndTryAdvance(TokenType.CHARACTER, 2, _sourceCode[_currentPosition].ToString(), _sourceCode[_currentPosition]);
                case '\n':
                    AdvanceCurrentLine();
                    return MakeTokenAndTryAdvance(TokenType.EOL, 1, "\n", "\n");
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

            // Other cases done individually.

            if (currChar == '.' || IsNumeric(currChar))
            {
                // Check for a range first, then a number.
                if (CanLookAheadStartInclusive(2))
                {
                    if (PeekString(2).SequenceEqual(".."))
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
                    bool decimalPointAlreadyDefined = false;
                    while (_currentPosition < _sourceLength)
                    {
                        char lastc = currChar;
                        currChar = _sourceCode[_currentPosition];

                        if (currChar == '.' && _sourceCode[_currentPosition + 1] == '.')
                        {
                            string str = _sourceCode[startPos.._currentPosition];
                            if (dotOnlyFraction) str = str.Insert(0, "0");
                            return MakeTokenAndTryAdvance(TokenType.NUMBER, 0, str, str);
                        }

                        if (currChar == '.')
                        {
                            if (decimalPointAlreadyDefined)
                            {
                                faultyCodeLine = GetCodeLineFromSource(_sourceCode, _currentLine + 1).TrimStart();
                                ConstructAndThrowLexerException(_currentLine + 1, _currentColumn, "Invalid number format: multiple decimal points found.", faultyCodeLine, PeekNextToken());
                            }
                            decimalPointAlreadyDefined = true;
                        }

                        if (currChar == 'E' || currChar == 'e')
                        {
                            // After seeing an 'E', the very next character MUST be a digit or a sign.
                            char next = PeekNext();
                            if (!IsNumeric(next) && next != '+' && next != '-')
                            {
                                string faultyLine = GetCodeLineFromSource(_sourceCode, _currentLine + 1);
                                ConstructAndThrowLexerException(_currentLine + 1, _currentColumn + 1, "Scientific notation 'E' must be followed by digits.", faultyLine, PeekNextToken());
                            }
                            else
                            {
                                AdvancePosition();
                            }
                        }
                        else if (IsNumeric(currChar) ||
                            currChar == '.' ||
                            ((currChar == '-' || currChar == '+') && (lastc == 'E' || lastc == 'e')))
                        {
                            AdvancePosition();
                        }
                        else break;
                    }

                    // Check if the number ends with a dot
                    if (_sourceCode[_currentPosition - 1] == '.')
                    {
                        faultyCodeLine = GetCodeLineFromSource(_sourceCode, _currentLine + 1).TrimStart();
                        ConstructAndThrowLexerException(_currentLine + 1, _currentColumn, "Number literal cannot end with a decimal point.", faultyCodeLine, PeekNextToken());
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
                while (!_hasReachedEndInternal)
                {
                    if (IsIdentifier(Peek())) AdvancePosition();
                    else break;
                }

                var identifierSpan = _sourceCode.AsSpan(startPos, _currentPosition - startPos);

                TokenType type = FluenceKeywords.GetKeywordType(identifierSpan);

                if (type != TokenType.IDENTIFIER)
                {
                    return new Token(type);
                }
                else
                {
                    string text = identifierSpan.ToString();
                    return new Token(TokenType.IDENTIFIER, text, text);
                }
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

            int errorColumn = _currentColumn == 0 ? 2 : _currentColumn;
            faultyCodeLine = GetCodeLineFromSource(_sourceCode, _currentLine + 1).TrimStart();
            char invalidChar = _sourceCode[startPos];

            ConstructAndThrowLexerException(_currentLine + 1, errorColumn, $"Invalid character '{invalidChar}' found in source. Could not generate appropriate Token.", faultyCodeLine, PeekNextToken());
            return new();
        }

        private static void ConstructAndThrowLexerException(int lineNum, int column, string errorText, string faultyLine, Token token)
        {
            LexerExceptionContext context = new LexerExceptionContext()
            {
                LineNum = lineNum,
                Column = column,
                FaultyLine = faultyLine,
                Token = token
            };
            throw new FluenceLexerException(errorText, context);
        }

        private Token ScanString(int startPos, bool isFString = false)
        {
            int stringOpenColumn = _currentColumn;
            int stringInitialLine = _currentLine + 1;

            while (Peek() != '"' && !_hasReachedEndInternal)
            {
                char peek = Peek();
                if (peek == '\n') AdvanceCurrentLine();

                if (peek == '\\')
                {
                    AdvancePosition(); // Consume the '\'.
                }
                AdvancePosition();
            }

            if (_hasReachedEndInternal)
            {
                string initialLineContent = GetCodeLineFromSource(_sourceCode, stringInitialLine).TrimStart();
                string truncatedLine = TruncateLine(initialLineContent);

                ConstructAndThrowLexerException(stringInitialLine, stringOpenColumn, "Unclosed string literal. The file ended before a closing '\"' was found.", truncatedLine, PeekNextToken());
            }

            AdvancePosition(); // Consume the closing quote "

            // Extract the full text (including quotes) and the inner value.
            string lexeme = _sourceCode[startPos.._currentPosition];

            string literalValue = _sourceCode.Substring(startPos + (isFString ? 2 : 1), _currentPosition - startPos - (isFString ? 3 : 2));

            TokenType type = isFString ? TokenType.F_STRING : TokenType.STRING;

            return new Token(type, lexeme, literalValue);
        }

        internal static string TruncateLine(string line, int maxLength = 75)
        {
            if (string.IsNullOrEmpty(line) || line.Length <= maxLength)
            {
                return line;
            }
            return string.Concat(line.AsSpan(0, maxLength - 3), "...");
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
                if (span[i] is '\r' or '\n')
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
            while (!_hasReachedEndInternal)
            {
                if (CanLookAheadStartInclusive(2) && PeekString(2).SequenceEqual("\r\n"))
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

            int availableLength = _sourceLength - _currentPosition;
            ReadOnlySpan<char> peek = PeekString(Math.Min(4, availableLength));

            if (peek.Length >= 2)
            {
                switch (peek[1])
                {
                    case '>': // Could be |> or |>> or |>>=
                        if (peek.Length >= 4 && peek[2] == '>' && peek[3] == '=')
                        {
                            return MakeTokenAndTryAdvance(TokenType.REDUCER_PIPE, 4);
                        }
                        if (peek.Length >= 3 && peek[2] == '>')
                        {
                            return MakeTokenAndTryAdvance(TokenType.MAP_PIPE, 3);
                        }
                        return MakeTokenAndTryAdvance(TokenType.PIPE, 2);

                    case '?': // Could be |? or |??
                        if (peek.Length >= 3 && peek[2] == '?')
                        {
                            return MakeTokenAndTryAdvance(TokenType.GUARD_PIPE, 3);
                        }
                        return MakeTokenAndTryAdvance(TokenType.OPTIONAL_PIPE, 2);

                    case '~': // Could be |~>
                        if (peek.Length >= 3 && peek[2] == '>')
                        {
                            return MakeTokenAndTryAdvance(TokenType.SCAN_PIPE, 3);
                        }
                        break;

                    case '|': // Must be ||
                        return MakeTokenAndTryAdvance(TokenType.OR, 2);
                }
            }

            // If we fall through all the fast-paths, it must be the single-character operator.
            return MakeTokenAndTryAdvance(TokenType.PIPE_CHAR, 1);
        }

        private Token ScanLessThanOperator()
        {
            // <||!=| <||==| <||??| <||<=| <||>=|
            // <||<|   <||>| 
            // <==|, <!=|, <<=|, <>=|, <??|, <n?| 
            // <<|, <>|, <n|, <?|
            // <=, <<, <|, 
            // <

            int availableLength = _sourceLength - _currentPosition;
            ReadOnlySpan<char> peek = PeekString(Math.Min(6, availableLength));

            // First we check 6 Char operators.
            if (peek.Length >= 6 && peek[1] == '|' && peek[2] == '|')
            {
                switch (peek)
                {
                    case "<||!=|":
                        return MakeTokenAndTryAdvance(TokenType.COLLECTIVE_OR_NOT_EQUAL, 6);
                    case "<||==|":
                        return MakeTokenAndTryAdvance(TokenType.COLLECTIVE_OR_EQUAL, 6);
                    case "<||??|":
                        return MakeTokenAndTryAdvance(TokenType.OR_GUARD_CHAIN, 6);
                    case "<||<=|":
                        return MakeTokenAndTryAdvance(TokenType.COLLECTIVE_OR_LESS_EQUAL, 6);
                    case "<||>=|":
                        return MakeTokenAndTryAdvance(TokenType.COLLECTIVE_OR_GREATER_EQUAL, 6);
                }
            }

            if (peek.Length >= 5 && peek[1] == '|' && peek[2] == '|')
            {
                switch (peek[..5])
                {
                    case "<||<|":
                        return MakeTokenAndTryAdvance(TokenType.COLLECTIVE_OR_LESS, 5);
                    case "<||>|":
                        return MakeTokenAndTryAdvance(TokenType.COLLECTIVE_OR_GREATER, 5);
                }
            }

            // Now we check 4 Char operators.
            if (peek.Length >= 4 && peek[3] == '|')
            {
                switch (peek[..4])
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
            if (peek.Length >= 2 && char.IsDigit(_sourceCode[_currentPosition + 1]))
            {
                AdvancePosition();
                // Store the number for the Token in GetNextToken().
                string n = ReadNumber();

                if (Match("?|"))
                {
                    // We matched <n?|
                    // Only assign the number as text/literal, the rest of the operator is in the TokenType.
                    return new Token(TokenType.OPTIONAL_ASSIGN_N, n, n);
                }
                if (Match("|"))
                {
                    return new Token(TokenType.CHAIN_ASSIGN_N, n, n);
                }

                // If we get here then we have an error.
                // incomplete chain assignment.

                string initialLineContent = GetCodeLineFromSource(_sourceCode, _currentLine + 1).TrimStart();
                string truncatedLine = TruncateLine(initialLineContent);

                ConstructAndThrowLexerException(_currentLine + 1, _currentColumn - 2, "Faulty chain assignment pipe operator detected, expected '<n|' or '<n?|' or '<|' format.", truncatedLine, PeekNextToken());
            }

            // Now we check 3 Char operators.
            if (peek.Length >= 3 && peek[2] == '|')
            {
                switch (peek[..3])
                {
                    case "<<|": return MakeTokenAndTryAdvance(TokenType.COLLECTIVE_LESS, 3);
                    case "<>|": return MakeTokenAndTryAdvance(TokenType.COLLECTIVE_GREATER, 3);
                    case "<?|": return MakeTokenAndTryAdvance(TokenType.OPTIONAL_REST_ASSIGN, 3);
                }
            }

            // Two Char operators.
            if (peek.Length >= 2 && (peek[1] == '|' || peek[1] == '<' || peek[1] == '='))
            {
                switch (peek[..2])
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
            if (!PeekString(expected.Length).SequenceEqual(expected)) return false;

            AdvancePosition(expected.Length);
            return true;
        }

        private static bool IsIdentifier(char c) => char.IsLetterOrDigit(c) || c is '\u009F' || c is '_';

        private char Peek() => _currentPosition >= _sourceLength ? '\0' : _sourceCode[_currentPosition];
        private char PeekNext() => _currentPosition + 1 >= _sourceLength ? '\0' : _sourceCode[_currentPosition + 1];

        private string ReadNumber()
        {
            int start = _currentPosition;
            while (char.IsDigit(Peek())) AdvancePosition();
            return _sourceCode[start.._currentPosition];
        }

        private static bool IsNumeric(char c) => c is >= '0' and <= '9';

        private ReadOnlySpan<char> PeekString(int length) => _sourceCode.AsSpan(_currentPosition, length);

        private bool CanLookAheadStartInclusive(int numberOfChars = 1) => _currentPosition + numberOfChars <= _sourceLength;

        internal void SkipWhiteSpaceAndComments()
        {
            int commentStartLine = _currentLine + 1;
            int commentStartCol = _currentColumn + 1;

            while (!_hasReachedEndInternal)
            {
                bool skippedSomethingThisPass = false;

                while (!_hasReachedEndInternal && IsWhiteSpace(_sourceCode[_currentPosition]))
                {
                    AdvancePosition();
                    skippedSomethingThisPass = true;
                }

                if (!_hasReachedEndInternal && _sourceCode[_currentPosition] == '#')
                {
                    // Check for multi-line comment: '#*'
                    if (CanLookAheadStartInclusive(2) && _sourceCode[_currentPosition + 1] == '*')
                    {
                        int level = 0; // We can have #* inside #*, to not read first *# as end of multiline, we keep track of level.
                        commentStartCol += 2;
                        skippedSomethingThisPass = true;
                        AdvancePosition(2); // Consume '#*'
                        bool didntEndMultiLineComment = true;

                        while (!_hasReachedEndInternal)
                        {
                            if (CanLookAheadStartInclusive(2) && _sourceCode[_currentPosition] == '#' && _sourceCode[_currentPosition + 1] == '*')
                            {
                                level++;
                            }
                            if (CanLookAheadStartInclusive(2) && _sourceCode[_currentPosition] == '*' && _sourceCode[_currentPosition + 1] == '#')
                            {
                                if (level > 0) level--;
                                else
                                {
                                    didntEndMultiLineComment = false;
                                    AdvancePosition(2); // Consume '*#'
                                    break;
                                }
                            }
                            if (Peek() == '\n') AdvanceCurrentLine();
                            AdvancePosition();
                        }

                        if (didntEndMultiLineComment)
                        {
                            string initialLineContent = GetCodeLineFromSource(_sourceCode, commentStartLine).TrimStart();
                            string truncatedLine = TruncateLine(initialLineContent);

                            ConstructAndThrowLexerException(commentStartLine, commentStartCol, "Unterminated multi-line comment. The file ended before a closing '*#' was found.", truncatedLine, PeekNextToken());
                        }
                    }
                    else // It's a single-line comment
                    {
                        skippedSomethingThisPass = true;
                        while (!_hasReachedEndInternal && _sourceCode[_currentPosition] != '\n')
                        {
                            AdvancePosition();
                        }
                    }
                }

                if (!skippedSomethingThisPass)
                {
                    break;
                }
            }
        }

        private Token MakeTokenAndTryAdvance(TokenType type, int len = 0, string text = null, object lieteral = null)
        {
            AdvancePosition(len);
            return new Token(type, text, lieteral);
        }

        private static bool IsWhiteSpace(char c) => c is ' ' or '\t' || c is '\r';
    }
}