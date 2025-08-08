using Xunit.Abstractions;
using static Fluence.Token.TokenType;

namespace Fluence.Tests
{
    public class LexerFullScriptTests(ITestOutputHelper output)
    {
        private readonly ITestOutputHelper _output = output;

        private static List<Token> LexAllTokens(string source)
        {
            var lexer = new FluenceLexer(source);
            var tokens = new List<Token>();
            Token token;
            do
            {
                token = lexer.ConsumeToken();
                tokens.Add(token);
            } while (!lexer.HasReachedEnd);
            return tokens;
        }

        [Fact]
        public void CorrectlyTokenizesFullCalculatorScript()
        {
            string source = @"
            func input_int() => to_int(input());

            func Main() => {
                num1, num2, op <2| input_int() <1| input();

                if num1, num2, op <!=| nil {
                    result = match op {
                        ""+"" -> num1 + num2,
                        ""-"" -> num1 - num2,
                        ""*"" -> num1 * num2,
                        ""/"" -> num2 == 0 ? nil : num1 / num2,
                        rest   -> nil
                    };

                    if result is nil -> print(""Error..."") |> exit(_);
                    else -> print(f""Result: {result}"");
                } else -> print(""Error..."") |> exit(1);
            }";

            var expectedTypes = new List<Token.TokenType> {
                EOL, FUNC, IDENTIFIER, L_PAREN, R_PAREN, ARROW, IDENTIFIER, L_PAREN, IDENTIFIER, L_PAREN, R_PAREN, R_PAREN,
                EOL, FUNC, IDENTIFIER, L_PAREN, R_PAREN, ARROW, L_BRACE, EOL,
                IDENTIFIER, COMMA, IDENTIFIER, COMMA, IDENTIFIER, CHAIN_ASSIGN_N, IDENTIFIER, L_PAREN, R_PAREN, CHAIN_ASSIGN_N, IDENTIFIER, L_PAREN, R_PAREN, EOL,
                IF, IDENTIFIER, COMMA, IDENTIFIER, COMMA, IDENTIFIER, COLLECTIVE_NOT_EQUAL, NIL, L_BRACE, EOL,
                IDENTIFIER, EQUAL, MATCH, IDENTIFIER, L_BRACE, EOL,
                STRING, THIN_ARROW, IDENTIFIER, PLUS, IDENTIFIER, COMMA, EOL,
                STRING, THIN_ARROW, IDENTIFIER, MINUS, IDENTIFIER, COMMA,EOL,
                STRING, THIN_ARROW, IDENTIFIER, STAR, IDENTIFIER, COMMA,EOL,
                STRING, THIN_ARROW, IDENTIFIER, EQUAL_EQUAL, NUMBER, QUESTION, NIL, COLON, IDENTIFIER, SLASH, IDENTIFIER, COMMA,EOL,
                REST, THIN_ARROW, NIL,EOL,
                R_BRACE, EOL,
                IF, IDENTIFIER, IS, NIL, THIN_ARROW, IDENTIFIER, L_PAREN, STRING, R_PAREN, PIPE, IDENTIFIER, L_PAREN, UNDERSCORE, R_PAREN,EOL,
                ELSE, THIN_ARROW, IDENTIFIER, L_PAREN, F_STRING, R_PAREN, EOL,
                R_BRACE,
                ELSE, THIN_ARROW, IDENTIFIER, L_PAREN, STRING, R_PAREN, PIPE, IDENTIFIER, L_PAREN, NUMBER, R_PAREN,EOL,
                R_BRACE
            };

            var actualTokens = LexAllTokens(source);

            var actualTypes = actualTokens.Select(t => t.Type).ToList();

            if (!expectedTypes.SequenceEqual(actualTypes))
            {
                _output.WriteLine("--- ACTUAL TOKEN STREAM ---");
                foreach (var token in actualTokens)
                {
                    _output.WriteLine($"{token.Type,-25} {token.Text ?? "",-30} {token.Literal ?? ""}");
                }
            }

            Assert.Equal(expectedTypes, actualTypes);
        }
    }
}