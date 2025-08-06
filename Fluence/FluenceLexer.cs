using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            Token newToken = new Token();

            /* The operator suite is quite large
             * 1 Char: + - * / % < > = ! ^ ~ | &
             * 2 Char: == != <= => || && ** is >> << |> |? <| ~> ..
             * 3 Char: |?? |>> |~> <<| <>| <n| <?| not
             * 4 Char: |>>= <==| <!=| <<=| <>=| <??| <n|?
             * 5 Char: Surprisingly none yet.
             * 6 Char: <||!=| <||==| <||??|
             */

            /* Starting With _char_
             * <        <, <=, <<, <|, <<|, <>|, <n|, <?|, <==|, <!=|, <<=|, <>=|, <??|, <n|? and all of 6 Char
             * 
             * 
             * 
             * 
             */

            // First come 6 Char Operators.
            switch (currChar)
            {
                case '<':
                    newToken.Type = ScanLessThanOperator();
                    break;
            }

            return newToken;
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
                Advance();
                string number = ReadNumber();

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

        private char Peek() => _currentPosition >= _sourceLength ? '\0' : _sourceCode[_currentPosition];

        private string ReadNumber()
        {
            int start = _currentPosition;
            while (char.IsDigit(Peek())) Advance();
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
            c == '\\' ||
            c == '<' ||
            c == '>' ||
            c == '~' ||
            c == '?';

        private string PeekString(int length) => _sourceCode.Substring(_currentPosition, length);

        private bool CanLookAheadStartInclusive(int numberOfChars = 1)
        {
            return _currentPosition + numberOfChars <= _sourceLength;
        }

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