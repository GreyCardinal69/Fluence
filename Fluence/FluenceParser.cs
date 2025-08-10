using static Fluence.FluenceByteCode;
using static Fluence.FluenceByteCode.InstructionLine;
using static Fluence.Token;

namespace Fluence
{
    // No REPL for demo.
    // Needs exception handling.
    internal sealed class FluenceParser
    {
        private readonly FluenceLexer _lexer;

        private ParseState _currentParseState;

        public List<InstructionLine> CompiledCode => _currentParseState.CodeInstructions;

        private class JumpPoint
        {
        }

        private class BackPatch
        {
        }

        private class ParseState
        {
            internal List<InstructionLine> CodeInstructions = new List<InstructionLine>();
            internal List<BackPatch> BackPatches = new List<BackPatch>();
            internal List<JumpPoint> JumpPoints = new List<JumpPoint>();

            internal int NextTempNumber = 0;

            internal void AddCodeInstruction(InstructionLine instructionLine)
            {
                CodeInstructions.Add(instructionLine);
            }

            internal void AddBackPatch(BackPatch patch)
            {
                BackPatches.Add(patch);
            }

            internal void AddJumpPoint(JumpPoint point)
            {
                JumpPoints.Add(point);
            }
        }

        internal FluenceParser(FluenceLexer lexer)
        {
            _currentParseState = new();
            _lexer = lexer;

        }

        internal void Parse()
        {
            ParseTokens();
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Terminate, null));
        }

        private void ParseTokens()
        {
            while (!_lexer.HasReachedEnd)
            {
                _lexer.TrySkipEOLToken();

                ParseStatement();
            }
        }

        private void ParseStatement()
        {
            if (_lexer.PeekNextToken().Type == TokenType.EOL)
            {
                // It's a blank line. This is a valid, empty statement.
                // Consume the EOL token and simply return. We are done with this statement.
                _lexer.ConsumeToken();
                return;
            }

            Token token = _lexer.PeekNextToken();

            bool isNotAPrimaryKeyword =
                token.Type != TokenType.IS &&
                token.Type != TokenType.NOT &&
                token.Type != TokenType.TRUE &&
                token.Type != TokenType.FALSE;

            // Primary keywords like func, if, else, return, loops, and few others.
            if (FluenceKeywords.TokenTypeIsAKeywordType(token.Type) && isNotAPrimaryKeyword)
            {
                switch (token.Type)
                {
                    case TokenType.IF:
                        ParseIfStatement();
                        break;
                }
            }
            // Most likely an expression
            else
            {
                ParseAssignment();
            }
        }

        private void ParseIfStatement()
        {
            _lexer.ConsumeToken(); // Consume the if.
            var condition = ParseExpression();

            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.GotoIfFalse, null, condition));

            int elsePatchIndex = _currentParseState.CodeInstructions.Count - 1;

            // ifs come into ways, a block body if ... { ... }
            // or one line expressions, if ... -> .... ;
            if (_lexer.PeekNextToken().Type == TokenType.L_BRACE)
            {
                ParseBlockStatement();
            }
            else  // Single line if, expects ; instead.
            {
                ConsumeAndTryThrowIfUnequal(TokenType.THIN_ARROW, "Expected '->' token for single line if/else/else-if statement");
                ParseStatement();
            }

            // Skips EOLS in between.
            while (_lexer.PeekNextToken().Type == TokenType.EOL && !_lexer.HasReachedEnd) _lexer.ConsumeToken();

            // else, also handles else if, we just consume the else part, call parse with the rest.
            if (_lexer.PeekNextToken().Type == TokenType.ELSE)
            {
                int elseIfJumpOverIndex = _currentParseState.CodeInstructions.Count;
                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Goto, null));

                _lexer.ConsumeToken();

                int elseAddress = _currentParseState.CodeInstructions.Count;
                _currentParseState.CodeInstructions[elsePatchIndex].Lhs = new NumberValue(elseAddress, NumberValue.NumberType.Integer);

                // This is an else-if, we just call ParseIf again.
                if (_lexer.PeekNextToken().Type == TokenType.IF)
                {
                    ParseStatement();
                }
                else if (_lexer.PeekNextToken().Type == TokenType.L_BRACE)
                {
                    ParseBlockStatement();
                }
                else // single line else.
                {
                    ConsumeAndTryThrowIfUnequal(TokenType.THIN_ARROW, "Expected '->' token for single line if/else/else-if statement");
                    ParseStatement();
                }

                _currentParseState.CodeInstructions[elseIfJumpOverIndex].Lhs = new NumberValue(_currentParseState.CodeInstructions.Count, NumberValue.NumberType.Integer);
            }
            else
            {
                // No other else/else-ifs.
                int endAddress = _currentParseState.CodeInstructions.Count;
                _currentParseState.CodeInstructions[elsePatchIndex].Lhs = new NumberValue(endAddress, NumberValue.NumberType.Integer);
            }
        }

        private Value ParseList()
        {
            // [ is already consumed.

            TempValue temp = new TempValue(_currentParseState.NextTempNumber++);
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.NewList, temp));

            // Empty list: [].
            if (_lexer.PeekNextToken().Type == TokenType.R_BRACKET)
            {
                _lexer.ConsumeToken(); // Consume ending bracket ].
                return temp;
            }

            // Non empty array, parse and push first element, the while loop will likely encounter a comma,
            // if not the list has just one element.
            Value firstElement = ParseExpression();
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.PushElement, temp, firstElement));

            while (_lexer.PeekNextToken().Type == TokenType.COMMA)
            {
                _lexer.ConsumeToken(); // Consume comma.

                if (_lexer.PeekNextToken().Type == TokenType.R_BRACKET) // Trailing comma in list.
                {
                    Console.WriteLine("Trailing comma in list");
                    throw new Exception();
                }

                Value rhs = ParseExpression();
                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.PushElement, temp, rhs));
            }

            _lexer.ConsumeToken(); // Consume ].

            return temp;
        }

        private void ParseBlockStatement()
        {
            ConsumeAndTryThrowIfUnequal(TokenType.L_BRACE, "Expected '{' to start a block.");
            while (_lexer.PeekNextToken().Type != TokenType.R_BRACE)
            {
                ParseStatement();
            }
            ConsumeAndTryThrowIfUnequal(TokenType.R_BRACE, "Expected '}' to end a block.");
        }

        private static bool IsAssignmentOperator(TokenType type) => type switch
        {
            TokenType.EQUAL or
            TokenType.EQUAL_DIV or
            TokenType.EQUAL_MINUS or
            TokenType.EQUAL_PLUS or
            TokenType.EQUAL_MUL or
            TokenType.EQUAL_AMPERSAND or
            TokenType.EQUAL_PERCENT => true,
            _ => false,
        };

        private static bool IsUnaryOperator(TokenType type) => type switch
        {
            TokenType.DECREMENT or
            TokenType.INCREMENT or
            TokenType.MINUS => true,
            _ => false,
        };

        private void ParseAssignment()
        {
            // (=, +=, -=, *=, /=) - LOWEST PRECEDENCE
            Value lhs = ParseExpression();

            TokenType type = _lexer.PeekNextToken().Type;

            if (IsAssignmentOperator(type))
            {
                _lexer.ConsumeToken(); // Consume the "="

                // Parse the right-hand side expression.
                Value rhs = ParseExpression();

                if (type == TokenType.EQUAL)
                {
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, lhs, rhs));
                }
                else  // Compound, -=, +=, etc.
                {
                    InstructionCode instrType = GetInstructionCode(type);

                    TempValue temp = new TempValue(_currentParseState.NextTempNumber++);

                    // temp = var - value.
                    _currentParseState.AddCodeInstruction(new InstructionLine(instrType, temp, lhs, rhs));

                    // var = temp.
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, lhs, temp));
                }
            }
        }

        private static InstructionCode GetInstructionCode(TokenType type) => type switch
        {
            // LEVEL 1: ASSIGNMENT (=, +=, -=, *=, /=) - LOWEST PRECEDENCE
            TokenType.EQUAL_PLUS => InstructionCode.Add,
            TokenType.EQUAL_MINUS => InstructionCode.Subtract,
            TokenType.EQUAL_MUL => InstructionCode.Multiply,
            TokenType.EQUAL_DIV => InstructionCode.Divide,
            TokenType.EQUAL_PERCENT => InstructionCode.Modulo,
            TokenType.EQUAL_AMPERSAND => InstructionCode.BitwiseAnd,

            //  LEVEL 2: ADDITION AND SUBTRACTION (+, -)
            TokenType.PLUS => InstructionCode.Add,
            TokenType.MINUS => InstructionCode.Subtract,

            // Precedent level 3: MULTIPLICATION, DIVISION, MODULO (*, /, %)
            TokenType.STAR => InstructionCode.Multiply,
            TokenType.SLASH => InstructionCode.Divide,
            TokenType.PERCENT => InstructionCode.Modulo,

            // Precedent level 4: EXPONENTIATION (**)
            TokenType.EXPONENT => InstructionCode.Power,

            // LEVEL 5: UNARY OPERATORS (-, ++, --)
            // TokenType.MINUS => InstructionCode.Subtract,
            TokenType.INCREMENT => InstructionCode.Increment,
            TokenType.DECREMENT => InstructionCode.Decrement,

            // Equality
            TokenType.EQUAL_EQUAL => InstructionCode.Equal,
            TokenType.BANG_EQUAL => InstructionCode.NotEqual,

            // Comparisons
            TokenType.GREATER => InstructionCode.GreaterThan,
            TokenType.LESS => InstructionCode.LessThan,
            TokenType.GREATER_EQUAL => InstructionCode.GreaterEqual,
            TokenType.LESS_EQUAL => InstructionCode.LessEqual,

            TokenType.BITWISE_LEFT_SHIFT => InstructionCode.BitwiseLShift,
            TokenType.BITWISE_RIGHT_SHIFT => InstructionCode.BitwiseRShift,
            TokenType.TILDE => InstructionCode.BitwiseNot,
            TokenType.CARET => InstructionCode.BitwiseXor,
            TokenType.PIPE_CHAR => InstructionCode.BitwiseOr,
            TokenType.AMPERSAND => InstructionCode.BitwiseAnd,

            _ => InstructionCode.Skip
        };

        private Value ParseExpression()
        {
            // for now
            return ParseLogicalOr();
        }

        private Value ParseLogicalOr()
        {
            // This calls the next higher precedence level.
            Value left = ParseLogicalAnd();

            while (_lexer.PeekNextToken().Type == TokenType.OR)
            {
                Token op = _lexer.ConsumeToken();
                Value right = ParseLogicalAnd();

                Value temp = new TempValue(_currentParseState.NextTempNumber++);

                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Or, temp, left, right, op));

                left = temp; // The result becomes the new left-hand side for the next loop.
            }

            return left;
        }

        private Value ParseLogicalAnd()
        {
            // This calls the next higher precedence level.
            Value left = ParseBitwiseOr();

            while (_lexer.PeekNextToken().Type == TokenType.AND)
            {
                Token op = _lexer.ConsumeToken();
                Value right = ParseBitwiseOr();

                Value temp = new TempValue(_currentParseState.NextTempNumber++);

                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.And, temp, left, right, op));

                left = temp; // The result becomes the new left-hand side for the next loop.
            }

            return left;
        }

        private Value ParseBitwiseOr()
        {
            // This calls the next higher precedence level.
            Value left = ParseBitwiseXor();

            // | is called PIPE_CHAR.
            while (_lexer.PeekNextToken().Type == TokenType.PIPE_CHAR)
            {
                Token op = _lexer.ConsumeToken();
                Value right = ParseBitwiseXor();

                Value temp = new TempValue(_currentParseState.NextTempNumber++);

                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.BitwiseOr, temp, left, right, op));

                left = temp; // The result becomes the new left-hand side for the next loop.
            }

            return left;
        }

        private Value ParseBitwiseXor()
        {
            // This calls the next higher precedence level.
            Value left = ParseBitwiseAnd();

            // ^
            while (_lexer.PeekNextToken().Type == TokenType.CARET)
            {
                Token op = _lexer.ConsumeToken();
                Value right = ParseBitwiseAnd();

                Value temp = new TempValue(_currentParseState.NextTempNumber++);

                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.BitwiseXor, temp, left, right, op));

                left = temp; // The result becomes the new left-hand side for the next loop.
            }

            return left;
        }

        private Value ParseBitwiseAnd()
        {
            // This calls the next higher precedence level.
            Value left = ParseEquality();

            // &
            while (_lexer.PeekNextToken().Type == TokenType.AMPERSAND)
            {
                Token op = _lexer.ConsumeToken();
                Value right = ParseEquality();

                Value temp = new TempValue(_currentParseState.NextTempNumber++);

                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.BitwiseAnd, temp, left, right, op));

                left = temp; // The result becomes the new left-hand side for the next loop.
            }

            return left;
        }

        private Value ParseEquality()
        {
            // This calls the next higher precedence level.
            Value left = ParseBitwiseShift();

            while (_lexer.PeekNextToken().Type == TokenType.EQUAL_EQUAL || _lexer.PeekNextToken().Type == TokenType.BANG_EQUAL)
            {
                Token op = _lexer.ConsumeToken();
                Value right = ParseBitwiseShift();

                Value temp = new TempValue(_currentParseState.NextTempNumber++);
                var opcode = (op.Type == TokenType.EQUAL_EQUAL)
                    ? InstructionCode.Equal
                    : InstructionCode.NotEqual;

                _currentParseState.AddCodeInstruction(new InstructionLine(opcode, temp, left, right, op));

                left = temp; // The result becomes the new left-hand side for the next loop.
            }

            return left;
        }

        private Value ParseBitwiseShift()
        {
            // This calls the next higher precedence level.
            Value left = ParseComparison();

            while (_lexer.PeekNextToken().Type == TokenType.BITWISE_LEFT_SHIFT || _lexer.PeekNextToken().Type == TokenType.BITWISE_RIGHT_SHIFT)
            {
                Token op = _lexer.ConsumeToken();
                Value right = ParseComparison();

                Value temp = new TempValue(_currentParseState.NextTempNumber++);
                var opcode = (op.Type == TokenType.BITWISE_LEFT_SHIFT)
                    ? InstructionCode.BitwiseLShift
                    : InstructionCode.BitwiseRShift;

                _currentParseState.AddCodeInstruction(new InstructionLine(opcode, temp, left, right, op));

                left = temp; // The result becomes the new left-hand side for the next loop.
            }

            return left;
        }

        private static bool IsComparisonTokenType(TokenType type) =>
            type == TokenType.GREATER ||
            type == TokenType.LESS ||
            type == TokenType.GREATER_EQUAL ||
            type == TokenType.LESS_EQUAL;

        private Value ParseComparison()
        {
            // This calls the next higher precedence level.
            Value left = ParseAdditionSubtraction();

            while (IsComparisonTokenType(_lexer.PeekNextToken().Type))
            {
                Token op = _lexer.ConsumeToken();
                Value right = ParseAdditionSubtraction();

                Value temp = new TempValue(_currentParseState.NextTempNumber++);

                _currentParseState.AddCodeInstruction(new InstructionLine(GetInstructionCode(op.Type), temp, left, right, op));

                left = temp; // The result becomes the new left-hand side for the next loop.
            }

            return left;
        }

        private Value ParseAdditionSubtraction()
        {
            // This calls the next higher precedence level.
            Value left = ParseMulDivModulo();

            while (_lexer.PeekNextToken().Type == TokenType.PLUS || _lexer.PeekNextToken().Type == TokenType.MINUS)
            {
                Token op = _lexer.ConsumeToken();
                Value right = ParseMulDivModulo();

                Value temp = new TempValue(_currentParseState.NextTempNumber++);
                var opcode = (op.Type == TokenType.PLUS)
                    ? InstructionCode.Add
                    : InstructionCode.Subtract;

                _currentParseState.AddCodeInstruction(new InstructionLine(opcode, temp, left, right, op));

                left = temp; // The result becomes the new left-hand side for the next loop.
            }

            return left;
        }

        private static bool TokenTypeIsMulDivOrModulo(TokenType type)
            => type == TokenType.PERCENT ||
               type == TokenType.STAR ||
               type == TokenType.SLASH;

        //  MULTIPLICATION, DIVISION, MODULO (*, /, %)
        private Value ParseMulDivModulo()
        {
            Value left = ParseExponentation();

            while (TokenTypeIsMulDivOrModulo(_lexer.PeekNextToken().Type))
            {
                Token op = _lexer.ConsumeToken();
                Value right = ParseExponentation();

                Value temp = new TempValue(_currentParseState.NextTempNumber++);

                _currentParseState.AddCodeInstruction(new InstructionLine(GetInstructionCode(op.Type), temp, left, right, op));

                left = temp;
            }

            return left;
        }

        private Value ParseExponentation()
        {
            Value left = ParseUnary();

            while (_lexer.PeekNextToken().Type == TokenType.EXPONENT)
            {
                Token op = _lexer.ConsumeToken();
                Value right = ParseUnary();

                Value temp = new TempValue(_currentParseState.NextTempNumber++);

                _currentParseState.AddCodeInstruction(new InstructionLine(GetInstructionCode(op.Type), temp, left, right, op));

                left = temp;
            }

            return left;
        }

        private Value ParseUnary()
        {
            Value left = ParsePostFix();

            while (IsUnaryOperator(_lexer.PeekNextToken().Type))
            {
                Token op = _lexer.ConsumeToken();
                Value right = ParsePostFix();

                Value temp = new TempValue(_currentParseState.NextTempNumber++);

                _currentParseState.AddCodeInstruction(new InstructionLine(GetInstructionCode(op.Type), temp, left, right, op));

                left = temp;
            }

            return left;
        }

        private static bool IsPostFixToken(TokenType type) =>
            type == TokenType.INCREMENT || type == TokenType.DECREMENT;

        private Value ParsePostFix()
        {
            // ++ and --
            Value left = ParseAccess();

            while (IsPostFixToken(_lexer.PeekNextToken().Type))
            {
                Token op = _lexer.ConsumeToken();

                Value one = new NumberValue(1, NumberValue.NumberType.Integer);
                Value temp = new TempValue(_currentParseState.NextTempNumber++);

                InstructionCode instrCode = (op.Type == TokenType.INCREMENT) ? InstructionCode.Add : InstructionCode.Subtract;

                _currentParseState.AddCodeInstruction(new InstructionLine(instrCode, temp, left, one, op));
                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, left, temp, null, op));

                left = temp;
            }

            return left;
        }

        private Value ParseAccess()
        {
            // Array access [], get and set.
            Value left = ParsePrimary();

            while (true)
            {
                TokenType type = _lexer.PeekNextToken().Type;

                // Access get/set.
                if (type == TokenType.L_BRACKET)
                {
                    _lexer.ConsumeToken(); // Consume [.

                    Value index = ParseExpression();

                    ConsumeAndTryThrowIfUnequal(TokenType.R_BRACKET, "Bad list access, not ending bracket.");

                    // list[...] = ...
                    if (_lexer.PeekNextToken().Type == TokenType.EQUAL)
                    {
                        TempValue temp = new TempValue(_currentParseState.NextTempNumber++);
                        _lexer.ConsumeToken(); // Consume =.

                        Value valueToSet = ParseExpression();
                        _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.SetElement, left, index, valueToSet));
                        return valueToSet;
                    }
                    else // x = list[...]
                    {
                        TempValue valueToGet = new TempValue(_currentParseState.NextTempNumber++);
                        _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.GetElement, valueToGet, left, index));
                        left = valueToGet;

                    }
                } // Property access.
                else if (type == TokenType.DOT)
                {

                } // Function call.
                else if (type == TokenType.L_PAREN)
                {

                }
                else
                {
                    break;
                }
            }

            return left;
        }

        private Value ParsePrimary()
        {
            Token token = _lexer.ConsumeToken();

            if (token.Type == TokenType.MINUS || token.Type == TokenType.BANG || token.Type == TokenType.TILDE)
            {
                Value operand = ParsePrimary();

                if (operand is NumberValue numVal)
                {
                    if (token.Type == TokenType.MINUS)
                    {
                        switch (numVal.Type)
                        {
                            case NumberValue.NumberType.Integer:
                                return new NumberValue(-Convert.ToInt32(numVal.Value), numVal.Type);
                            case NumberValue.NumberType.Float:
                                return new NumberValue(-float.Parse(numVal.Value.ToString()), numVal.Type);
                            case NumberValue.NumberType.Double:
                                return new NumberValue(-Convert.ToDouble(numVal.Value), numVal.Type);
                        }
                    }
                    else
                    {
                        return new BooleanValue((int)numVal.Value == 0);
                    }
                }

                Value temp = new TempValue(_currentParseState.NextTempNumber++);

                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Negate, temp, operand));
                return temp;
            }

            switch (token.Type)
            {
                case TokenType.IDENTIFIER: return new VariableValue(token.Text);
                case TokenType.NUMBER: return NumberValue.FromToken(token);
                case TokenType.STRING: return new StringValue(token.Text);
                case TokenType.TRUE: return new BooleanValue(true);
                case TokenType.FALSE: return new BooleanValue(false);
                case TokenType.L_BRACKET:
                    // We are in list, either initialization, or [i] access.
                    return ParseList();
            }

            if (token.Type == TokenType.L_PAREN)
            {
                Value expr = ParseExpression();
                // Unclosed parentheses.
                ConsumeAndTryThrowIfUnequal(TokenType.R_PAREN, ")");
                return expr;
            }

            // Log the token type for now, for debugging.
            Console.WriteLine(token);
            throw new Exception();
        }

        private void ConstructAndThrowParserException(int lineNum, int column, string errorMessage, string faultyLine, string expected, Token token)
        {
            ParserExceptionContext context = new ParserExceptionContext()
            {
                Column = column,
                FaultyLine = faultyLine,
                LineNum = lineNum,
                UnexpectedToken = token,
                ExpectedDescription = expected
            };
            throw new FluenceParserException(errorMessage, context);
        }

        private Token ConsumeAndTryThrowIfUnequal(TokenType expectedType, string errorMessage)
        {
            Token token = _lexer.ConsumeToken();
            if (token.Type != expectedType)
            {
                // for now just log.
                Console.WriteLine(errorMessage);
                // throw
            }
            return token;
        }
    }
}