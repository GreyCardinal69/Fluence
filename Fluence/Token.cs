namespace Fluence
{
    internal readonly record struct Token
    {
        internal enum TokenType
        {
            UNKNOWN = 0,

            // Single characters.
            L_PAREN,      // (
            R_PAREN,      // )
            L_BRACE,      // {
            R_BRACE,      // }
            L_BRACKET,    // [
            R_BRACKET,    // ]
            COMMA,        // ,
            DOT,          // .
            COLON,        // :
            PLUS,         // +
            MINUS,        // -
            STAR,         // *
            SLASH,        // /
            PERCENT,      // %
            AMPERSAND,    // &
            PIPE_CHAR,    // |  (Note: separate from the full |> operator), bitwise or.
            CARET,        // ^
            TILDE,        // ~
            QUESTION,     // ?

            // One or two characters.
            BANG, BANG_EQUAL,       // !, !=
            EQUAL, EQUAL_EQUAL,     // =, ==
            GREATER, GREATER_EQUAL, // >, >=
            LESS, LESS_EQUAL,       // <, <=
            DOT_DOT,                // For Ranges
            BITWISE_LEFT_SHIFT,     // <<
            BITWISE_RIGHT_SHIFT,    // >>
            AND,                    // &&
            OR,                     // ||
            INCREMENT,              // ++
            DECREMENT,              // --
            EXPONENT,               // **

            EQUAL_PLUS,             // +=
            EQUAL_MINUS,            // -=
            EQUAL_MUL,              // *=
            EQUAL_DIV,              // /=
            EQUAL_PERCENT,          // %=
            EQUAL_AMPERSAND,        // &=

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
            REST,

            // Pipe operators.
            PIPE,               // |>
            OPTIONAL_PIPE,      // |?
            GUARD_PIPE,         // |??
            MAP_PIPE,           // |>>
            REDUCER_PIPE,       // |>>=
            SCAN_PIPE,          // |~>
            COMPOSITION_PIPE,   // ~>

            // Distributive Family Pipe operators.
            CHAIN_ASSIGN_N,                      // <n|
            REST_ASSIGN,                         // <|
            OPTIONAL_ASSIGN_N,                   // <n?|
            OPTIONAL_REST_ASSIGN,                // <?|
            SEQUENTIAL_REST_ASSIGN,              // <~|
            OPTIONAL_SEQUENTIAL_REST_ASSIGN,     // <~?|
            
            GUARD_CHAIN,        // <??|
            OR_GUARD_CHAIN,     // <||??|

            // Dot family
            DOT_AND_CHECK,      // .or(...)
            DOT_OR_CHECK,       // .and(....)
            DOT_INCREMENT,      // .++(...)
            DOT_DECREMENT,      // .--(...)

            SWAP,               // a >< b

            TERNARY_JOINT,      // ?: same as ? ... : ... but instead ?: ... , ...

            BOOLEAN_FLIP,       // bool!!, x = !x.

            COLLECTIVE_EQUAL,           // <==|
            COLLECTIVE_NOT_EQUAL,       // <!=|
            COLLECTIVE_LESS,            // <<|
            COLLECTIVE_LESS_EQUAL,      // <<=|
            COLLECTIVE_GREATER,         // <>|
            COLLECTIVE_GREATER_EQUAL,   // <>=|

            // The OR variants.
            COLLECTIVE_OR_EQUAL,            // <||==|
            COLLECTIVE_OR_NOT_EQUAL,        // <||!=|
            COLLECTIVE_OR_LESS,             // <||<|
            COLLECTIVE_OR_LESS_EQUAL,       // <||<=|
            COLLECTIVE_OR_GREATER,          // <||>|
            COLLECTIVE_OR_GREATER_EQUAL,    // <||>=|

            UNDERSCORE,
            EOL,
            EOF
        }

        internal readonly TokenType Type;
        internal readonly string Text;
        internal readonly object Literal;

        internal static Token EOL => new Token(TokenType.EOL, "\n");
        internal static Token EOF = new Token(TokenType.EOF);

        internal Token(TokenType type = TokenType.UNKNOWN, string text = "", object literal = null)
        {
            Type = type;
            Text = text;
            Literal = literal;
        }

        public override string ToString() =>
            Text == null ? Type.ToString() : $"{Type}: {Text}";
    }
}