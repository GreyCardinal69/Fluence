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
                    // => is either for function declaration or greater equal, parser will have to find out.
                    else if (CanLookAheadStartInclusive(2) && PeekString(2) == "=>") return MakeTokenAndTryAdvance(TokenType.ARROW, 2);
                    else return MakeTokenAndTryAdvance(TokenType.EQUAL, 1);
                case '[': return MakeTokenAndTryAdvance(TokenType.L_BRACKET, 1);
                case ']': return MakeTokenAndTryAdvance(TokenType.R_BRACKET, 1);
                case '{': return MakeTokenAndTryAdvance(TokenType.L_BRACE, 1);
                case '}': return MakeTokenAndTryAdvance(TokenType.R_BRACE, 1);
                case '(': return MakeTokenAndTryAdvance(TokenType.L_PAREN, 1);
                case ')': return MakeTokenAndTryAdvance(TokenType.R_PAREN, 1);
                case ';': return MakeTokenAndTryAdvance(TokenType.EOL, 1, ";");
                case ',': return MakeTokenAndTryAdvance(TokenType.COMMA, 1);
                case '\n':
                    _currentLine++;
                    return MakeTokenAndTryAdvance(TokenType.EOL, 1);
                case '\r':
                    string text = "";
                    if (_currentPosition < _sourceLength && _sourceCode[_currentPosition] == '\n')
                    {
                        _currentPosition++;
                        text = "\r\n";
                    }
                    else
                    {
                        text = "\r";
                    }
                    newToken = MakeTokenAndTryAdvance(TokenType.EOL, 1, text, text);
                    _currentLine++;
                    break;
            }

            if (newToken.Type != TokenType.UNKNOWN) return newToken;

            // Other cases done individually.

            // Used for peeking ahead, size unknown so universal.
            string peek;

            if (currChar == '.' || IsNumeric(currChar))
            {
                // Check for a range first, then number.
                if (CanLookAheadStartInclusive(2))
                {
                    peek = PeekString(2);
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

                if (IsNumeric(Peek()) || dotOnlyFraction )
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

                    if (dotOnlyFraction) lexeme = lexeme.Insert(0, "0");

                    // We already advanced in the loop and "f" check.
                    return MakeTokenAndTryAdvance(TokenType.NUMBER, 0, lexeme, lexeme);
                }

                // Can't look ahead, just a random dot?
                return MakeTokenAndTryAdvance(TokenType.DOT, 1);
            }

            // _ is used for pipes, next char must not be an identifier if it is for a pipe.
            if (currChar == '_' && !IsIdentifier(PeekNext())) return MakeTokenAndTryAdvance(TokenType.UNDERSCORE, 1);

            // Lex an identifier, unless an F string.
            if (IsIdentifier(currChar) && !(currChar == 'f' && PeekNext() == '"'))
            {
                startPos = _currentPosition;
                while (!HasReachedEnd)
                {
                    if (IsIdentifier(Peek())) _currentPosition++;
                    else break;
                }

                string text = _sourceCode.Substring(startPos, _currentPosition - startPos);
                if (FluenceKeywords.IsAKeyword(text))
                {
                    return MakeTokenAndTryAdvance(FluenceKeywords.GetTokenTypeFromKeyword(text));
                }

                return MakeTokenAndTryAdvance(TokenType.IDENTIFIER, 0, text, text);
            }

            // Unless it is something alien, this should be the last match, otherwise 
            // TokenType.Unknown will be thrown.
            if (CanLookAheadStartInclusive(2) && currChar == 'f' && PeekNext() == '"')
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
                _ = Advance();
                // Store the number for the Token in GetNextToken().
                string n = ReadNumber();
                string op;

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
                || (c == '_') // Unless by itself, should be an identifier.
                || (c >= '0' && c <= '9');
        }

        private char Peek() => _currentPosition >= _sourceLength ? '\0' : _sourceCode[_currentPosition];
        private char PeekNext() => _currentPosition + 1 >= _sourceLength ? '\0' : _sourceCode[_currentPosition + 1];

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
                        if (!CanLookAheadStartInclusive(2))
                        {
                            // error here, we are in multiline comment, yet can't look ahead two chars, so it ends as "#* .... *".
                        }
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

        private Token MakeTokenAndTryAdvance(TokenType type, int len = 0, string text = null, object lieteral = null)
        {
            _currentPosition += len;
            return new Token(type, text, lieteral);
        }

        private bool IsMultiLineComment()
        {
            if (_currentPosition > _sourceLength - 1) return false;

            return _sourceCode[_currentPosition] == '#' && _sourceCode[_currentPosition + 1] == '*';
        }

        private bool IsWhiteSpace(char c) => c == ' ' || c == '\t';
    }
}