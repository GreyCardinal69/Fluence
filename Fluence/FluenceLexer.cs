using Testing_Chamber.Minis;
using static Fluence.Token;

namespace Fluence
{
    internal class FluenceLexer
    {
        private readonly string _sourceCode;
        private readonly int _sourceLength;
        private int _currentPosition;
        private int _currentLine;
        private Queue<Token> _tokenQueue;

        private int _chainAssignNumber = 0;

        internal int CurrentLine => _currentLine;
        internal bool HasReachedEnd => _currentPosition >= _sourceLength && _tokenQueue.Count == 0;
        internal int CurrentPosition => _currentPosition;
        internal char CharAtCurrentPosition => _sourceCode[_currentPosition];

        internal FluenceLexer(string source)
        {
            _sourceCode = source;
            _sourceLength = source.Length;
            _currentPosition = 0;
            _currentLine = 0;
            _tokenQueue = new Queue<Token>();
        }

        internal Token GetNextToken()
        {
            SkipWhiteSpaceAndComments();

            if (HasReachedEnd) return Token.EOL;

            char currChar = _sourceCode[_currentPosition];
            int startPos = _currentPosition;

            Token newToken = new Token();
            newToken.Type = TokenType.UNKNOWN;

            /* The operator suite is quite large
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
                case '<':
                    newToken.Type = ScanLessThanOperator();
                    if (_chainAssignNumber > 0)
                    {
                        // <n| and <n|? assign n variables to the left, n needs to be kept for the parser to know.
                        newToken.Text = _chainAssignNumber.ToString();
                        newToken.Literal = _chainAssignNumber;
                        _chainAssignNumber = 0;
                    }
                    break;
                case '|':
                    newToken.Type = ScanPipe();
                    break;
                case '+':
                    if (CanLookAheadStartInclusive(2) && PeekString(2) == "++") newToken.Type = ReturnTokenTypeAndAdvance(TokenType.INCREMENT, 2);
                    else newToken.Type = ReturnTokenTypeAndAdvance(TokenType.PLUS, 1);
                    break;
                case '-':
                    if (CanLookAheadStartInclusive(2) && PeekString(2) == "--") newToken.Type = ReturnTokenTypeAndAdvance(TokenType.DECREMENT, 2);
                    else newToken.Type = ReturnTokenTypeAndAdvance(TokenType.MINUS, 1);
                    break;
                case '/':
                    newToken.Type = ReturnTokenTypeAndAdvance(TokenType.SLASH, 1);
                    break;
                case '%':
                    newToken.Type = ReturnTokenTypeAndAdvance(TokenType.PERCENT, 1);
                    break;
                case '^':
                    newToken.Type = ReturnTokenTypeAndAdvance(TokenType.CARET, 1);
                    break;
                case '*':
                    if (CanLookAheadStartInclusive(2) && PeekString(2) == "**") newToken.Type = ReturnTokenTypeAndAdvance(TokenType.EXPONENT, 2);
                    else newToken.Type = ReturnTokenTypeAndAdvance(TokenType.STAR, 1);
                    break;
                case '&':
                    if (CanLookAheadStartInclusive(2) && PeekString(2) == "&&") newToken.Type = ReturnTokenTypeAndAdvance(TokenType.AND, 2);
                    else newToken.Type = ReturnTokenTypeAndAdvance(TokenType.AMPERSAND, 1);
                    break;
                case '>':
                    if (CanLookAheadStartInclusive(2) && PeekString(2) == ">>") newToken.Type = ReturnTokenTypeAndAdvance(TokenType.BITWISE_RIGHT_SHIFT, 2);
                    else newToken.Type = ReturnTokenTypeAndAdvance(TokenType.GREATER, 1);
                    break;
                case '~':
                    if (CanLookAheadStartInclusive(2) && PeekString(2) == "~>") newToken.Type = ReturnTokenTypeAndAdvance(TokenType.COMPOSITION_PIPE, 2);
                    else newToken.Type = ReturnTokenTypeAndAdvance(TokenType.TILDE, 1);
                    break;
                case '!':
                    if (CanLookAheadStartInclusive(2) && PeekString(2) == "!=") newToken.Type = ReturnTokenTypeAndAdvance(TokenType.BANG_EQUAL, 2);
                    else newToken.Type = ReturnTokenTypeAndAdvance(TokenType.BANG, 1);
                    break;
                case '=':
                    if (CanLookAheadStartInclusive(2) && PeekString(2) == "==") newToken.Type = ReturnTokenTypeAndAdvance(TokenType.EQUAL_EQUAL, 2);
                    // => is either for function declaration or greater equal, parser will have to find out.
                    else if (CanLookAheadStartInclusive(2) && PeekString(2) == "=>") newToken.Type = ReturnTokenTypeAndAdvance(TokenType.ARROW, 2);
                    else newToken.Type = ReturnTokenTypeAndAdvance(TokenType.EQUAL, 1);
                    break;
                case '[':
                    newToken.Type = ReturnTokenTypeAndAdvance(TokenType.L_BRACE, 1);
                    break;
                case ']':
                    newToken.Type = ReturnTokenTypeAndAdvance(TokenType.R_BRACE, 1);
                    break;
                case '{':
                    newToken.Type = ReturnTokenTypeAndAdvance(TokenType.L_BRACKET, 1);
                    break;
                case '}':
                    newToken.Type = ReturnTokenTypeAndAdvance(TokenType.R_BRACKET, 1);
                    break;
                case '(':
                    newToken.Type = ReturnTokenTypeAndAdvance(TokenType.L_PAREN, 1);
                    break;
                case ')':
                    newToken.Type = ReturnTokenTypeAndAdvance(TokenType.R_PAREN, 1);
                    break;
                case ';':
                    newToken.Type = ReturnTokenTypeAndAdvance(TokenType.EOL, 1);
                    newToken.Text = ";";
                    break;
                case '\n':
                    newToken.Type = ReturnTokenTypeAndAdvance(TokenType.EOL, 1);
                    _currentLine++;
                    break;
                case '\r':
                    newToken.Type = ReturnTokenTypeAndAdvance(TokenType.EOL, 1);
                    if (_currentPosition < _sourceLength && _sourceCode[_currentPosition] == '\n')
                    {
                        _currentPosition++;
                        newToken.Text = "\r\n";
                    }
                    else
                    {
                        newToken.Text = "\r";
                    }
                    _currentLine++;
                    break;
            }

            if (newToken.Type != TokenType.UNKNOWN) return newToken;

            // Other cases done individually.

            // Used for peeking ahead, size unknown so universal.
            string peek;

            if (currChar == '.')
            {
                // Several cases here.
                // Just '.' -> Token.Dot
                // .. -> Range
                // 1.000 Decimal
                // 1.000f Float

                if (CanLookAheadStartInclusive(2))
                {
                    startPos = _currentPosition;
                    peek = PeekString(2);
                    if (peek == "..")
                    {
                        // A range.
                        newToken.Type = TokenType.DOT_DOT;
                    }
                    else if (IsNumeric(peek[1]))
                    {
                        newToken.Type = TokenType.NUMBER;
                        while (_currentPosition < _sourceLength)
                        {
                            char lastc = currChar;
                            currChar = _sourceCode[_currentPosition];
                            if (IsNumeric(currChar) || currChar == '.' || currChar == 'E' || currChar == 'e' ||
                                ((currChar == '-' || currChar == '+') && (lastc == 'E' || lastc == 'e')))
                            {
                                _currentPosition++;
                            }
                            else break;
                        }
                        // Float here.
                        if (Peek() == 'f')
                        {
                            char previousChar = _sourceCode[_currentPosition - 1];
                            if (char.IsDigit(previousChar))
                            {
                                _ = Advance();
                            }
                        }

                        string lexeme = _sourceCode.Substring(startPos, _currentPosition - startPos);
                        newToken.Text = lexeme;
                        newToken.Literal = lexeme;
                    }

                    return newToken;
                }

                // Can't look ahead, just a random dot?
                newToken.Type = TokenType.DOT;
                return newToken;
            }

            // _ is used for pipes, next char must not be an identifier if it is for a pipe.
            if (currChar == '_' && !IsIdentifier(Peek()))
            {
                newToken.Type = ReturnTokenTypeAndAdvance(TokenType.UNDERSCORE, 1);
                return newToken;
            }

            // Lex an identifier, unless an F string.
            if (IsIdentifier(currChar) && !(currChar == 'f' && Peek() == '"'))
            {
                startPos = _currentPosition;
                while (_currentPosition < _sourceCode.Length)
                {
                    if (IsIdentifier(Peek())) _currentPosition++;
                    else break;
                }

                string text = _sourceCode.Substring(startPos, _currentPosition - startPos);
                if (FluenceKeywords.IsAKeyword(text))
                {
                    newToken.Type = FluenceKeywords.GetTokenTypeFromKeyword(text);
                }
                else
                {
                    newToken.Text = text;
                    newToken.Type = TokenType.IDENTIFIER;
                }

                newToken.Literal = text;

                return newToken;
            }

            // Unless it is something alien, this should be the last match, otherwise 
            // TokenType.Unknown will be thrown.
            if (currChar == 'f' && Peek() == '"')
            {
                Advance(); // consume 'f'
                Advance(); // consume '"'
                return ScanString(startPos, true);
            }
            else if (currChar == '"')
            {
                Advance(); // consume '"'
                return ScanString(startPos, false);
            }

            newToken.Type = TokenType.UNKNOWN;
            newToken.Text = _sourceCode.Substring(startPos, _currentPosition - startPos);
            return newToken;
        }

        private Token ScanString(int startPos, bool isFString = false)
        {
            while (Peek() != '"' && !HasReachedEnd)
            {
                if (Peek() == '\n') _currentLine++;

                if (Peek() == '\\')
                {
                    Advance(); // Consume the '\'
                               // Consume the character being escaped (e.g., the '"' in '\"')
                }
                Advance();
            }

            if (HasReachedEnd)
            {
                // We ran out of code before finding the closing quote.
                // Error here.
            }

            Advance(); // Consume the closing quote "

            // Extract the full text (including quotes) and the inner value.
            string lexeme = _sourceCode.Substring(startPos, _currentPosition - startPos);

            string literalValue = _sourceCode.Substring(startPos + 1, _currentPosition - startPos - 2);

            TokenType type = isFString ? TokenType.F_STRING : TokenType.STRING;

            return new Token(type, lexeme, literalValue);
        }

        private TokenType ScanPipe()
        {
            // |>>=
            // |??, |>>, |~>,
            // ||, |>, |?
            // |

            if (CanLookAheadStartInclusive(4))
                if (PeekString(4) == "|>>=") return ReturnTokenTypeAndAdvance(TokenType.REDUCER_PIPE, 4);

            if (CanLookAheadStartInclusive(3))
            {
                string threeChar = PeekString(3);

                switch (threeChar)
                {
                    case "|??": return ReturnTokenTypeAndAdvance(TokenType.GUARD_PIPE, 3);
                    case "|>>": return ReturnTokenTypeAndAdvance(TokenType.MAP_PIPE, 3);
                    case "|~>": return ReturnTokenTypeAndAdvance(TokenType.SCAN_PIPE, 3);
                }
            }

            if (CanLookAheadStartInclusive(2))
            {
                string twoChar = PeekString(2);

                switch (twoChar)
                {
                    case "||": return ReturnTokenTypeAndAdvance(TokenType.OR, 2);
                    case "|>": return ReturnTokenTypeAndAdvance(TokenType.PIPE, 2);
                    case "|?": return ReturnTokenTypeAndAdvance(TokenType.OPTIONAL_PIPE, 2);
                }
            }

            return ReturnTokenTypeAndAdvance(TokenType.PIPE_CHAR, 1);
        }

        private TokenType ScanLessThanOperator()
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
                        return ReturnTokenTypeAndAdvance(TokenType.COLLECTIVE_OR_NOT_EQUAL, 6);
                    case "<||==|":
                        return ReturnTokenTypeAndAdvance(TokenType.COLLECTIVE_OR_EQUAL, 6);
                    case "<||??|":
                        return ReturnTokenTypeAndAdvance(TokenType.OR_GUARD_CHAIN, 6);
                }
            }

            // Now we check 4 Char operators.
            if (CanLookAheadStartInclusive(4))
            {
                string fourChar = PeekString(4);

                switch (fourChar)
                {
                    case "<==|":
                        return ReturnTokenTypeAndAdvance(TokenType.COLLECTIVE_EQUAL, 4);
                    case "<!=|":
                        return ReturnTokenTypeAndAdvance(TokenType.COLLECTIVE_NOT_EQUAL, 4);
                    case "<<=|":
                        return ReturnTokenTypeAndAdvance(TokenType.COLLECTIVE_LESS_EQUAL, 4);
                    case "<>=|":
                        return ReturnTokenTypeAndAdvance(TokenType.COLLECTIVE_GREATER_EQUAL, 4);
                    case "<??|":
                        return ReturnTokenTypeAndAdvance(TokenType.GUARD_CHAIN, 4);
                }
            }

            // <n?| and <n|
            if (CanLookAheadStartInclusive(2) && char.IsDigit(_sourceCode[_currentPosition + 1]))
            {
                _ = Advance();
                // Store the number for the Token in GetNextToken().
                _chainAssignNumber = Convert.ToInt32(ReadNumber());

                if (Match("|?"))
                {
                    // We matched <n|?
                    return ReturnTokenTypeAndAdvance(TokenType.OPTIONAL_ASSIGN_N, 0);
                }
                if (Match("|"))
                {
                    return ReturnTokenTypeAndAdvance(TokenType.CHAIN_ASSIGN_N, 0);
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
                    case "<<|": return ReturnTokenTypeAndAdvance(TokenType.COLLECTIVE_LESS, 3);
                    case "<>|": return ReturnTokenTypeAndAdvance(TokenType.COLLECTIVE_GREATER, 3);
                    case "<?|": return ReturnTokenTypeAndAdvance(TokenType.OPTIONAL_ASSIGN, 3);
                }
            }

            // Two Char operators.
            if (CanLookAheadStartInclusive(2))
            {
                string twoChar = PeekString(2);

                switch (twoChar)
                {
                    case "<=": return ReturnTokenTypeAndAdvance(TokenType.LESS_EQUAL, 2);
                    case "<<": return ReturnTokenTypeAndAdvance(TokenType.BITWISE_LEFT_SHIFT, 2);
                    case "<|": return ReturnTokenTypeAndAdvance(TokenType.REST_ASSIGN, 2);
                }
            }

            // One character here.
            return ReturnTokenTypeAndAdvance(TokenType.LESS, 1);
        }

        private char Advance() => _sourceCode[_currentPosition++];

        private bool Match(string expected)
        {
            if (!CanLookAheadStartInclusive(expected.Length)) return false;
            if (_sourceCode.Substring(_currentPosition, expected.Length) != expected) return false;
            _currentPosition += expected.Length;
            return true;
        }

        public static bool IsIdentifier(char c)
        {
            return c == '\u009F'
                || (c >= 'a' && c <= 'z')
                || (c >= 'A' && c <= 'Z')
                || (c >= '0' && c <= '9');
        }

        private char Peek() => _currentPosition >= _sourceLength ? '\0' : _sourceCode[_currentPosition];

        private string ReadNumber()
        {
            int start = _currentPosition;
            while (char.IsDigit(Peek())) _ = Advance();
            return _sourceCode.Substring(start, _currentPosition - start);
        }

        private static bool IsNumeric(char c)
        {
            return c >= '0' && c <= '9';
        }

        private TokenType ReturnTokenTypeAndAdvance(TokenType type, int length)
        {
            _currentPosition += length;
            return type;
        }

        private bool StringHasOnlyOperatorChars(int to)
        {
            for (int i = _currentPosition; i < _currentPosition + to; i++)
            {
                if (!IsOperatorChar(_sourceCode[i])) return false;
            }
            return true;
        }

        private bool IsOperatorChar(char c) =>
            c == '!' ||
            c == '%' ||
            c == '^' ||
            c == '&' ||
            c == '*' ||
            c == '-' ||
            c == '+' ||
            c == '=' ||
            c == '|' ||
            c == '/' ||
            c == '<' ||
            c == '>' ||
            c == '~' ||
            c == '?';

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
                        _currentPosition++;
                    }
                    continue;
                }

                if (IsMultiLineComment())
                {
                    _currentPosition += 2;
                    while (!HasReachedEnd)
                    {
                        if (_sourceCode[_currentPosition] != '*' && _sourceCode[_currentPosition + 1] != '#') _currentPosition++;
                        else
                        {
                            _currentPosition += 2;
                            break;
                        }
                    }
                    continue;
                }

                if (currentChar == '#')
                {
                    while (_sourceCode[_currentPosition] != '\n') _currentPosition++;
                    continue;
                }

                break;
            }
        }

        private bool IsMultiLineComment()
        {
            if (_currentPosition > _sourceLength - 1) return false;

            return _sourceCode[_currentPosition] == '#' && _sourceCode[_currentPosition + 1] == '*';
        }

        private bool IsWhiteSpace(char c) => c == ' ' || c == '\t';
    }
}