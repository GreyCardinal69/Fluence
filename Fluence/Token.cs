namespace Fluence
{
    /// <summary>
    /// Represents a single lexical token from a Fluence script.
    /// This is an immutable struct, containing the token's type, its original text, and any literal value it represents.
    /// </summary>
    internal readonly record struct Token
    {
        /// <summary>
        /// Defines all possible types of tokens in the Fluence language.
        /// </summary>
        internal enum TokenType
        {
            UNKNOWN = 0,

            // == Single-Character Tokens ==
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
            AMPERSAND,    // & (Bitwise AND)
            PIPE_CHAR,    // | (Bitwise OR)
            CARET,        // ^ (Bitwise XOR)
            TILDE,        // ~ (Bitwise NOT)
            QUESTION,     // ?

            // == Multi-Character Operators ==
            BANG, BANG_EQUAL,       // !, !=
            EQUAL, EQUAL_EQUAL,     // =, ==
            GREATER, GREATER_EQUAL, // >, >=
            LESS, LESS_EQUAL,       // <, <=
            DOT_DOT,                // For Ranges (a..b).
            BITWISE_LEFT_SHIFT,     // <<
            BITWISE_RIGHT_SHIFT,    // >>
            AND,                    // &&
            OR,                     // ||
            INCREMENT,              // ++
            DECREMENT,              // --
            EXPONENT,               // **

            // == Compound Assignment Operators ==
            EQUAL_PLUS,             // +=
            EQUAL_MINUS,            // -=
            EQUAL_MUL,              // *=
            EQUAL_DIV,              // /=
            EQUAL_PERCENT,          // %=
            EQUAL_AMPERSAND,        // &=

            // == Function and Block Arrows ==
            ARROW,      // =>
            THIN_ARROW, // ->

            // == Literals & Identifiers ==
            IDENTIFIER,
            STRING,
            F_STRING,       // Formatted String
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
            IS,         // `is` is an alias for `==`.
            NOT,        // `not` is an alias for `!=`.
            SPACE,
            USE,
            TYPE,
            STRUCT,
            ENUM,
            MATCH,
            SELF,
            REST,   // `rest` keyword in match statements.

            // == Pipe Family Operators ==
            PIPE,               // |>
            OPTIONAL_PIPE,      // |?
            GUARD_PIPE,         // |??
            MAP_PIPE,           // |>>
            REDUCER_PIPE,       // |>>=
            SCAN_PIPE,          // |~>
            COMPOSITION_PIPE,   // ~>

            // == Chain Assignment & Broadcast Family Operators ==
            CHAIN_ASSIGN_N,                      // <n|
            REST_ASSIGN,                         // <|
            OPTIONAL_ASSIGN_N,                   // <n?|
            OPTIONAL_REST_ASSIGN,                // <?|
            SEQUENTIAL_REST_ASSIGN,              // <~|
            OPTIONAL_SEQUENTIAL_REST_ASSIGN,     // <~?|
            GUARD_CHAIN,                         // <??|
            OR_GUARD_CHAIN,                      // <||??|

            // == Dot-Prefixed Operators ==
            DOT_AND_CHECK,      // .or(...)
            DOT_OR_CHECK,       // .and(....)
            DOT_INCREMENT,      // .++(...)
            DOT_DECREMENT,      // .--(...)
            DOT_PLUS_EQUAL,     // .+=
            DOT_MINUS_EQUAL,    // .-=
            DOT_STAR_EQUAL,     // .*=
            DOT_SLASH_EQUAL,    // ./=

            SWAP,               // ><, swaps values of two variables.
            TERNARY_JOINT,      // ?: same as ? ... : ... but instead ?: ... , ...
            BOOLEAN_FLIP,       // bool!!, x = !x.

            // == Collective Comparison (AND variants) ==
            COLLECTIVE_EQUAL,           // <==|
            COLLECTIVE_NOT_EQUAL,       // <!=|
            COLLECTIVE_LESS,            // <<|
            COLLECTIVE_LESS_EQUAL,      // <<=|
            COLLECTIVE_GREATER,         // <>|
            COLLECTIVE_GREATER_EQUAL,   // <>=|

            // == Collective Comparison (OR variants) ==
            COLLECTIVE_OR_EQUAL,            // <||==|
            COLLECTIVE_OR_NOT_EQUAL,        // <||!=|
            COLLECTIVE_OR_LESS,             // <||<|
            COLLECTIVE_OR_LESS_EQUAL,       // <||<=|
            COLLECTIVE_OR_GREATER,          // <||>|
            COLLECTIVE_OR_GREATER_EQUAL,    // <||>=|

            // == Special & Control Tokens ==
            UNDERSCORE,     // _
            EOL,            // End Of Line (statement terminator, from ';')

            /// <summary>
            /// An internal token representing a physical newline. Used by the lexer for accurate
            /// line counting but removed before the parsing phase.
            /// </summary>
            EOL_LEXER,

            /// <summary>
            /// Represents the end of the input file.
            /// </summary>
            EOF
        }

        /// <summary>
        /// The grammatical type of the token.
        /// </summary>
        internal readonly TokenType Type;

        /// <summary>
        /// The raw string of characters from the source code that this token represent.
        /// </summary>
        internal readonly string Text;

        /// <summary>
        /// For literal tokens, this holds the actual value (e.g., the number 123, the string "hello").
        /// For other tokens, this is typically null.
        /// </summary>
        internal readonly object Literal;

        /// <summary>A shared, single instance of the End-Of-Line-Lexer token.</summary>
        internal static Token EOL_LEXER => new Token(TokenType.EOL_LEXER);

        /// <summary>A shared, single instance of the End-of-File token.</summary>
        internal static readonly Token EOF = new Token(TokenType.EOF);

        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> struct.
        /// </summary>
        /// <param name="type">The type of the token.</param>
        /// <param name="text">The raw text of the token.</param>
        /// <param name="literal">The literal value, if any.</param>
        internal Token(TokenType type = TokenType.UNKNOWN, string text = "", object literal = null)
        {
            Type = type;
            Text = text;
            Literal = literal;
        }

        public override string ToString()
        {
            if (Literal != null) return $"{Type}: {Text} [{Literal}]";
            return string.IsNullOrEmpty(Text) ? Type.ToString() : $"{Type}: {Text}";
        }
    }
}