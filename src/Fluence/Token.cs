namespace Fluence
{
    internal record Token
    {
        internal enum TokenType
        {
            // Single characters.
            L_PAREN,      // (
            R_PAREN,      // )
            L_BRACE,      // {
            R_BRACE,      // }
            L_BRACKET,    // [
            R_BRACKET,    // ]
            COMMA,        // ,
            DOT,          // .
            SEMICOLON,    // ;
            COLON,        // :
            PLUS,         // +
            MINUS,        // -
            STAR,         // *
            SLASH,        // /
            PERCENT,      // %
            AMPERSAND,    // &
            PIPE_CHAR,    // |  (Note: separate from the full |> operator)
            CARET,        // ^
            TILDE,        // ~
            QUESTION,     // ?

            // One or two characters.
            BANG, BANG_EQUAL,       // !, !=
            EQUAL, EQUAL_EQUAL,     // =, ==
            GREATER, GREATER_EQUAL, // >, >=
            LESS, LESS_EQUAL,       // <, <=
            STAR_STAR,              // ** (Power)
            DOT_DOT,                // For Ranges

            // Function and labdas.
            ARROW,      // =>
            THIN_ARROW, // ->

            // Literals.
            IDENTIFIER,
            STRING,
            F_STRING,
            CHARACTER,
            NUMBER,

            // Keywords.
            BREAK,
            CONTINUE,
            IF,
            ELSE,
            WHILE,
            LOOP,
            FOR,
            IN,
            FUNC,
            NIL,
            RETURN,
            TRUE,
            FALSE,
            IS,
            NOT,
            SPACE,
            USE,
            TYPE,
            STRUCT,
            ENUM,
            MATCH,
            SELF,

            // Pipe operators.
            PIPE,               // |>
            OPTIONAL_PIPE,      // |?
            GUARD_PIPE,         // |??
            MAP_PIPE,           // |>>
            REDUCER_PIPE,       // |>>=
            SCAN_PIPE,          // |~>
            COMPOSITION_PIPE,   // ~>

            // Distributive Family Pipe operators.
            CHAIN_ASSIGN_N,     // <n|
            REST_ASSIGN,        // <|
            OPTIONAL_ASSIGN_N,  // <n?|
            GUARD_CHAIN,        // <??|
            OR_GUARD_CHAIN,     // <||??|

            COLLECTIVE_EQUAL,           // <==|
            COLLECTIVE_NOT_EQUAL,       // <!=|
            COLLECTIVE_LESS,            // <<|
            COLLECTIVE_LESS_EQUAL,      // <<=|
            COLLECTIVE_GREATER,         // <>|
            COLLECTIVE_GREATER_EQUAL,   // <>=|

            // The OR variants.
            COLLECTIVE_OR_EQUAL,        // <||==|
            COLLECTIVE_OR_NOT_EQUAL,    // <||!=|
            COLLECTIVE_OR_LESS,         // <||<|
            COLLECTIVE_OR_LESS_EQUAL,   // <||<=|
            COLLECTIVE_OR_GREATER,      // <||>|
            COLLECTIVE_OR_GREATER_EQUAL,// <||>=|

            UNDERSCORE,

            EOF
        }

        internal readonly TokenType Type;
        internal readonly string Text;
        internal readonly object Literal;

        internal Token(TokenType type, string text, object literal = null )
        {
            Type = type;
            Text = text;
            Literal = literal;
        }

        public override string ToString() =>
            Text == null ? Type.ToString() : $"{Type}: {Text}";
    }
}