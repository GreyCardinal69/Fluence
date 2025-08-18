using System.Text;
using static Fluence.FluenceByteCode;
using static Fluence.FluenceByteCode.InstructionLine;
using static Fluence.Token;

namespace Fluence
{
    // No REPL for demo.
    // Needs exception handling.
    internal sealed class FluenceParser
    {
        private FluenceLexer _lexer;
        // Used to lex special, tiny bits of code, like expressions in f-string.
        private FluenceLexer _auxLexer;
        // Used for parsing default field values in first pass.
        private FluenceLexer _fieldLexer;

        private readonly ParseState _currentParseState;

        internal FluenceScope CurrentParserStateGlobalScope => _currentParseState.GlobalScope;

        public List<InstructionLine> CompiledCode => _currentParseState.CodeInstructions;

        private class LoopContext
        {
            internal List<int> ContinuePatchAddresses { get; } = new List<int>();
            internal List<int> BreakPatchAddresses { get; } = new List<int>();

            internal LoopContext() { }
        }

        private class MatchContext
        {
            internal List<int> BreakPatches { get; } = new List<int>();
        }

        private class ParseState
        {
            internal List<InstructionLine> CodeInstructions = new List<InstructionLine>();
            internal Stack<LoopContext> ActiveLoopContexts = new Stack<LoopContext>();
            internal Stack<MatchContext> ActiveMatchContexts = new Stack<MatchContext>();
            internal List<InstructionLine> FunctionVariableDeclarations = new List<InstructionLine>();

            internal StructSymbol CurrentStructContext { get; set; } = null;

            internal FluenceScope GlobalScope { get; } = new FluenceScope();
            internal FluenceScope CurrentScope { get; set; } = new FluenceScope();
            internal Dictionary<string, FluenceScope> NameSpaces { get; } = new();

            internal int NextTempNumber = 0;
            internal int CurrentTempNumber => NextTempNumber - 1;

            internal void AddFunctionVariableDeclaration(InstructionLine instructionLine)
            {
                FunctionVariableDeclarations.Add(instructionLine);
            }

            internal void AddCodeInstruction(InstructionLine instructionLine)
            {
                CodeInstructions.Add(instructionLine);
            }

            internal void InsertFunctionVariableDeclarations()
            {
                CodeInstructions.InsertRange(CodeInstructions.Count - 1, FunctionVariableDeclarations);
            }
        }

        internal FluenceParser(FluenceLexer lexer)
        {
            _currentParseState = new();
            _currentParseState.CurrentScope = _currentParseState.GlobalScope;
            _lexer = lexer;
        }

        internal void DumpSymbolTables()
        {
            StringBuilder sb = new StringBuilder("------------------------------------\n\nGenerated Symbol Hierarchy:\n\n");

            // Dump the global scope first
            DumpScope(sb, _currentParseState.GlobalScope, "Global Scope", 0);

            // If there are any namespaces, dump them as separate top-level scopes
            if (_currentParseState.NameSpaces.Any())
            {
                sb.AppendLine(); // Add a separator
                foreach (var ns in _currentParseState.NameSpaces)
                {
                    DumpScope(sb, ns.Value, $"Namespace: {ns.Key}", 0);
                }
            }

            sb.AppendLine("------------------------------------");
            Console.WriteLine(sb.ToString());
        }

        /// <summary>
        /// A recursive helper to dump the contents of a single scope and its children.
        /// </summary>
        /// <param name="sb">The StringBuilder to append to.</param>
        /// <param name="scope">The scope to dump.</param>
        /// <param name="scopeName">The display name for this scope.</param>
        /// <param name="indentationLevel">The current level of indentation.</param>
        private void DumpScope(StringBuilder sb, FluenceScope scope, string scopeName, int indentationLevel)
        {
            string indent = new string(' ', indentationLevel * 4);

            sb.Append(indent).Append(scopeName).AppendLine(" {");

            if (!scope.Symbols.Any())
            {
                sb.Append(indent).AppendLine("    (empty)");
            }
            else
            {
                // Dump all symbols within the current scope
                foreach (var item in scope.Symbols)
                {
                    DumpSymbol(sb, item.Key, item.Value, indentationLevel + 1);
                }
            }

            sb.Append(indent).AppendLine("}");
        }

        /// <summary>
        /// Helper to dump a single symbol's details with proper indentation.
        /// </summary>
        private void DumpSymbol(StringBuilder sb, string symbolName, Symbol symbol, int indentationLevel)
        {
            string indent = new string(' ', indentationLevel * 4);
            string innerIndent = new string(' ', (indentationLevel + 1) * 4);

            switch (symbol)
            {
                case EnumSymbol enumSymbol:
                    sb.Append(indent).Append($"Symbol: {symbolName}, type Enum {{").AppendLine();
                    foreach (var member in enumSymbol.Members)
                    {
                        sb.Append(innerIndent).Append(member.Value.MemberName).Append(", ").Append(member.Value.Value).AppendLine();
                    }
                    sb.Append(indent).AppendLine("}");
                    break;

                case FunctionSymbol functionSymbol:
                    sb.Append(indent).Append($"Symbol: {symbolName}, type Function Header {{ Arity: {functionSymbol.Arity}, StartAddress: {FluenceDebug.FormatByteCodeAddress(functionSymbol.StartAddress)} }}").AppendLine();
                    break;

                case StructSymbol structSymbol:
                    sb.Append(indent).Append($"Symbol: {symbolName}, type Struct {{").AppendLine();
                    sb.Append(innerIndent).Append("Fields: ").Append(string.Join(", ", structSymbol.Fields)).AppendLine(".");

                    if (structSymbol.Functions.Any())
                    {
                        sb.Append(innerIndent).AppendLine("Functions: {");
                        foreach (var function in structSymbol.Functions)
                        {
                            sb.Append(innerIndent).Append($"    Name: {function.Key}, Arity: {function.Value.Arity}, Start Address: {FluenceDebug.FormatByteCodeAddress(function.Value.StartAddress)}").AppendLine();
                        }
                        sb.Append(innerIndent).AppendLine("}");
                    }

                    sb.Append("\tDefault Values of Fields:\n");
                    foreach (var item in structSymbol.DefaultFieldValuesAsTokens)
                    {
                        sb.Append($"\t\t{item.Key} : {string.Join(", ", item.Value)}\n");
                    }

                    FunctionValue constructor = structSymbol.Constructor;
                    if (constructor != null)
                    {
                        sb.Append(innerIndent).Append($"Constructor: {constructor.Name} {{ Arity: {constructor.Arity}, Start Address: {FluenceDebug.FormatByteCodeAddress(constructor.StartAddress)} }}").AppendLine();
                    }

                    sb.Append(indent).AppendLine("}");
                    break;
            }
        }

        internal void Parse()
        {
            ParseTokens();

            _currentParseState.AddCodeInstruction(
                new InstructionLine(
                    InstructionCode.CallFunction,
                    new TempValue(_currentParseState.NextTempNumber++),
                    new VariableValue("Main"),
                    new NumberValue(0)
                )
            );

            _currentParseState.InsertFunctionVariableDeclarations();

            // We add a universal TERMINATE instruction for the VM, at the very end of the generated byte code.
            // Both for convenience and so that we dont end on dangling instructions, like add and any other.
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Terminate, null));
        }

        internal void AddNameSpace(FluenceScope nameSpace)
        {
            _currentParseState.NameSpaces.TryAdd(nameSpace.Name, nameSpace);
        }

        private void ParseTokens()
        {
            _lexer.LexFullSource();
            _lexer.RemoveLexerEOLS();

#if DEBUG
            _lexer.DumpTokenStream("Initial Token Stream (Before Pre-Parsing declarations)");
#endif

            ParseDeclarations(0, _lexer.TokenCount);

#if DEBUG
            _lexer.DumpTokenStream("Token stream after parsing declarations.");
#endif

            while (!_lexer.HasReachedEnd)
            {
                if (_lexer.HasReachedEnd) break;

                // We reached end of file, so we just quit.
                if (_lexer.PeekNextToken().Type == TokenType.EOF)
                {
                    _lexer.ConsumeToken();
                    break;
                }

                ParseStatement();
            }
        }

        private void ParseDeclarations(int start, int end)
        {
            int currentIndex = start;
            while (currentIndex < end)
            {
                Token token = _lexer.PeekAheadByN(currentIndex + 1);
                if (token.Type == TokenType.EOF) break;

                if (token.Type == TokenType.SPACE)
                {
                    int namespaceNameIndex = currentIndex + 1;
                    int namespaceEndIndex = FindMatchingBrace(namespaceNameIndex);

                    string namespaceName = _lexer.PeekAheadByN(namespaceNameIndex + 1).Text;
                    FluenceScope parentScope = _currentParseState.CurrentScope;
                    FluenceScope namespaceScope = new FluenceScope(parentScope, namespaceName);
                    _currentParseState.NameSpaces.TryAdd(namespaceName, namespaceScope);
                    _currentParseState.CurrentScope = namespaceScope;

                    ParseDeclarations(namespaceNameIndex + 2, namespaceEndIndex + 1);

                    _currentParseState.CurrentScope = parentScope;

                    currentIndex = namespaceEndIndex + 1;
                    continue;
                }

                if (token.Type == TokenType.ENUM)
                {
                    int declarationStartIndex = currentIndex;
                    int declarationEndIndex = FindEnumStructDeclarationEnd(declarationStartIndex);

                    ParseEnumDeclaration(declarationStartIndex, declarationEndIndex);

                    int count = declarationEndIndex - declarationStartIndex + 1;

                    _lexer.RemoveTokenRange(declarationStartIndex, count);
                    continue;
                }
                else if (token.Type == TokenType.FUNC)
                {
                    int declarationStartIndex = currentIndex;
                    int declarationEndIndex = FindFunctionHeaderDeclarationEnd(declarationStartIndex);

                    ParseFunctionHeaderDeclaration(declarationStartIndex, declarationEndIndex);

                    int functionEndIndex = FindFunctionBodyEnd(declarationEndIndex);
                    currentIndex = functionEndIndex + 1;
                    continue;
                }
                else if (token.Type == TokenType.STRUCT)
                {
                    int declarationStartIndex = currentIndex;
                    int declarationEndIndex = FindStructDeclarationEnd(declarationStartIndex);
                    ParseStructDeclaration(declarationStartIndex, declarationEndIndex);
                    currentIndex = declarationEndIndex;
                    continue;
                }
                currentIndex++;


            }
        }

        private int FindMatchingBrace(int startIndex)
        {
            int currentIndex = startIndex;

            while (_lexer.PeekAheadByN(currentIndex + 1).Type != TokenType.L_BRACE)
            {
                if (_lexer.PeekAheadByN(currentIndex + 1).Type == TokenType.EOF)
                {
                    throw new Exception($"Syntax Error: Could not find opening brace '{{' to start block after index {startIndex}.");
                }
                currentIndex++;
            }

            currentIndex++;
            int braceDepth = 1;

            while (braceDepth > 0)
            {
                if (currentIndex >= _lexer.TokenCount)
                {
                    throw new Exception("Syntax Error: Unclosed block. Reached end of file while looking for matching '}'.");
                }

                Token currentToken = _lexer.PeekAheadByN(currentIndex + 1);

                switch (currentToken.Type)
                {
                    case TokenType.L_BRACE:
                        braceDepth++;
                        break;
                    case TokenType.R_BRACE:
                        braceDepth--;
                        break;
                    case TokenType.EOF:
                        throw new Exception("Syntax Error: Unclosed block. Reached end of file while looking for matching '}'.");
                }

                if (braceDepth == 0)
                {
                    return currentIndex;
                }

                currentIndex++;
            }

            throw new Exception($"Internal Parser Error: Failed to find matching brace starting from index {startIndex}.");
        }

        private int FindFunctionHeaderDeclarationEnd(int startIndex)
        {
            int currentIndex = startIndex + 1;
            while (true)
            {
                Token token = _lexer.PeekAheadByN(currentIndex + 1);

                if (token.Type == TokenType.ARROW)
                {
                    return currentIndex;
                }

                if (token.Type == TokenType.EOF)
                {
                    throw new Exception("Syntax Error: Unclosed enum or function declaration.");
                }

                currentIndex++;
            }
        }

        private int FindEnumStructDeclarationEnd(int startIndex)
        {
            int currentIndex = startIndex + 1;
            while (true)
            {
                Token token = _lexer.PeekAheadByN(currentIndex + 1);

                if (token.Type == TokenType.R_BRACE)
                {
                    return currentIndex;
                }

                if (token.Type == TokenType.EOF)
                {
                    throw new Exception("Syntax Error: Unclosed enum or struct declaration.");
                }

                currentIndex++;
            }
        }

        private int FindStructDeclarationEnd(int startIndex)
        {
            int currentIndex = startIndex + 1;
            Token bodyStartToken = _lexer.PeekAheadByN(currentIndex + 2); // Skip struct + Name.
            currentIndex += 2;

            if (bodyStartToken.Type == TokenType.L_BRACE)
            {
                int braceDepth = 1;
                currentIndex++; // Move past the opening '{'

                while (braceDepth > 0)
                {
                    Token currentToken = _lexer.PeekAheadByN(currentIndex);

                    if (currentToken.Type == TokenType.L_BRACE)
                    {
                        braceDepth++;
                    }
                    else if (currentToken.Type == TokenType.R_BRACE)
                    {
                        braceDepth--;
                    }
                    else if (currentToken.Type == TokenType.EOF)
                    {
                        // This is a syntax error.
                        throw new Exception($"Syntax Error: Unclosed Struct body. Reached end of file while looking for matching '}}'.");
                    }

                    // If we've found the final closing brace, we're done.
                    if (braceDepth == 0)
                    {
                        return currentIndex;
                    }

                    currentIndex++;
                }
            }

            return currentIndex;
        }

        private int FindFunctionBodyEnd(int startIndex)
        {
            int currentIndex = startIndex + 1;
            Token bodyStartToken = _lexer.PeekAheadByN(currentIndex + 1);

            if (bodyStartToken.Type == TokenType.L_BRACE)
            {
                int braceDepth = 1;
                currentIndex++; // Move past the opening '{'

                while (braceDepth > 0)
                {
                    Token currentToken = _lexer.PeekAheadByN(currentIndex + 1);

                    if (currentToken.Type == TokenType.L_BRACE)
                    {
                        braceDepth++;
                    }
                    else if (currentToken.Type == TokenType.R_BRACE)
                    {
                        braceDepth--;
                    }
                    else if (currentToken.Type == TokenType.EOF)
                    {
                        // This is a syntax error.
                        throw new Exception($"Syntax Error: Unclosed function body. Reached end of file while looking for matching '}}'.");
                    }

                    // If we've found the final closing brace, we're done.
                    if (braceDepth == 0)
                    {
                        return currentIndex;
                    }

                    currentIndex++;
                }
            }

            int parenDepth = 0;
            while (true)
            {
                Token currentToken = _lexer.PeekAheadByN(currentIndex + 1);

                if (currentToken.Type == TokenType.L_PAREN) parenDepth++;
                else if (currentToken.Type == TokenType.R_PAREN) parenDepth--;

                // A semicolon (EOL) only terminates the statement if we are not inside parentheses.
                if (currentToken.Type == TokenType.EOL && parenDepth == 0)
                {
                    return currentIndex;
                }

                if (currentToken.Type == TokenType.EOF)
                {
                    throw new Exception("Syntax Error: Unterminated expression body for function. Reached end of file.");
                }

                currentIndex++;
            }
        }

        private void ParseFunctionHeaderDeclaration(int startTokenIndex, int endTokenIndex)
        {
            Token nameToken = _lexer.PeekAheadByN(startTokenIndex + 1 + 1);
            string funcName = nameToken.Text;

            int arity = 0;
            // Start scanning for members after the opening '('.
            // `func` `Name` `(`
            int currentIndex = startTokenIndex + 3;

            while (currentIndex < endTokenIndex)
            {
                Token currentToken = _lexer.PeekAheadByN(currentIndex + 1);

                if (currentToken.Type == TokenType.IDENTIFIER)
                {
                    arity++;
                    currentIndex++;
                }
                else if (currentToken.Type == TokenType.COMMA || currentToken.Type == TokenType.R_PAREN)
                {
                    currentIndex++;
                }

                currentIndex++;
            }

            FunctionSymbol functionSymbol = new FunctionSymbol(funcName, arity, -1);

            if (!_currentParseState.CurrentScope.Declare(funcName, functionSymbol))
            {
                // error here, duplicate function.
            }

            _currentParseState.CurrentScope.Declare(funcName, functionSymbol);
        }

        private void ParseStructDeclaration(int startTokenIndex, int endTokenIndex)
        {
            string structName = _lexer.PeekAheadByN(startTokenIndex + 2).Text;
            StructSymbol structSymbol = new StructSymbol(structName);

            for (int i = startTokenIndex + 3; i < endTokenIndex; i++)
            {
                Token token = _lexer.PeekAheadByN(i + 1);

                if (token.Type == TokenType.IDENTIFIER)
                {
                    Token next = _lexer.PeekAheadByN(i + 2);

                    // TO DO, as of now just checks for x; y; declaration of fields.
                    // Alternatively, what about x = 0; Default initialization?
                    // Must do further checks later.
                    if (next.Type == TokenType.EQUAL)
                    {
                        structSymbol.Fields.Add(token.Text);

                        int statementEndIndex = i + 2;
                        while (statementEndIndex < endTokenIndex && _lexer.PeekAheadByN(statementEndIndex + 1).Type != TokenType.EOL)
                        {
                            statementEndIndex++;
                        }

                        List<Token> defaultValueTokens = new List<Token>();
                        for (int z = i + 3; z < statementEndIndex + 1; z++)
                        {
                            defaultValueTokens.Add(_lexer.PeekAheadByN(z));
                        }
                        i = statementEndIndex;

                        structSymbol.DefaultFieldValuesAsTokens.TryAdd(token.Text, defaultValueTokens);
                    }
                }
                else if (token.Type == TokenType.FUNC)
                {
                    Token nameToken = _lexer.PeekAheadByN(i + 2);

                    int currentIndex = i + 3;
                    Token next = _lexer.PeekAheadByN(currentIndex);

                    if (next.Type == TokenType.L_PAREN)
                    {
                        currentIndex++;
                        string funcName = nameToken.Text;

                        int arity = 0;
                        while (_lexer.PeekAheadByN(currentIndex).Type != TokenType.R_PAREN)
                        {
                            Token currentToken = _lexer.PeekAheadByN(currentIndex);

                            if (currentToken.Type == TokenType.IDENTIFIER)
                            {
                                arity++;
                                currentIndex++;

                            }
                            else if (currentToken.Type == TokenType.COMMA)
                            {
                                currentIndex++;
                            }
                            else if (currentToken.Type == TokenType.ARROW)
                            {
                                break;
                            }

                            currentIndex++;
                        }

                        FunctionValue functionValue = new FunctionValue(funcName, arity, -1, "");

                        if (structSymbol.Functions.TryGetValue(funcName, out FunctionValue functionValue1))
                        {
                            if (functionValue1.Arity == functionValue.Arity)
                            {
                                // Error, duplicate function.
                            }
                        }
                        i = currentIndex;

                        if (funcName == "init")
                        {
                            structSymbol.Constructor = functionValue;
                        }
                        else
                        {
                            structSymbol.Functions.Add(funcName, functionValue);
                        }
                    }
                }
            }

            if (!_currentParseState.CurrentScope.Declare(structName, structSymbol))
            {
                // error here, duplicate struct declaration.
            }
            _currentParseState.CurrentScope.Declare(structName, structSymbol);
        }

        private void ParseEnumDeclaration(int startTokenIndex, int endTokenIndex, bool inGlobal = false)
        {
            Token nameToken = _lexer.PeekAheadByN(startTokenIndex + 1 + 1);
            string enumName = nameToken.Text;
            var enumSymbol = new EnumSymbol(enumName);

            int currentValue = 0;
            // Start scanning for members after the '{'.
            // `enum` `Name` `{`
            int currentIndex = startTokenIndex + 3;

            while (currentIndex < endTokenIndex)
            {
                Token currentToken = _lexer.PeekAheadByN(currentIndex + 1);

                if (currentToken.Type == TokenType.IDENTIFIER)
                {
                    string memberName = currentToken.Text;
                    if (enumSymbol.Members.ContainsKey(memberName))
                    {
                        throw new Exception($"Syntax Error: Duplicate enum member '{memberName}'.");
                    }

                    var enumValue = new EnumValue(enumName, memberName, currentValue);
                    enumSymbol.Members.Add(memberName, enumValue);
                    currentValue++;
                }
                else if (currentToken.Type != TokenType.COMMA && currentToken.Type != TokenType.EOL)
                {
                    throw new Exception($"Syntax Error: Unexpected token '{currentToken.Type}' in enum body.");
                }

                currentIndex++;
            }

            if (inGlobal && !_currentParseState.GlobalScope.Declare(enumName, enumSymbol))
            {
                _currentParseState.GlobalScope.Declare(enumName, enumSymbol);
                return;
            }

            if (!_currentParseState.CurrentScope.Declare(enumName, enumSymbol))
            {
                // error here, duplicate enum declaration.
            }

            _currentParseState.CurrentScope.Declare(enumName, enumSymbol);
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
                token.Type != TokenType.FALSE &&
                token.Type != TokenType.NIL;

            Token next;

            // Primary keywords like func, if, else, return, loops, and few others.
            if (FluenceKeywords.TokenTypeIsAKeywordType(token.Type) && isNotAPrimaryKeyword)
            {
                switch (token.Type)
                {
                    case TokenType.IF:
                        ParseIfStatement();
                        break;
                    case TokenType.LOOP:
                        ParseLoopStatement();
                        break;
                    case TokenType.EOF:
                        return;
                    case TokenType.BREAK:
                        _lexer.ConsumeToken(); // Consume break;

                        if (_currentParseState.ActiveLoopContexts.Count == 0) { /* throw error: 'break' outside loop */ }
                        LoopContext currentLoop = _currentParseState.ActiveLoopContexts.Peek();

                        _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Goto, null!));
                        currentLoop.BreakPatchAddresses.Add(_currentParseState.CodeInstructions.Count - 1);

                        next = _lexer.PeekNextToken();


                        if (next.Type == TokenType.EOF)
                        {
                            return;
                        }

                        // If we reach here, then we lack a semicolon, most likely at the end of an expression,
                        // not within if/loops/etc. Or we have a bug.
                        ConsumeAndExpect(TokenType.EOL, $"Syntax Error: Missing newline or ';' to terminate the statement. Line {_lexer.CurrentLine}");
                        break;
                    case TokenType.CONTINUE:
                        _lexer.ConsumeToken(); // Consume 'continue'
                        if (_currentParseState.ActiveLoopContexts.Count == 0) { /* throw error */ }

                        LoopContext currentLoop2 = _currentParseState.ActiveLoopContexts.Peek();

                        _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Goto, null!));

                        currentLoop2.ContinuePatchAddresses.Add(_currentParseState.CodeInstructions.Count - 1);

                        next = _lexer.PeekNextToken();


                        if (next.Type == TokenType.EOF)
                        {
                            return;
                        }

                        // If we reach here, then we lack a semicolon, most likely at the end of an expression,
                        // not within if/loops/etc. Or we have a bug.
                        ConsumeAndExpect(TokenType.EOL, $"Syntax Error: Missing newline or ';' to terminate the statement. Line {_lexer.CurrentLine}");
                        break;
                    case TokenType.WHILE:
                        ParseWhileStatement();
                        break;
                    case TokenType.FOR:
                        ParseForStatement();
                        break;
                    case TokenType.FUNC:
                        ParseFunction();
                        break;
                    case TokenType.RETURN:
                        ParseReturnStatement();
                        break;
                    case TokenType.MATCH:
                        ParseMatchStatement();
                        break;
                    case TokenType.STRUCT:
                        ParseStructStatement();
                        break;
                    case TokenType.SELF:
                        ParseAssignment(); // Self is really just a variable.
                        break;
                    case TokenType.SPACE:  // In the second pass, we don't create a new namespace. We just enter it.
                        ConsumeAndExpect(TokenType.SPACE, "Expected 'space'.");
                        Token nameToken = ConsumeAndExpect(TokenType.IDENTIFIER, "Expected namespace name.");
                        string namespaceName = nameToken.Text;
                        ConsumeAndExpect(TokenType.L_BRACE, "Expected '{'.");

                        // --- THE FIX ---
                        // Get the PRE-EXISTING scope that was created during ParseDeclarations.
                        if (!_currentParseState.NameSpaces.TryGetValue(namespaceName, out FluenceScope namespaceScope))
                        {
                            throw new Exception($"Internal Compiler Error: Namespace '{namespaceName}' was not found in the symbol table during the second pass.");
                        }

                        // Temporarily enter the namespace scope
                        FluenceScope parentScope = _currentParseState.CurrentScope;
                        _currentParseState.CurrentScope = namespaceScope;

                        // Parse all statements inside the block
                        while (_lexer.PeekNextToken().Type != TokenType.R_BRACE && !_lexer.HasReachedEnd)
                        {
                            ParseStatement();
                        }
                        ConsumeAndExpect(TokenType.R_BRACE, "Expected '}'.");

                        // Restore the parent scope
                        _currentParseState.CurrentScope = parentScope;
                        break;
                    case TokenType.USE:
                        ParseUseStatement();
                        break;
                }
            }
            // Most likely an expression
            else
            {
                ParseAssignment();

                next = _lexer.PeekNextToken();


                if (next.Type == TokenType.EOF)
                {
                    return;
                }

                // If we reach here, then we lack a semicolon, most likely at the end of an expression,
                // not within if/loops/etc. Or we have a bug.
                ConsumeAndExpect(TokenType.EOL, $"Syntax Error: Missing newline or ';' to terminate the statement. Line {_lexer.CurrentLine}");
            }
        }

        private void ParseStructStatement()
        {
            // This is basically a second pass of structs.
            // On the first pass we create the symbol table.
            // Fields, methods, init.
            // Now we only seek to generate bytecode of the functions and
            // Patch start addresses.

            // TO DO, currently can't have functions with the same name, even if different arity.

            _lexer.ConsumeToken(); // Consume struct.
            string structName = _lexer.ConsumeToken().Text; // Consume name;
            _lexer.ConsumeToken(); // Consume {

            StructSymbol structSymbol;
            _currentParseState.CurrentScope.TryResolve(structName, out Symbol symbol);
            structSymbol = (StructSymbol)symbol;

            _currentParseState.CurrentStructContext = structSymbol;

            // Empty struct.
            if (_lexer.PeekNextToken().Type == TokenType.R_BRACE)
            {
                _currentParseState.CurrentStructContext = null;
                _lexer.ConsumeToken();
                return;
            }

            int currentIndex = 1;
            while (true)
            {
                Token currentToken = _lexer.PeekNextToken();

                if (currentToken.Type == TokenType.FUNC)
                {
                    bool isInit = _lexer.PeekAheadByN(2).Text == "init";
                    ParseFunction(true, isInit, structName);
                }
                else
                {
                    _lexer.ConsumeToken();
                    currentIndex++;
                }

                // End of struct body.
                if (currentToken.Type == TokenType.R_BRACE)
                {
                    break;
                }
            }

            _currentParseState.CurrentStructContext = null;
        }

        private void ParseForStatement()
        {
            _lexer.ConsumeToken(); // Consume for.

            // For x in ... statement.
            if (_lexer.PeekAheadByN(2).Type == TokenType.IN)
            {
                ParseForInStatement();
            }
            else
            {
                ParseForCStyleStatement();
            }
        }

        private void ParseForCStyleStatement()
        {
            ParseStatement();

            Value condition = ParseExpression();
            int conditionCheckIndex = _currentParseState.CodeInstructions.Count;
            _lexer.ConsumeToken(); // Consume the ;

            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.GotoIfFalse, null!, condition));
            int loopExitPatchIndex = _currentParseState.CodeInstructions.Count - 1;

            // Add a placeholder jump to skip over the incrementer and go directly to the loop body.
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Goto, null!));
            int loopBodyJumpPatchIndex = _currentParseState.CodeInstructions.Count - 1;

            // Part 3: Incrementer (e.g., "i += 1")
            // This is where 'continue' statements will jump to.
            int incrementerStartIndex = _currentParseState.CodeInstructions.Count;
            ParseAssignment();
            _lexer.ConsumeToken(); // Consume the ;

            // After the incrementer runs, add a jump back to the condition check.
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Goto, new NumberValue(conditionCheckIndex - 1, NumberValue.NumberType.Integer)));

            var loopContext = new LoopContext();
            _currentParseState.ActiveLoopContexts.Push(loopContext);

            int bodyStartIndex = _currentParseState.CodeInstructions.Count;
            if (_lexer.PeekNextToken().Type == TokenType.L_BRACE)
            {
                ParseBlockStatement();
            }
            else
            {
                ConsumeAndExpect(TokenType.THIN_ARROW, "Expected '->' token for single line for loop statement");
                ParseStatement();
            }

            // At the end of the body, add a jump to the incrementer.
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Goto, new NumberValue(incrementerStartIndex, NumberValue.NumberType.Integer)));

            // Patch the jump that skips the incrementer to now point to the body.
            _currentParseState.CodeInstructions[loopBodyJumpPatchIndex].Lhs = new NumberValue(bodyStartIndex, NumberValue.NumberType.Integer);

            // The loop officially ends at the current instruction count.
            int loopEndAddress = _currentParseState.CodeInstructions.Count;

            // Patch the main exit jump to point to the instruction after the loop.
            _currentParseState.CodeInstructions[loopExitPatchIndex].Lhs = new NumberValue(loopEndAddress, NumberValue.NumberType.Integer);

            // Patch all 'break' statements to also jump to the end of the loop.
            foreach (var patchIndex in loopContext.BreakPatchAddresses)
            {
                _currentParseState.CodeInstructions[patchIndex].Lhs = new NumberValue(loopEndAddress, NumberValue.NumberType.Integer);
            }

            foreach (var patchIndex in loopContext.ContinuePatchAddresses)
            {
                _currentParseState.CodeInstructions[patchIndex].Lhs = new NumberValue(incrementerStartIndex, NumberValue.NumberType.Integer);
            }

            _currentParseState.ActiveLoopContexts.Pop();
        }

        private void ParseForInStatement()
        {
            Token itemToken = ConsumeAndExpect(TokenType.IDENTIFIER, "Expected loop variable name after 'for'.");
            VariableValue loopVariable = new VariableValue(itemToken.Text);

            ConsumeAndExpect(TokenType.IN, "Expected 'in' keyword in for-loop.");

            Value collectionExpr = ParseExpression();

            // Create hidden variables for the index and the collection copy.
            TempValue indexVar = new TempValue(_currentParseState.NextTempNumber++, "ForInIndex");
            TempValue collectionVar = new TempValue(_currentParseState.NextTempNumber++, "ForInCollectionCopy");

            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, indexVar, new NumberValue(0, NumberValue.NumberType.Integer)));
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, collectionVar, collectionExpr));

            int loopTopAddress = _currentParseState.CodeInstructions.Count;

            var loopContext = new LoopContext();
            _currentParseState.ActiveLoopContexts.Push(loopContext);

            TempValue lengthVar = new TempValue(_currentParseState.NextTempNumber++, "ForInCollectionLen");
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.GetLength, lengthVar, collectionVar));
            TempValue conditionVar = new TempValue(_currentParseState.NextTempNumber++);
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.LessThan, conditionVar, indexVar, lengthVar));

            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.GotoIfFalse, null!, conditionVar));
            int loopExitPatchIndex = _currentParseState.CodeInstructions.Count - 1;

            // Assign the loop variable: `item = collection[index]`
            TempValue currentElementVar = new TempValue(_currentParseState.NextTempNumber++);
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.GetElement, currentElementVar, collectionVar, indexVar));
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, loopVariable, currentElementVar));

            // ForIn has two forms, block {...} or ForIn cond -> ...;
            if (_lexer.PeekNextToken().Type == TokenType.L_BRACE)
            {
                ParseBlockStatement();
            }
            else
            {
                ConsumeAndExpect(TokenType.THIN_ARROW, "Expected '->' token for single line if/else/else-if statement");
                ParseStatement();
            }

            // Increment the index: `index = index + 1`
            TempValue incrementedIndex = new TempValue(_currentParseState.NextTempNumber++);
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Add, incrementedIndex, indexVar, new NumberValue(1)));
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, indexVar, incrementedIndex));

            // Unconditional jump back to the top to re-check the condition.
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Goto, new NumberValue(loopTopAddress)));

            int loopEndAddress = _currentParseState.CodeInstructions.Count;
            _currentParseState.CodeInstructions[loopExitPatchIndex].Lhs = new NumberValue(loopEndAddress);

            foreach (var patchIndex in loopContext.BreakPatchAddresses)
            {
                _currentParseState.CodeInstructions[patchIndex].Lhs = new NumberValue(loopEndAddress);
            }

            int continueAddress = loopEndAddress - 3;
            foreach (var patchIndex in loopContext.ContinuePatchAddresses)
            {
                _currentParseState.CodeInstructions[patchIndex].Lhs = new NumberValue(continueAddress);
            }

            _currentParseState.ActiveLoopContexts.Pop();
        }

        private void ParseWhileStatement()
        {
            _lexer.ConsumeToken(); // Consume while.

            Value condition = ParseExpression();

            int loopStartIndex = _currentParseState.CodeInstructions.Count;
            LoopContext whileContext = new LoopContext();
            _currentParseState.ActiveLoopContexts.Push(whileContext);

            // the condition of the while, we must jump back here if loop reaches end ( not terminated by break ).
            // We'll patch this later.
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.GotoIfFalse, null!, condition));
            int loopExitPatch = _currentParseState.CodeInstructions.Count - 1;

            // While has two forms, block {...} or while cond -> ...;
            if (_lexer.PeekNextToken().Type == TokenType.L_BRACE)
            {
                ParseBlockStatement();
            }
            else
            {
                ConsumeAndExpect(TokenType.THIN_ARROW, "Expected '->' token for single line if/else/else-if statement");
                ParseStatement();
            }

            // We jump to the start of the loop, which is the condition check.
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Goto, new NumberValue(loopStartIndex - 1, NumberValue.NumberType.Integer)));

            int loopEndIndex = _currentParseState.CodeInstructions.Count;

            // Patch our GoToIfFalse.
            _currentParseState.CodeInstructions[loopExitPatch].Lhs = new NumberValue(loopEndIndex, NumberValue.NumberType.Integer);

            // Assign breakPatches to the end of the loop, or the instruction after if there is more code.
            foreach (int breakPatch in whileContext.BreakPatchAddresses)
            {
                _currentParseState.CodeInstructions[breakPatch].Lhs = new NumberValue(loopEndIndex, NumberValue.NumberType.Integer);
            }

            int continueAddress = loopStartIndex;
            // Patch all 'continue' statements to jump to the top.
            foreach (var patchIndex in whileContext.ContinuePatchAddresses)
            {
                _currentParseState.CodeInstructions[patchIndex].Lhs = new NumberValue(continueAddress);
            }
            _currentParseState.ActiveLoopContexts.Pop();
        }

        private void ParseLoopStatement()
        {
            _lexer.ConsumeToken(); // Consume loop

            int loopStartIndex = _currentParseState.CodeInstructions.Count;
            LoopContext loopContext = new LoopContext();
            _currentParseState.ActiveLoopContexts.Push(loopContext);

            ParseBlockStatement();

            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Goto, new NumberValue(loopStartIndex, NumberValue.NumberType.Integer)));

            int loopEndIndex = _currentParseState.CodeInstructions.Count;
            foreach (int breakPatch in loopContext.BreakPatchAddresses)
            {
                _currentParseState.CodeInstructions[breakPatch].Lhs = new NumberValue(loopEndIndex, NumberValue.NumberType.Integer);
            }

            foreach (var patchIndex in loopContext.ContinuePatchAddresses)
            {
                _currentParseState.CodeInstructions[patchIndex].Lhs = new NumberValue(loopStartIndex, NumberValue.NumberType.Integer);
            }
            _currentParseState.ActiveLoopContexts.Pop();
        }

        private void ParseIfStatement()
        {
            _lexer.ConsumeToken(); // Consume the if.
            var condition = ParseTernary();

            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.GotoIfFalse, null!, condition));

            int elsePatchIndex = _currentParseState.CodeInstructions.Count - 1;

            // ifs come into ways, a block body if ... { ... }
            // or one line expressions, if ... -> .... ;
            if (_lexer.PeekNextToken().Type == TokenType.L_BRACE)
            {
                ParseBlockStatement();
            }
            else  // Single line if, expects ; instead.
            {
                ConsumeAndExpect(TokenType.THIN_ARROW, "Expected '->' token for single line if/else/else-if statement");
                ParseStatement();
            }

            // Skips EOLS in between.
            while (_lexer.PeekNextToken().Type == TokenType.EOL && !_lexer.HasReachedEnd) _lexer.ConsumeToken();

            // else, also handles else if, we just consume the else part, call parse with the rest.
            if (_lexer.PeekNextToken().Type == TokenType.ELSE)
            {
                int elseIfJumpOverIndex = _currentParseState.CodeInstructions.Count;
                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Goto, null!));

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
                    ConsumeAndExpect(TokenType.THIN_ARROW, "Expected '->' token for single line if/else/else-if statement");
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

        private void ParseReturnStatement()
        {
            _lexer.ConsumeToken(); // Consume return;

            Value result = null;

            // return;
            if (_lexer.PeekNextToken().Type == TokenType.EOL)
            {
                result = new NilValue();
            }
            else
            {
                result = ParseExpression();
            }

            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Return, result));
        }

        private void ParseFunction(bool inStruct = false, bool isInit = false, string structName = null)
        {
            _lexer.ConsumeToken(); // Consume func.

            Token nameToken = ConsumeAndExpect(TokenType.IDENTIFIER, "Expected function name.");
            string functionName = nameToken.Text;

            ConsumeAndExpect(TokenType.L_PAREN, "Expected '(' after function name.");

            List<string> parameters = new List<string>();
            // Has arguments
            if (_lexer.PeekNextToken().Type != TokenType.R_PAREN)
            {
                while (_lexer.PeekNextToken().Type == TokenType.IDENTIFIER)
                {
                    parameters.Add(_lexer.ConsumeToken().Text);
                    if (_lexer.PeekNextToken().Type == TokenType.COMMA)
                    {
                        _lexer.ConsumeToken();
                    }
                }
            }
            ConsumeAndExpect(TokenType.R_PAREN, "No closing parenthesis '}' after function args.");
            ConsumeAndExpect(TokenType.ARROW, "No arrow after function declaration");

            int functionStartAddress;
            if (inStruct)
            {
                functionStartAddress = _currentParseState.CodeInstructions.Count + 1;
            }
            else
            {
                functionStartAddress = _currentParseState.CodeInstructions.Count + 1;
            }

            FunctionValue func = new FunctionValue(functionName, parameters.Count, functionStartAddress);

            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Goto, null!));
            int jumpOverBodyGoTo = _currentParseState.CodeInstructions.Count - 1;

            if (inStruct)
            {
                if (!_currentParseState.CurrentScope.TryResolve(structName, out Symbol symbol) || !(symbol is StructSymbol structSymbol))
                {
                    throw new Exception($"Internal Compiler Error: Could not find struct '{structName}' in current scope '{_currentParseState.CurrentScope.Name}'.");
                }

                if (isInit)
                {
                    if (structSymbol.Constructor == null)
                    {
                        throw new Exception($"Internal Compiler Error: Constructor for '{structName}' was not found in the symbol table.");
                    }
                    foreach (var field in structSymbol.DefaultFieldValuesAsTokens)
                    {
                        string fieldName = field.Key;
                        List<Token> expressionTokens = field.Value;

                        _fieldLexer = _lexer;
                        _lexer = new FluenceLexer(expressionTokens);

                        Value defaultValueResult = ParseTernary();

                        _lexer = _fieldLexer; // Restore the main lexer

                        _currentParseState.AddCodeInstruction(
                            new InstructionLine(
                                InstructionCode.SetField,
                                new VariableValue("self"),
                                new StringValue(fieldName),
                                defaultValueResult // This will be the TempValue from the 'Add' instruction
                            )
                        );
                    }
                    structSymbol.Constructor.SetStartAddress(functionStartAddress);
                    _currentParseState.AddFunctionVariableDeclaration(new InstructionLine(InstructionCode.Assign, new VariableValue($"{structName}.{structSymbol.Constructor.Name}"), structSymbol.Constructor));
                }
                else
                {
                    if (!structSymbol.Functions.TryGetValue(functionName, out FunctionValue functionValue))
                    {
                        throw new Exception($"Internal Compiler Error: Method '{functionName}' for struct '{structName}' was not found in the symbol table.");
                    }
                    functionValue.SetStartAddress(functionStartAddress);
                    _currentParseState.AddFunctionVariableDeclaration(new InstructionLine(InstructionCode.Assign, new VariableValue($"{structName}.{functionValue.Name}"), functionValue));
                }
            }
            else // !inStruct
            {
                // This will also work for functions defined in the current scope.
                if (!_currentParseState.CurrentScope.TryResolve(functionName, out Symbol symbol) || !(symbol is FunctionSymbol functionSymbol))
                {
                    throw new Exception($"Internal Compiler Error: Could not find function '{functionName}' in current scope '{_currentParseState.CurrentScope.Name}'.");
                }

                functionSymbol.SetStartAddress(functionStartAddress);
                _currentParseState.AddFunctionVariableDeclaration(new InstructionLine(InstructionCode.Assign, new VariableValue($"{func.Name}"), func));
            }

            // Either => for one line, or => {...} for a block.
            if (_lexer.PeekNextToken().Type == TokenType.L_BRACE)
            {
                ParseBlockStatement();
                if (_currentParseState.CodeInstructions[^1].Instruction != InstructionCode.Return)
                {
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Return, new NilValue()));
                }
            }
            else
            {
                // func Test() =>, this format is just for returning something.
                Value returnValue = ParseExpression();
                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Return, returnValue));
            }

            int afterBodyAddress = _currentParseState.CodeInstructions.Count;
            _currentParseState.CodeInstructions[jumpOverBodyGoTo].Lhs = new NumberValue(afterBodyAddress);
        }

        private Value ParseMatchStatement()
        {
            if (_lexer.PeekNextToken().Type == TokenType.MATCH)
            {
                // If we have lhs = match x
                // Then match falls to ParsePrimary(), which consumes it.
                // If it is just match x {...}, match token remains, so we consume it.
                _lexer.ConsumeToken();
            }

            Value matchOn = ParseTernary();

            ConsumeAndExpect(TokenType.L_BRACE, "Missing opening [ on match.");

            if (IsSwitchStyleMatch())
            {
                ParseMatchSwitchStyle(matchOn);
                return new NilValue();
            }
            // Check for match x { } empty match.
            else if (_lexer.PeekAheadByN(2).Type == TokenType.R_BRACE)
            {
                return new NilValue();
            }
            else
            {
                return ParseMatchExpressionStyle(matchOn);
            }
        }

        private void ParseMatchSwitchStyle(Value matchOn)
        {
            MatchContext context = new MatchContext();
            _currentParseState.ActiveMatchContexts.Push(context);

            List<int> nextCasePatches = new List<int>();
            bool fallThrough = false;
            int fallThroughSkipIndex = -1;

            while (_lexer.PeekNextToken().Type != TokenType.R_BRACE)
            {
                if (_lexer.PeekNextToken().Type == TokenType.EOL)
                {
                    // in an indented match there will be eols between cases, skip them.
                    _lexer.ConsumeToken();
                    continue;
                }

                int nextCaseAddress = _currentParseState.CodeInstructions.Count;
                // Patch all fall-throughs from the previous case to jump here.
                foreach (var patch in nextCasePatches)
                {
                    _currentParseState.CodeInstructions[patch].Lhs = new NumberValue(nextCaseAddress);
                }
                nextCasePatches.Clear();

                if (_lexer.PeekNextToken().Type == TokenType.REST)
                {
                    _lexer.ConsumeToken();
                }
                else
                {
                    Value pattern = ParseExpression();

                    TempValue condition = new TempValue(_currentParseState.NextTempNumber++);
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Equal, condition, matchOn, pattern));
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.GotoIfFalse, null!, condition));

                    if (fallThrough)
                    {
                        _currentParseState.CodeInstructions[fallThroughSkipIndex].Lhs = new NumberValue(_currentParseState.CodeInstructions.Count);
                        fallThrough = false;
                        fallThroughSkipIndex = 0;
                    }

                    nextCasePatches.Add(_currentParseState.CodeInstructions.Count - 1);
                }

                ConsumeAndExpect(TokenType.COLON, "Expected ':' after match case pattern.");

                // Parse the body after the colon
                while (_lexer.PeekNextToken().Type != TokenType.R_BRACE && _lexer.PeekNextToken().Type != TokenType.REST)
                {
                    if (_lexer.PeekNextToken().Type == TokenType.BREAK)
                    {
                        _lexer.ConsumeToken();
                        _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Goto, null!));
                        context.BreakPatches.Add(_currentParseState.CodeInstructions.Count - 1);
                        break;
                    }
                    else if (_lexer.PeekAheadByN(2).Type == TokenType.COLON)
                    {
                        // We have fallthrough cases here, we make this goto, but fill it in the next iteration.
                        // This skips the next cases equal and gotoIfFalse instructions.
                        _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Goto, null!));
                        fallThroughSkipIndex = _currentParseState.CodeInstructions.Count - 1;
                        fallThrough = true;
                        break;
                    }
                    ParseStatement();
                }

                nextCasePatches.Add(_currentParseState.CodeInstructions.Count - 1);
            }

            ConsumeAndExpect(TokenType.R_BRACE, "Expecting closing } after match");

            _currentParseState.ActiveMatchContexts.Pop();

            int matchEndAddress = _currentParseState.CodeInstructions.Count;

            foreach (var patch in nextCasePatches)
            {
                _currentParseState.CodeInstructions[patch].Lhs = new NumberValue(matchEndAddress);
            }

            foreach (var patch in context.BreakPatches)
            {
                _currentParseState.CodeInstructions[patch].Lhs = new NumberValue(matchEndAddress);
            }
        }

        private Value ParseMatchExpressionStyle(Value matchOn)
        {
            TempValue result = new TempValue(_currentParseState.NextTempNumber++);

            List<int> endJumpPatches = new List<int>();
            bool hasRestCase = false;

            while (_lexer.PeekNextToken().Type != TokenType.R_BRACE)
            {
                if (_lexer.PeekNextToken().Type == TokenType.EOL)
                {
                    // in an indented match there will be eols between cases, skip them.
                    _lexer.ConsumeToken();
                    continue;
                }

                if (hasRestCase)
                {
                    // error, can't define after rest.
                }

                int nextCasePatch = -1;

                if (_lexer.PeekNextToken().Type == TokenType.REST)
                {
                    hasRestCase = true;
                    _lexer.ConsumeToken(); // Consume the rest.
                    // Nothing else, final else, no comparison.
                }
                else
                {
                    Value pattern = ParseTernary();

                    TempValue condition = new TempValue(_currentParseState.NextTempNumber++);
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Equal, condition, matchOn, pattern));

                    // We'll patch later
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.GotoIfFalse, null!, condition));
                    nextCasePatch = _currentParseState.CodeInstructions.Count - 1;
                }

                Value caseResult = null;

                bool generatedAssign = false;

                if (_lexer.PeekNextToken().Type == TokenType.THIN_ARROW)
                {
                    ConsumeAndExpect(TokenType.THIN_ARROW, "Missing thin arrow in match case.");

                    caseResult = ParseTernary();
                }
                else
                {
                    ConsumeAndExpect(TokenType.ARROW, "Missing arrow in match case.");
                    int instructionCountBeforeBlock = _currentParseState.CodeInstructions.Count;
                    ParseBlockStatement();

                    // Validation: The block MUST have generated at least one instruction, and it MUST be a Return.
                    if (_currentParseState.CodeInstructions.Count == instructionCountBeforeBlock ||
                        _currentParseState.CodeInstructions[^1].Instruction != InstructionCode.Return)
                    {
                        throw new Exception("Syntax Error: A block body '=> { ... }' in a match expression must end with a 'return' statement.");
                    }

                    Value returnedValue = _currentParseState.CodeInstructions[^1].Lhs;
                    generatedAssign = true;

                    // Parseblock ends with a return statement, but we don't want to exit function, just the block,
                    // so we replace it with an assign instruction instead.
                    _currentParseState.CodeInstructions[^1] = new InstructionLine(InstructionCode.Assign, result, returnedValue);
                }

                if (!generatedAssign)
                {
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, result, caseResult));
                }

                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Goto, null!));
                endJumpPatches.Add(_currentParseState.CodeInstructions.Count - 1);

                if (nextCasePatch != -1)
                {
                    int nextCaseAddress = _currentParseState.CodeInstructions.Count;
                    _currentParseState.CodeInstructions[nextCasePatch].Lhs = new NumberValue(nextCaseAddress);
                }

                ConsumeAndExpect(TokenType.EOL, "Expecting ; after expression or block body in match cases.");
            }

            ConsumeAndExpect(TokenType.R_BRACE, "Expecting closing } after match.");

            if (!hasRestCase)
            {
                // error, rest is a must.
            }

            int matchEndAddress = _currentParseState.CodeInstructions.Count;

            foreach (var endJump in endJumpPatches)
            {
                _currentParseState.CodeInstructions[endJump].Lhs = new NumberValue(matchEndAddress);
            }

            return result;
        }

        private Value ConcatenateStringValues(Value left, Value right)
        {
            if (left == null) return right;
            if (right == null) return left;

            TempValue temp = new TempValue(_currentParseState.NextTempNumber++);
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Add, temp, left, right));
            return temp;
        }

        private Value ParseFString(object literal)
        {
            // f".... {var} ....".
            string str = literal.ToString();

            Value result = null;
            int lastIndex = 0;

            while (lastIndex < str.Length)
            {
                int exprStart = str.IndexOf('{', lastIndex);

                if (exprStart == -1)
                {
                    string end = str[lastIndex..];
                    result = ConcatenateStringValues(result, new StringValue(end));
                    break;
                }

                string startOfLiteral = str[lastIndex..exprStart];
                if (!string.IsNullOrEmpty(startOfLiteral))
                {
                    result = ConcatenateStringValues(result, new StringValue(startOfLiteral));
                }

                int exprClose = str.IndexOf('}', exprStart);

                // no closing }
                if (exprClose == -1)
                {
                    // error?
                }

                // skip { at start, } at end.
                string expr = str.Substring(exprStart + 1, exprClose - exprStart - 1);

                // We push our main code's token stream into the aux lexer,
                // In the current lexer we put the {expr}, parse it, then we switch back.
                _auxLexer = _lexer;
                _lexer = new FluenceLexer(expr);

                Value exprInside = ResolveValue(ParseTernary());
                _lexer = _auxLexer;

                TempValue temp = new TempValue(_currentParseState.NextTempNumber++);
                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.ToString, ResolveValue(temp), exprInside));

                result = ConcatenateStringValues(result, temp);

                lastIndex = exprClose + 1;
            }

            return result ?? new StringValue("");
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

            _lexer.ConsumeToken(); // Consume ]

            return temp;
        }

        private void ParseUseStatement()
        {
            _lexer.ConsumeToken();

            Token nameToken = _lexer.ConsumeToken();
            string namespaceName = nameToken.Text;

            if (!_currentParseState.NameSpaces.ContainsKey(namespaceName))
            {
                // Error, unknown space.
            }

            FluenceScope namespaceToUse = _currentParseState.NameSpaces[namespaceName];

            foreach (var entry in namespaceToUse.Symbols)
            {
                string key = entry.Key;
                Symbol symbol = entry.Value;

                if (_currentParseState.CurrentScope.TryGetLocalSymbol(key, out Symbol _))
                {
                    // error conflicting symbols.
                }

                _currentParseState.CurrentScope.Declare(key, symbol);
            }

            ConsumeAndExpect(TokenType.EOL, "Expected ';' or newline after 'use' statement.");
        }

        private void ParseBlockStatement()
        {
            ConsumeAndExpect(TokenType.L_BRACE, "Expected '{' to start a block.");
            while (_lexer.PeekNextToken().Type != TokenType.R_BRACE)
            {
                ParseStatement();
            }
            ConsumeAndExpect(TokenType.R_BRACE, "Expected '}' to end a block.");
        }

        private void ParseAssignment()
        {
            List<Value> lhs = ParseChainAssignmentLhs();

            // Multi-Assign operators like .+=, .-= and so on.
            if (IsMultiCompoundAssignmentOperator(_lexer.PeekNextToken().Type))
            {
                ParseMultiCompoundAssignment(lhs);
                return;
            }

            TokenType type = _lexer.PeekNextToken().Type;

            if (IsChainAssignmentOperator(type))
            {
                if (type == TokenType.SEQUENTIAL_REST_ASSIGN || type == TokenType.OPTIONAL_SEQUENTIAL_REST_ASSIGN)
                {
                    ParseSequentialRestAssign(lhs);
                    return;
                }

                ParseChainAssignment(lhs);
                return;
            }

            Value left = lhs[0];

            if (IsSimpleAssignmentOperator(type) || type == TokenType.SWAP)
            {
                _lexer.ConsumeToken(); // Consume the "="

                // Parse the right-hand side expression.
                Value rhs = ResolveValue(ParseTernary());

                if (type == TokenType.EQUAL)
                {
                    if (left is VariableValue)
                    {
                        _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, left, rhs));
                    }
                    else if (left is ElementAccessValue access)
                    {
                        // This is a "write" operation. Generate SetElement.
                        _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.SetElement, ResolveValue(access.Target), access.Index, rhs));
                    }
                    else if (left is PropertyAccessValue propAccess)
                    {
                        Value resolvedTarget = ResolveValue(propAccess.Target);
                        _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.SetField, resolvedTarget, new StringValue(propAccess.FieldName), rhs));
                    }
                }
                else if (type == TokenType.SWAP)
                {
                    Value temp = new TempValue(_currentParseState.NextTempNumber++);

                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, temp, left));
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, left, rhs));
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, rhs, temp));
                }
                else  // Compound, -=, +=, etc.
                {
                    InstructionCode instrType = GetInstructionCode(type);

                    TempValue temp = new TempValue(_currentParseState.NextTempNumber++);

                    // temp = var - value.
                    _currentParseState.AddCodeInstruction(new InstructionLine(instrType, temp, left, rhs));

                    // var = temp.
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, left, temp));
                }
            }
            else
            {
                // In Fluence the statement variable; is valid, but it would be ignored.
                // We should generate bytecode regardless. That would return StatementCompleteValue.
                // It represents nothing so we just skip here.
                if (left is StatementCompleteValue)
                {
                    // do nothing
                }
                else if (left is ElementAccessValue val)
                {
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.SetElement, val.Target, val.Index, new NilValue()));
                }
                else if (left is VariableValue variable)
                {
                    // The expression was just a variable. Force a read.
                    var temp = new TempValue(_currentParseState.NextTempNumber++);
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, temp, variable));
                }
            }
        }

        private void ParseMultiCompoundAssignment(List<Value> leftSides)
        {
            Token opToken = _lexer.ConsumeToken();

            InstructionCode operation = GetInstructionCodeForMultiCompoundAssignment(opToken.Type);

            var rhsList = new List<Value>();
            do
            {
                rhsList.Add(ParseExpression());
            } while (ConsumeTokenIfMatch(TokenType.COMMA));

            if (leftSides.Count != rhsList.Count)
            {
                throw new Exception("Syntax Error: Mismatched number of targets and values in multi-compound assignment.");
            }

            for (int i = 0; i < leftSides.Count; i++)
            {
                Value lhs = leftSides[i];
                Value rhs = rhsList[i];

                Value resolvedRhs = ResolveValue(rhs);
                Value resolvedLhs = ResolveValue(lhs);

                TempValue temp = new TempValue(_currentParseState.NextTempNumber++);
                _currentParseState.AddCodeInstruction(new InstructionLine(operation, temp, resolvedLhs, resolvedRhs));

                if (lhs is ElementAccessValue val)
                {
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.SetElement, val.Target, val.Index, temp));
                }
                else if (resolvedLhs is VariableValue variable)
                {
                    TempValue newTemp = new TempValue(_currentParseState.NextTempNumber++);
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, lhs, temp));
                }
                else if (resolvedLhs is PropertyAccessValue propAccess)
                {
                    Value resolvedTarget = ResolveValue(propAccess.Target);
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.SetField, resolvedTarget, new StringValue(propAccess.FieldName), temp));
                }
            }
        }

        private List<Value> ParseChainAssignmentLhs()
        {
            List<Value> lhs = new List<Value>();

            if (IsBroadCastPipeFunctionCall())
            {
                Value functionToCall = ParsePrimary();
                _lexer.ConsumeToken(); // Consume ).

                List<Value> args = new List<Value>();
                int underscoreIndex = -1;
                int currentIndex = 0;

                do
                {
                    if (_lexer.PeekNextToken().Type == TokenType.UNDERSCORE)
                    {
                        _lexer.ConsumeToken();
                        if (underscoreIndex != -1) ; // error here.
                        underscoreIndex = currentIndex;
                        args.Add(new NilValue());
                    }
                    else
                    {
                        args.Add(ParseTernary());
                    }
                    currentIndex++;
                } while (ConsumeTokenIfMatch(TokenType.COMMA));

                ConsumeAndExpect(TokenType.R_PAREN, "Expecting closing ) in function call.");

                if (underscoreIndex != -1) ; // error here.
                lhs.Add(new BroadcastCallTemplate(functionToCall, args, underscoreIndex));
                return lhs;
            }

            do
            {
                lhs.Add(ParseTernary());
            } while (ConsumeTokenIfMatch(TokenType.COMMA));

            return lhs;
        }

        private void ParseSequentialRestAssign(List<Value> lhs)
        {
            int lhsIndex = 0;
            Value firstLhs = lhs[0];
            Token op = _lexer.ConsumeToken();

            bool isOptional = op.Type == TokenType.OPTIONAL_SEQUENTIAL_REST_ASSIGN;

            // Sequential assign operators do not expect any other pipes.
            do
            {
                Value rhs = ParseTernary();

                TempValue valueToAssign = new TempValue(_currentParseState.NextTempNumber++);
                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, valueToAssign, rhs));

                int skipOptionalAssign = -1;
                if (isOptional)
                {
                    TempValue isNil = new TempValue(_currentParseState.NextTempNumber++);
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Equal, isNil, valueToAssign, new NilValue()));
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.GotoIfTrue, null, isNil));
                    skipOptionalAssign = _currentParseState.CodeInstructions.Count - 1;
                }

                if (lhsIndex < lhs.Count)
                {
                    if (lhs[lhsIndex] is ElementAccessValue val)
                    {
                        _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.SetElement, val.Target, val.Index, valueToAssign));
                    }
                    else if (lhs[lhsIndex] is PropertyAccessValue propAccess)
                    {
                        Value resolvedTarget = propAccess.Target;

                        _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.SetField, resolvedTarget, new StringValue(propAccess.FieldName), valueToAssign));
                    }
                    else
                    {
                        _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, lhs[lhsIndex], valueToAssign));
                    }
                    lhsIndex++;
                }


                if (skipOptionalAssign != -1)
                {
                    _currentParseState.CodeInstructions[skipOptionalAssign].Lhs = new NumberValue(_currentParseState.CodeInstructions.Count);
                }
            }
            while (ConsumeTokenIfMatch(TokenType.COMMA));
        }

        private void ParseChainAssignment(List<Value> lhs)
        {
            int lhsIndex = 0;
            Value firstLhs = lhs[0];

            if (firstLhs is BroadcastCallTemplate broadcastCall)
            {
                // Although weird, there are cases when you could pipe multiple broadcast calls.
                // For example when you want to print some values, but print only those that are not nil.
                while (IsChainAssignmentOperator(_lexer.PeekNextToken().Type))
                {

                    if (lhs.Count > 1)
                    {
                        // error, many functions.
                    }

                    Token op = _lexer.ConsumeToken(); // Consume <| or <?|.

                    if (op.Type != TokenType.REST_ASSIGN || op.Type != TokenType.OPTIONAL_REST_ASSIGN)
                    {
                        // Error, wrong operand.
                    }

                    bool isOptional = op.Type == TokenType.OPTIONAL_REST_ASSIGN;

                    List<Value> rhs = new List<Value>();

                    do
                    {
                        rhs.Add(ParseTernary());
                    } while (ConsumeTokenIfMatch(TokenType.COMMA));

                    if (rhs.Count == 0)
                    {
                        // error here. Nothing to broadcast.
                    }

                    foreach (var arg in rhs)
                    {
                        int skipOptionalAssign = -1;
                        if (isOptional)
                        {
                            TempValue isNil = new TempValue(_currentParseState.NextTempNumber++);
                            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Equal, isNil, arg, new NilValue()));
                            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.GotoIfTrue, null, isNil));
                            skipOptionalAssign = _currentParseState.CodeInstructions.Count - 1;
                        }

                        broadcastCall.Arguments[broadcastCall.PlaceholderIndex] = arg;

                        foreach (var item in broadcastCall.Arguments)
                        {
                            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.PushParam, item));
                        }

                        TempValue temp = new TempValue(_currentParseState.NextTempNumber++);
                        _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.CallFunction, temp, broadcastCall.Callable, new NumberValue(broadcastCall.Arguments.Count)));

                        if (skipOptionalAssign != -1)
                        {
                            _currentParseState.CodeInstructions[skipOptionalAssign].Lhs = new NumberValue(_currentParseState.CodeInstructions.Count);
                        }
                    }
                }
            }
            else
            {

                while (IsChainAssignmentOperator(_lexer.PeekNextToken().Type))
                {
                    Token op = _lexer.ConsumeToken();

                    bool isOptional = op.Type == TokenType.OPTIONAL_REST_ASSIGN || op.Type == TokenType.OPTIONAL_ASSIGN_N;

                    Value rhs = ParseTernary();

                    TempValue valueToAssign = new TempValue(_currentParseState.NextTempNumber++);
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, valueToAssign, rhs));

                    int skipOptionalAssign = -1;
                    if (isOptional)
                    {
                        TempValue isNil = new TempValue(_currentParseState.NextTempNumber++);
                        _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Equal, isNil, valueToAssign, new NilValue()));
                        _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.GotoIfTrue, null, isNil));
                        skipOptionalAssign = _currentParseState.CodeInstructions.Count - 1;
                    }

                    if (op.Type == TokenType.CHAIN_ASSIGN_N || op.Type == TokenType.OPTIONAL_ASSIGN_N)
                    {
                        int count = Convert.ToInt32(op.Literal);
                        for (int i = 0; i < count; i++)
                        {
                            if (lhs[lhsIndex] is ElementAccessValue val)
                            {
                                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.SetElement, val.Target, val.Index, valueToAssign));
                            }
                            else if (lhs[lhsIndex] is PropertyAccessValue propAccess)
                            {
                                Value resolvedTarget = ResolveValue(propAccess.Target);
                                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.SetField, resolvedTarget, new StringValue(propAccess.FieldName), valueToAssign));
                            }
                            else
                            {
                                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, lhs[lhsIndex], valueToAssign));
                            }
                            lhsIndex++;
                        }
                    }
                    else
                    {
                        while (lhsIndex < lhs.Count)
                        {
                            if (lhs[lhsIndex] is ElementAccessValue val)
                            {
                                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.SetElement, val.Target, val.Index, valueToAssign));
                            }
                            else if (lhs[lhsIndex] is PropertyAccessValue propAccess)
                            {
                                Value resolvedTarget = ResolveValue(propAccess.Target);
                                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.SetField, resolvedTarget, new StringValue(propAccess.FieldName), valueToAssign));
                            }
                            else
                            {
                                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, lhs[lhsIndex], valueToAssign));
                            }
                            lhsIndex++;
                        }
                    }

                    if (skipOptionalAssign != -1)
                    {
                        _currentParseState.CodeInstructions[skipOptionalAssign].Lhs = new NumberValue(_currentParseState.CodeInstructions.Count);
                    }
                }
            }
        }

        private Value ParseTernary()
        {
            // If Ternary, this becomes the condition.
            Value left = ParsePipe();

            TokenType type = _lexer.PeekNextToken().Type;

            // Two formats, normal: cond ? a : b
            // Joint: cond ?: a, b
            if (type == TokenType.TERNARY_JOINT || type == TokenType.QUESTION)
            {
                _lexer.ConsumeToken(); // Consume '?' or '?:'

                // Immediately generate the conditional jump. We will back-patch its target.
                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.GotoIfFalse, null!, left));
                int falseJumpPatch = _currentParseState.CodeInstructions.Count - 1;

                Value trueExpr = ParseTernary();

                TempValue result = new TempValue(_currentParseState.NextTempNumber++);
                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, result, trueExpr));

                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Goto, null!));
                int endJumpPatch = _currentParseState.CodeInstructions.Count - 1;

                int falsePathAddress = _currentParseState.CodeInstructions.Count;
                _currentParseState.CodeInstructions[falseJumpPatch].Lhs = new NumberValue(falsePathAddress);

                // 7. Consume the ':' or ',' delimiter.
                if (type == TokenType.QUESTION)
                {
                    ConsumeAndExpect(TokenType.COLON, "Expected ':' in standard ternary.");
                }
                else
                {
                    ConsumeAndExpect(TokenType.COMMA, "Expected ',' in Fluid-style ternary.");
                }

                // Recursively parse the "false" path expression.

                Value falseExpr = ParseTernary();
                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, result, falseExpr));

                int endAddress = _currentParseState.CodeInstructions.Count;
                _currentParseState.CodeInstructions[endJumpPatch].Lhs = new NumberValue(endAddress);

                // The "value" of this entire ternary expression for the rest of the parser
                // is the temporary variable that holds the chosen result.
                return result;
            }

            return left;
        }

        private Value ParsePipe()
        {
            Value left = ParseExpression();

            // While we see a pipe, parse it and try again.
            while (_lexer.PeekNextToken().Type == TokenType.PIPE)
            {
                _lexer.ConsumeToken(); // Consume the pipe.

                left = ParsePipedFunctionCall(left);
            }

            return left;
        }

        private Value ParsePipedFunctionCall(Value leftSidePipedValue)
        {
            Value targetFunction = ParsePrimary();
            ConsumeAndExpect(TokenType.L_PAREN, "Expected a function opening ( for call.");

            List<Value> args = new List<Value>();

            while (ConsumeTokenIfMatch(TokenType.COMMA) || _lexer.PeekNextToken().Type != TokenType.R_PAREN)
            {
                if (_lexer.PeekNextToken().Type == TokenType.UNDERSCORE)
                {
                    _lexer.ConsumeToken();
                    args.Add(leftSidePipedValue);
                }
                else
                {
                    args.Add(ParsePipe());
                }
            }

            ConsumeAndExpect(TokenType.R_PAREN, "Expected closing ) for function call/pipe.");

            foreach (var arg in args)
            {
                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.PushParam, arg));
            }

            TempValue result = new TempValue(_currentParseState.NextTempNumber++);
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.CallFunction, result, targetFunction, new NumberValue(args.Count)));

            return result;
        }

        /// <summary>
        /// The main entry point for parsing any expression.
        /// It begins the chain of precedence by calling <see cref="ParseLogicalOr"/>.
        /// </summary>
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

        private Value ParseComparison()
        {
            TokenType type = _lexer.PeekNextToken().Type;

            if (type == TokenType.DOT_AND_CHECK || type == TokenType.DOT_OR_CHECK)
            {
                return ParseDotAndOrOperators();
            }

            Value left = ParseRange();

            // Potential collective comparison
            if (_lexer.PeekNextToken().Type == TokenType.COMMA)
            {
                // indeed so.
                if (IsCollectiveComparisonAhead())
                {

                    List<Value> args = new List<Value>() { left };
                    _lexer.ConsumeToken();

                    do
                    {
                        args.Add(ParseRange());
                    } while (ConsumeTokenIfMatch(TokenType.COMMA) && IsNotAStandardComparison(_lexer.PeekNextToken().Type));

                    return GenerateCollectiveComparisonByteCode(args, _lexer.ConsumeToken(), ParseRange());
                }
            }

            while (IsStandardComparisonOperator(_lexer.PeekNextToken().Type))
            {
                Token op = _lexer.ConsumeToken();
                Value right = ParseRange();

                Value temp = new TempValue(_currentParseState.NextTempNumber++);

                _currentParseState.AddCodeInstruction(new InstructionLine(GetInstructionCode(op.Type), temp, left, right, op));

                left = temp; // The result becomes the new left-hand side for the next loop.
            }

            return left;
        }

        private Value ParseDotAndOrOperators()
        {
            Token token = _lexer.ConsumeToken(); // Consume .and or .or

            ConsumeAndExpect(TokenType.L_PAREN, "Expecting opening ( after .and/.or");

            InstructionCode logicalOp = token.Type == TokenType.DOT_AND_CHECK ? InstructionCode.And : InstructionCode.Or;

            Value result = null;

            do
            {
                Value condition = ParseExpression();

                if (result == null)
                {
                    result = condition;
                }
                else
                {
                    TempValue temp = new TempValue(_currentParseState.NextTempNumber++);
                    _currentParseState.AddCodeInstruction(new InstructionLine(logicalOp, temp, condition, result));
                    result = temp;
                }
            }
            while (ConsumeTokenIfMatch(TokenType.COMMA));

            ConsumeAndExpect(TokenType.R_PAREN, "Expecting closing ) after .and/.or");

            return result ?? new BooleanValue(logicalOp == InstructionCode.And);
        }

        private Value GenerateCollectiveComparisonByteCode(List<Value> lhsExprs, Token op, Value rhs)
        {
            Value result = null;

            InstructionCode comparisonType = GetInstructionCodeForCollectiveOp(op.Type);
            InstructionCode logicalOp = IsOrCollectiveOperator(op.Type) ? InstructionCode.Or : InstructionCode.And;

            foreach (Value lhs in lhsExprs)
            {
                TempValue currentResult = new TempValue(_currentParseState.NextTempNumber++);
                _currentParseState.AddCodeInstruction(new InstructionLine(comparisonType, currentResult, lhs, rhs));

                if (result == null)
                {
                    result = currentResult;
                }
                else
                {
                    TempValue combinedResult = new TempValue(_currentParseState.NextTempNumber++);
                    _currentParseState.AddCodeInstruction(new InstructionLine(logicalOp, combinedResult, result, currentResult));
                    result = combinedResult;
                }
            }

            return result;
        }

        private Value ParseRange()
        {
            Value left = ParseAdditionSubtraction(); // Parse the start of the range

            if (_lexer.PeekNextToken().Type == TokenType.DOT_DOT)
            {
                _lexer.ConsumeToken(); // Consume '..'
                Value right = ParseAdditionSubtraction();
                TempValue resultList = new TempValue(_currentParseState.NextTempNumber++);

                // A user can do 10..0, the inverse of 0..10, should be accounted for in the Interpreter/VM.
                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.NewRangeList, resultList, left, right));

                return resultList;
            }

            return left; // Not a range, just a regular expression.
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

        //  MULTIPLICATION, DIVISION, MODULO (*, /, %)
        private Value ParseMulDivModulo()
        {
            Value left = ParseExponentation();

            while (IsMultiplicativeOperator(_lexer.PeekNextToken().Type))
            {
                Token op = _lexer.ConsumeToken();
                Value right = ParseExponentation();

                Value temp = new TempValue(_currentParseState.NextTempNumber++);

                _currentParseState.AddCodeInstruction(new InstructionLine(GetInstructionCode(op.Type), ResolveValue(temp), left, right, op));

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

                if (left is PropertyAccessValue propAccess)
                {
                    Value resolved = ResolveValue(propAccess);
                    _currentParseState.AddCodeInstruction(new InstructionLine(GetInstructionCode(op.Type), temp, resolved, right, op));
                }
                else
                {
                    _currentParseState.AddCodeInstruction(new InstructionLine(GetInstructionCode(op.Type), temp, left, right, op));
                }

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

        private Value ParsePostFix()
        {
            // ++ and --
            if (_lexer.PeekNextToken().Type == TokenType.DOT_DECREMENT || _lexer.PeekNextToken().Type == TokenType.DOT_INCREMENT)
            {
                ParseMultiIncrementDecrementOperators();
                return new StatementCompleteValue();
            }

            Value left = ParseAccess();
            bool operationPerformed = false;

            while (IsPostFixToken(_lexer.PeekNextToken().Type))
            {
                operationPerformed = true;
                Token op = _lexer.ConsumeToken();

                if (op.Type == TokenType.BOOLEAN_FLIP)
                {
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Negate, left));
                    continue;
                }

                Value one = new NumberValue(1, NumberValue.NumberType.Integer);
                Value temp = new TempValue(_currentParseState.NextTempNumber++);

                InstructionCode instrCode = (op.Type == TokenType.INCREMENT) ? InstructionCode.Add : InstructionCode.Subtract;

                _currentParseState.AddCodeInstruction(new InstructionLine(instrCode, temp, left, one, op));
                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, left, temp, null, op));

                left = temp;
            }

            return operationPerformed ? new StatementCompleteValue() : left;
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















































        /// <summary>
        /// Parses a multi-target increment or decrement operation.
        /// </summary>
        private void ParseMultiIncrementDecrementOperators()
        {
            Token opToken = _lexer.ConsumeToken(); // Consume .++ or .--

            ConsumeAndExpect(TokenType.L_PAREN, $"Expected an opening '(' after the '{opToken.ToDisplayString()}' operator.");

            InstructionCode operation = (opToken.Type == TokenType.DOT_DECREMENT)
                ? InstructionCode.Decrement
                : InstructionCode.Increment;

            do
            {
                // This gives us the original descriptor or variable.
                Value targetDescriptor = ParseExpression();

                // Resolve the *current value* of the target into a temporary variable.
                Value currentValue = ResolveValue(targetDescriptor);

                TempValue result = new TempValue(_currentParseState.NextTempNumber++);
                var one = new NumberValue(1);
                _currentParseState.AddCodeInstruction(new InstructionLine(operation, result, currentValue, one, opToken));

                GenerateWriteBackInstruction(targetDescriptor, result);
            } while (ConsumeTokenIfMatch(TokenType.COMMA));


            ConsumeAndExpect(TokenType.R_PAREN, $"a closing ')' after the '{opToken.ToDisplayString()}' operator's arguments.");
        }

        /// <summary>
        /// A helper method that generates the correct instruction (Assign, SetField, or SetElement)
        /// to write a value back to the location described by a descriptor.
        /// </summary>
        /// <param name="descriptor">The original Value, which may be a simple VariableValue or a complex descriptor like PropertyAccessValue.</param>
        /// <param name="valueToAssign">The Value (usually a TempValue) that holds the result to be written.</param>
        private void GenerateWriteBackInstruction(Value descriptor, Value valueToAssign)
        {
            switch (descriptor)
            {
                case VariableValue variable:
                    // Simple case: a = result
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.Assign, variable, valueToAssign));
                    break;
                case PropertyAccessValue propAccess:
                    // Complex case: a.b.c = result
                    // We must resolve the target object (a.b) before setting the field (c).
                    Value targetObject = ResolveValue(propAccess.Target);
                    _currentParseState.AddCodeInstruction(new InstructionLine(
                        InstructionCode.SetField,
                        targetObject,
                        new StringValue(propAccess.FieldName),
                        valueToAssign
                    ));
                    break;
                case ElementAccessValue elementAccess:
                    // Complex case: a[i][j] = result
                    // We must resolve the target collection (a[i]) and the index (j) before setting the element.
                    Value targetCollection = ResolveValue(elementAccess.Target);
                    Value index = ResolveValue(elementAccess.Index);
                    _currentParseState.AddCodeInstruction(new InstructionLine(
                        InstructionCode.SetElement,
                        targetCollection,
                        index,
                        valueToAssign
                    ));
                    break;
                default:
                    // This should not happen with valid syntax (e.g., trying to assign to a literal like `5++`).
                    // The parser would likely fail earlier, but this is a good safeguard.
                    ConstructAndThrowParserException("Invalid assignment target. Expected a variable, property, or list element.\"", _lexer.PeekAheadByN(1));
                    break;
            }
        }

        /// <summary>
        /// Ensures that a given Value is a simple, usable value
        /// rather than an abstract descriptor. If the input Value is a descriptor (like PropertyAccessValue
        /// or ElementAccessValue), this method generates the necessary GetField or GetElement bytecode
        /// to retrieve the actual value and returns the TempValue that will hold the result at runtime.
        /// </summary>
        /// <param name="val">The Value to resolve.</param>
        /// <returns>A simple Value that can be used as an operand in other instructions.</returns>
        private Value ResolveValue(Value val)
        {
            // Base case: The value is already a simple type, not a descriptor. Return it directly.
            if (val is not (PropertyAccessValue or ElementAccessValue))
            {
                return val;
            }

            if (val is PropertyAccessValue propAccess)
            {
                // First, we must recursively resolve the object being accessed.
                // This correctly handles chained accesses like `a.b.c`.
                Value resolvedTarget = ResolveValue(propAccess.Target);

                TempValue result = new TempValue(_currentParseState.NextTempNumber++);
                _currentParseState.AddCodeInstruction(new InstructionLine(
                    InstructionCode.GetField,
                    result,
                    resolvedTarget,
                    new StringValue(propAccess.FieldName)
                ));

                return result;
            }

            if (val is ElementAccessValue elementAccess)
            {
                // Recursively resolve the collection and the index.
                Value resolvedCollection = ResolveValue(elementAccess.Target);
                Value resolvedIndex = ResolveValue(elementAccess.Index);

                TempValue result = new TempValue(_currentParseState.NextTempNumber++);
                _currentParseState.AddCodeInstruction(new InstructionLine(
                    InstructionCode.GetElement,
                    result,
                    resolvedCollection,
                    resolvedIndex
                ));

                return result;
            }

            // This should be unreachable, but it satisfies the compiler.
            return val;
        }

        /// <summary>
        /// Parses a constructor call via parentheses, e.g., `MyStruct(arg1, arg2)`.
        /// </summary>
        /// <param name="structSymbol">The symbol for the struct being instantiated.</param>
        /// <returns>A TempValue that will hold the new struct instance at runtime.</returns>
        private TempValue ParseConstructorCall(StructSymbol structSymbol)
        {
            ConsumeAndExpect(TokenType.L_PAREN, $"Expected an opening '(' for the constructor call to '{structSymbol.Name}'.");

            TempValue instance = CreateNewInstance(structSymbol);

            List<Value> arguments = ParseArgumentList();

            ConsumeAndExpect(TokenType.R_PAREN, $"Expected closing ')' for the constructor call to '{structSymbol.Name}'.");

            // Check if an `init` method should be called.
            if (structSymbol.Constructor != null)
            {
                // A user-defined constructor exists. Check arity.
                if (arguments.Count != structSymbol.Constructor.Arity)
                {
                    Token errorToken = _lexer.PeekNextToken();
                    ConstructAndThrowParserException(
                        $"Mismatched arguments for constructor '{structSymbol.Name}'. Expected {structSymbol.Constructor.Arity} arguments, but got {arguments.Count}.",
                        errorToken
                    );
                }

                foreach (var arg in arguments)
                {
                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.PushParam, arg));
                }
                
                // The new instance is stored in 'instance'.
                TempValue ignoredResult = new TempValue(_currentParseState.NextTempNumber++);

                _currentParseState.AddCodeInstruction(new InstructionLine(
                    InstructionCode.CallMethod,
                    ignoredResult,
                    instance,
                    new StringValue("init")
                ));
            }
            else if (arguments.Count > 0)
            {
                // No user-defined constructor, but arguments were provided. This is an error.
                Token errorToken = _lexer.PeekAheadByN(1);
                ConstructAndThrowParserException(
                    $"Invalid constructor call for '{structSymbol.Name}'. Struct '{structSymbol.Name}' has no 'init' constructor and cannot be called with arguments.",
                    errorToken
                );
            }

            return instance;
        }

        /// <summary>
        /// Parses a direct struct initializer using brace syntax.
        /// </summary>
        /// <param name="structSymbol">The symbol for the struct being instantiated.</param>
        /// <returns>A TempValue that will hold the new struct instance at runtime.</returns>
        private TempValue ParseDirectInitializer(StructSymbol structSymbol)
        {
            ConsumeAndExpect(TokenType.L_BRACE, $"an opening '{{' for the direct initializer of '{structSymbol.Name}'.");

            // Create the new instance. Default field values are set here.
            TempValue instance = CreateNewInstance(structSymbol);
            var initializedFields = new HashSet<string>();

            if (_lexer.PeekNextToken().Type != TokenType.R_BRACE)
            {
                do
                {
                    Token fieldToken = ConsumeAndExpect(TokenType.IDENTIFIER, "Expected a field name in the struct initializer.");
                    string fieldName = fieldToken.Text;

                    if (!structSymbol.Fields.Contains(fieldName))
                    {
                        ConstructAndThrowParserException($"Invalid field '{fieldName}'. Struct '{structSymbol.Name}' does not have a field with this name.", fieldToken);
                    }
                    if (!initializedFields.Add(fieldName))
                    {
                        ConstructAndThrowParserException($"Duplicate field '{fieldName}'. Each field can only be initialized only once.", fieldToken);
                    }

                    ConsumeAndExpect(TokenType.COLON, $"Expected a ':' after the field name '{fieldName}'.");
                    Value fieldValue = ParseExpression();

                    _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.SetField, instance, new StringValue(fieldName), ResolveValue(fieldValue)));

                } while (ConsumeTokenIfMatch(TokenType.COMMA));
            }

            ConsumeAndExpect(TokenType.R_BRACE, "Expected a closing '}' to end the struct initializer.");

            return instance;
        }

        /// <summary>
        /// A helper method that generates the NewInstance bytecode instruction for a given struct.
        /// </summary>
        private TempValue CreateNewInstance(StructSymbol symbol)
        {
            TempValue instance = new TempValue(_currentParseState.NextTempNumber++);
            _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.NewInstance, instance, symbol));
            return instance;
        }

        /// <summary>
        /// Parses postfix expressions, which include function calls `()`, index access `[]`, and property access `.`.
        /// This method is called repeatedly in a loop to handle chained accesses like `my_obj.field[0]()`.
        /// </summary>
        /// <returns>A Value representing the result of the access chain.</returns>
        private Value ParseAccess()
        {
            // First, parse the primary expression that is being "accessed".
            Value left = ParsePrimary();

            // Then, loop to handle any number of chained postfix operators.
            while (true)
            {
                TokenType type = _lexer.PeekNextToken().Type;

                // Access get/set.
                if (type == TokenType.L_BRACKET)
                {
                    left = ParseIndexAccess(left);
                }
                // Property access.
                else if (type == TokenType.DOT)
                {
                    _lexer.ConsumeToken(); // Consume the dot.

                    Token memberToken = ConsumeAndExpect(TokenType.IDENTIFIER, "Expected a member name after '.' .");
                    string memberName = memberToken.Text;

                    if (left is VariableValue variable)
                    {
                        if (_currentParseState.CurrentScope.TryResolve(variable.IdentifierValue, out Symbol symbol) && symbol is EnumSymbol enumSymbol)
                        {
                            if (enumSymbol.Members.TryGetValue(memberName, out EnumValue enumValue))
                            {
                                left = enumValue;
                            }
                            else
                            {
                                ConstructAndThrowParserException($"Enum '{enumSymbol.Name}' does not have a member named '{memberToken.Text}'.", memberToken);
                            }
                        }
                        else
                        {
                            left = new PropertyAccessValue(left, memberName);
                        }
                    }
                }
                // Function call.
                else if (type == TokenType.L_PAREN)
                {
                    left = ParseFunctionCall(left);
                }
                else
                {
                    break;
                }
            }

            return left;
        }

        /// <summary>
        /// Parses a function or method call, assuming the callable expression (`left`) has already been parsed.
        /// </summary>
        private TempValue ParseFunctionCall(Value callable)
        {
            _lexer.ConsumeToken(); // Consume (.
            List<Value> arguments = ParseArgumentList();

            ConsumeAndExpect(TokenType.R_PAREN, "Expected a closing ')' for function call after function arguments.");

            foreach (Value arg in arguments)
            {
                _currentParseState.AddCodeInstruction(new InstructionLine(InstructionCode.PushParam, ResolveValue(arg)));
            }

            TempValue result = new TempValue(_currentParseState.NextTempNumber++);

            if (callable is PropertyAccessValue propAccess)
            {
                // This is a method call: instance.Method().
                _currentParseState.AddCodeInstruction(new InstructionLine(
                       InstructionCode.CallMethod,
                       result,
                       ResolveValue(propAccess.Target),
                       new StringValue(propAccess.FieldName)
                   ));
            }
            else
            {
                // This is a direct function call: func()
                _currentParseState.AddCodeInstruction(new InstructionLine(
                    InstructionCode.CallFunction,
                    result,
                    callable,
                    new NumberValue(arguments.Count)
                ));
            }

            return result;
        }

        /// <summary>
        /// Parses a comma-separated list of arguments until a closing parenthesis is encountered.
        /// </summary>
        /// <returns>A list of Values representing the parsed arguments.</returns>
        private List<Value> ParseArgumentList()
        {
            List<Value> arguments = new List<Value>();
            if (_lexer.PeekNextToken().Type != TokenType.R_PAREN)
            {
                do
                {
                    // Each argument is a full expression. We use the lowest precedence parser.
                    arguments.Add(ParseTernary());
                } while (ConsumeTokenIfMatch(TokenType.COMMA));
            }
            return arguments;
        }

        /// <summary>
        /// Parses an index access expression assuming the collection has been parsed.
        /// </summary>
        private ElementAccessValue ParseIndexAccess(Value left)
        {
            _lexer.ConsumeToken(); // Consume [.

            Value index = ParseExpression();

            ConsumeAndExpect(TokenType.R_BRACKET, "Expected a closing ']' for the index accessor.");

            // Create a descriptor for the access. This will be resolved into a GetElement
            // or SetElement instruction by a higher-level parsing method.
            return new ElementAccessValue(left, index, _currentParseState.NextTempNumber++, "Access");
        }

        /// <summary>
        /// Parses a primary expression, which is the highest level of precedence.
        /// This includes literals (numbers, strings, etc.), identifiers, grouping parentheses,
        /// and prefix unary operators.
        /// </summary>
        /// <returns>A Value representing the parsed primary expression.</returns>
        private Value ParsePrimary()
        {
            Token token = _lexer.ConsumeToken();

            if (token.Type == TokenType.NIL)
            {
                return new NilValue();
            }

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
                case TokenType.IDENTIFIER:
                    string name = token.Text;

                    // Check if it's a struct type, which could lead to a constructor call.
                    if (_currentParseState.CurrentScope.TryResolve(name, out Symbol symbol) && symbol is StructSymbol structSymbol)
                    {
                        // It's a struct, check if it's a constructor call Vec2(2,3).
                        // or a direct initializer Vec2{ x: 2, y: 3 }.
                        if (_lexer.PeekNextToken().Type == TokenType.L_PAREN)
                        {
                            return ParseConstructorCall(structSymbol);
                        }
                        else if (_lexer.PeekNextToken().Type == TokenType.L_BRACE)
                        {
                            return ParseDirectInitializer(structSymbol);
                        }
                    }
                    else
                    {
                        // Otherwise, it's just a regular variable.
                        return new VariableValue(name);
                    }
                    break;
                case TokenType.NUMBER: return NumberValue.FromToken(token);
                case TokenType.STRING: return new StringValue(token.Text);
                case TokenType.TRUE: return new BooleanValue(true);
                case TokenType.FALSE: return new BooleanValue(false);
                case TokenType.F_STRING: return ParseFString(token.Literal);
                case TokenType.CHARACTER: return new CharValue((char)token.Literal);
                case TokenType.L_BRACKET:
                    // We are in list, either initialization, or [i] access.
                    return ParseList();
                case TokenType.MATCH:
                    // This is when we call lhs = match x.
                    // Returning an actual value, an expression based match.
                    return ParseMatchStatement();
                case TokenType.SELF:
                    if (_currentParseState.CurrentStructContext == null)
                    {
                        ConstructAndThrowParserException("The 'self' keyword can only be used inside a struct method.", token);
                    }
                    // The 'self' keyword is just a special, pre-defined local variable.
                    // At runtime, the VM will ensure the instance is available.
                    return new VariableValue("self");
                case TokenType.L_PAREN:
                    // Go back to the lowest precedence to parse the inner expression.
                    Value expr = ParseTernary();
                    // Unclosed parentheses.
                    ConsumeAndExpect(TokenType.R_PAREN, "Expected: a closing ')' to match the opening parenthesis.");
                    return expr;
            }

            // If we've fallen through the entire switch, we have an invalid token.
            ConstructAndThrowParserException($"Unexpected token '{token.ToDisplayString()}' when expecting an expression. Expected a literal (e.g., number, string), variable, or '('.", token);
            return null;
        }

        /// <summary>
        /// Checks if the next token's type matches the expected type.
        /// If it matches, the token is consumed and the method returns true.
        /// If it does not match, the token is not consumed and the method returns false.
        /// </summary>
        private bool ConsumeTokenIfMatch(TokenType expectedType)
        {
            if (_lexer.PeekNextToken().Type == expectedType)
            {
                _lexer.ConsumeToken(); // It's a match, so we consume it.
                return true;
            }

            return false;
        }

        /// <summary>
        /// Consumes the next token from the lexer and throws a formatted parser exception if it does not match the expected type.
        /// This is the primary method for enforcing grammatical structure.
        /// </summary>
        /// <param name="expectedType">The TokenType that is grammatically required at this point in the stream.</param>
        /// <returns>The consumed token if it matches the expected type.</returns>
        /// <exception cref="FluenceParserException">Thrown if the consumed token's type does not match the expectedType.</exception>
        private Token ConsumeAndExpect(TokenType expectedType, string errorMessage)
        {
            Token token = _lexer.ConsumeToken();
            if (token.Type != expectedType)
            {
                ConstructAndThrowParserException(errorMessage, token);
            }
            return token;
        }

        private void ConstructAndThrowParserException(string errorMessage, Token token)
        {
            ParserExceptionContext context = new ParserExceptionContext()
            {
                Column = token.ColumnInSourceCode,
                FaultyLine = FluenceLexer.TruncateLine(FluenceLexer.GetCodeLineFromSource(_lexer.SourceCode, token.LineInSourceCode)),
                LineNum = token.LineInSourceCode,
                UnexpectedToken = token,
            };
            throw new FluenceParserException(errorMessage, context);
        }

        /// <summary>
        /// Checks if a token type is a multiplicative operator (*, /, %).
        /// </summary>
        private static bool IsMultiplicativeOperator(TokenType type) =>
            type is TokenType.STAR or TokenType.SLASH or TokenType.PERCENT;

        /// <summary>
        /// Checks if a token type is a simple assignment operator (=, +=, -=, etc.).
        /// </summary>
        private static bool IsSimpleAssignmentOperator(TokenType type) => type switch
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

        /// <summary>
        /// Checks if a token type is one of the chain-assignment operators (<|, <n|, <?|, etc.).
        /// </summary>
        private static bool IsChainAssignmentOperator(TokenType type) => type switch
        {
            TokenType.CHAIN_ASSIGN_N or
            TokenType.OPTIONAL_REST_ASSIGN or
            TokenType.OPTIONAL_ASSIGN_N or
            TokenType.SEQUENTIAL_REST_ASSIGN or
            TokenType.OPTIONAL_SEQUENTIAL_REST_ASSIGN or
            TokenType.REST_ASSIGN => true,
            _ => false
        };

        /// <summary>
        /// Checks if a token type is a multi-target compound assignment operator (.+=, .-=, etc.).
        /// </summary>
        private static bool IsMultiCompoundAssignmentOperator(TokenType type) => type switch
        {
            TokenType.DOT_PLUS_EQUAL or
            TokenType.DOT_MINUS_EQUAL or
            TokenType.DOT_STAR_EQUAL or
            TokenType.DOT_SLASH_EQUAL => true,
            _ => false,
        };

        /// <summary>
        /// Checks if a token type is a standard comparison operator (>, <, >=, <=).
        /// </summary>
        private static bool IsStandardComparisonOperator(TokenType type) =>
            type == TokenType.GREATER ||
            type == TokenType.LESS ||
            type == TokenType.GREATER_EQUAL ||
            type == TokenType.LESS_EQUAL;

        /// <summary>
        /// Checks if a token type is a collective comparison operator (<==|, <||==|, etc.).
        /// </summary>
        private static bool IsCollectiveOperator(TokenType type) =>
            type >= TokenType.COLLECTIVE_EQUAL && type <= TokenType.COLLECTIVE_OR_GREATER_EQUAL;

        /// <summary>
        /// Peeks ahead to determine if a match statement is using the switch-style syntax (`case:`)
        /// or the expression-style syntax (`case ->`).
        /// </summary>
        private bool IsSwitchStyleMatch()
        {
            int lookAhead = 1;
            while (true)
            {
                TokenType type = _lexer.PeekAheadByN(lookAhead).Type;
                if (type == TokenType.THIN_ARROW || type == TokenType.ARROW) return false;
                if (type == TokenType.COLON) return true;
                if (type == TokenType.R_BRACE) return false; // Empty match.
                lookAhead++;
            }
        }

        /// <summary>
        /// Peeks ahead to see if the upcoming tokens form a collective comparison expression.
        /// </summary>
        private bool IsCollectiveComparisonAhead()
        {
            int lookahead = 1;
            bool hasComma = false;

            while (true)
            {
                TokenType type = _lexer.PeekAheadByN(lookahead).Type;

                if (type == TokenType.COMMA) hasComma = true;

                if (IsCollectiveOperator(type) && hasComma)
                {
                    return true;
                }

                if (type == TokenType.L_BRACE || type == TokenType.THIN_ARROW || type == TokenType.EOF || type == TokenType.EOL)
                {
                    // Reached the end of the potential condition.
                    return false;
                }

                lookahead++;
            }
        }

        /// <summary>
        /// Peeks ahead in the token stream to determine if a broadcast pipe call (e.g., `func(_) <| ...`) is coming up.
        /// </summary>
        private bool IsBroadCastPipeFunctionCall()
        {
            // A broadcast call must start with `identifier (`
            if (_lexer.PeekNextToken().Type != TokenType.IDENTIFIER && _lexer.PeekAheadByN(2).Type != TokenType.L_PAREN)
            {
                return false;
            }

            int lookahead = 3;
            bool hasUnderscore = false;

            while (true)
            {
                TokenType type = _lexer.PeekAheadByN(lookahead).Type;

                if (type == TokenType.R_PAREN) break; // End of argument list
                if (type == TokenType.EOL)
                {
                    return false;
                }

                if (type == TokenType.EOF) return false; // End of file, not a valid call

                if (type == TokenType.UNDERSCORE) hasUnderscore = true;

                // Skip the argument and a potential comma
                lookahead++;
                if (_lexer.PeekAheadByN(lookahead).Type == TokenType.COMMA)
                {
                    lookahead++;
                }
            }

            // After the ')' at `lookahead`, the next token must be a chain operator.
            // The next token must be a chain assignment operator.
            return hasUnderscore && IsChainAssignmentOperator(_lexer.PeekAheadByN(lookahead + 1).Type);
        }

        /// <summary>
        /// Converts a multi-target compound assignment TokenType into its corresponding arithmetic InstructionCode.
        /// </summary>
        private static InstructionCode GetInstructionCodeForMultiCompoundAssignment(TokenType type) => type switch
        {
            TokenType.DOT_STAR_EQUAL => InstructionCode.Multiply,
            TokenType.DOT_SLASH_EQUAL => InstructionCode.Divide,
            TokenType.DOT_PLUS_EQUAL => InstructionCode.Add,
            TokenType.DOT_MINUS_EQUAL => InstructionCode.Subtract,
        };

        /// <summary>
        /// Checks if the token type is a unary operator: '++' or '--' or '-'.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static bool IsUnaryOperator(TokenType type) => type switch
        {
            TokenType.DECREMENT or
            TokenType.INCREMENT or
            TokenType.MINUS => true,
            _ => false,
        };

        /// <summary>
        /// Checks whether the operator is not a simple comparison operator, rather a complex one like collective comparison.
        /// </summary>
        /// <param name="type">The type of the token.</param>
        private static bool IsNotAStandardComparison(TokenType type)
        {
            return !IsStandardComparisonOperator(type) && type != TokenType.EQUAL_EQUAL && type != TokenType.BANG_EQUAL;
        }

        /// <summary>
        /// Checks if the operator is a collective OR operator.
        /// </summary>
        /// <param name="type">The type of the token.</param>
        /// <returns>True if it is.</returns>
        private static bool IsOrCollectiveOperator(TokenType type) => type switch
        {
            TokenType.COLLECTIVE_OR_EQUAL => true,
            TokenType.COLLECTIVE_OR_NOT_EQUAL => true,
            TokenType.COLLECTIVE_OR_LESS => true,
            TokenType.COLLECTIVE_OR_LESS_EQUAL => true,
            TokenType.COLLECTIVE_OR_GREATER => true,
            TokenType.COLLECTIVE_OR_GREATER_EQUAL => true,
            _ => false
        };

        /// <summary>
        /// Checks if the token is a postfix operator, such as '!!', '++', '--'.
        /// </summary>
        /// <param name="type">The type of the token.</param>
        private static bool IsPostFixToken(TokenType type) =>
            type == TokenType.INCREMENT ||
            type == TokenType.DECREMENT ||
            type == TokenType.BOOLEAN_FLIP;

        /// <summary>
        /// Converts a collective comparison TokenType into its corresponding base comparison InstructionCode.
        /// </summary>
        /// <param name="type">The TokenType of the collective comparison operator.</param>
        /// <returns>The corresponding base InstructionCode.</returns>
        private static InstructionCode GetInstructionCodeForCollectiveOp(TokenType type) => type switch
        {
            TokenType.COLLECTIVE_EQUAL => InstructionCode.Equal,
            TokenType.COLLECTIVE_NOT_EQUAL => InstructionCode.NotEqual,
            TokenType.COLLECTIVE_GREATER => InstructionCode.GreaterThan,
            TokenType.COLLECTIVE_GREATER_EQUAL => InstructionCode.GreaterEqual,
            TokenType.COLLECTIVE_LESS => InstructionCode.LessThan,
            TokenType.COLLECTIVE_LESS_EQUAL => InstructionCode.LessEqual,
            TokenType.COLLECTIVE_OR_EQUAL => InstructionCode.Equal,
            TokenType.COLLECTIVE_OR_NOT_EQUAL => InstructionCode.NotEqual,
            TokenType.COLLECTIVE_OR_LESS => InstructionCode.LessEqual,
            TokenType.COLLECTIVE_OR_LESS_EQUAL => InstructionCode.LessEqual,
            TokenType.COLLECTIVE_OR_GREATER => InstructionCode.GreaterThan,
            TokenType.COLLECTIVE_OR_GREATER_EQUAL => InstructionCode.GreaterEqual,
        };
    }
}