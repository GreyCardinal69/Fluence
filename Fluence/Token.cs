﻿namespace Fluence
{
    /// <summary>
    /// Represents a single lexical token from a Fluence script.
    /// This is an immutable struct, containing the token's type, its original text, and any literal value it represents.
    /// </summary>
    internal readonly record struct Token
    {
        /// <summary>
        /// Defines all possible types of tokens in Fluence.
        /// Many of the members are ordered sequentially to be able to use >= && <= range checks.
        /// Which are considerably faster to do.
        /// </summary>
        internal enum TokenType
        {
            UNKNOWN = 0,
            NO_USE,       // A sort of a "discard" token type.

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

            BANG, BANG_EQUAL,       // !, !=
            EQUAL_EQUAL,            // ==
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

            // Compound Assignment Operators.
            EQUAL,                  // =
            EQUAL_PLUS,             // +=
            EQUAL_MINUS,            // -=
            EQUAL_MUL,              // *=
            EQUAL_DIV,              // /=
            EQUAL_PERCENT,          // %=
            EQUAL_AMPERSAND,        // &=

            // Function and Block Arrows.
            ARROW,      // =>
            THIN_ARROW, // ->

            // Literals & Identifiers.
            IDENTIFIER,
            STRING,
            F_STRING,       // Formatted String.
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
            UNTIL,
            REST,   // `rest` keyword in match statements.
            SOLID,  // immutable.
            TIMES,  // N times do x.
            UNLESS, // Reverse of if, unless ( x ) do something.
            AS,     // Used with 'times', and maybe others in the future.
            REF,    // Passed by reference.
            TRY,
            CATCH,
            IMPL,   // Inheritance.
            TRAIT,  // Inheritable.

            // Pipe Family Operators.
            PIPE,               // |>
            OPTIONAL_PIPE,      // |?
            GUARD_PIPE,         // |??
            MAP_PIPE,           // |>>
            REDUCER_PIPE,       // |>>=
            SCAN_PIPE,          // |~>
            COMPOSITION_PIPE,   // ~>

            // Chain Assignment & Broadcast Family Operators.
            CHAIN_ASSIGN_N,                      // <n|
            REST_ASSIGN,                         // <|
            OPTIONAL_ASSIGN_N,                   // <n?|
            OPTIONAL_REST_ASSIGN,                // <?|
            SEQUENTIAL_REST_ASSIGN,              // <~|
            CHAIN_N_UNIQUE_ASSIGN,               // <!|
            OPTIONAL_CHAIN_N_UNIQUE_ASSIGN,      // <!?|
            OPTIONAL_SEQUENTIAL_REST_ASSIGN,     // <~?|

            GUARD_CHAIN,                         // <??|
            OR_GUARD_CHAIN,                      // <||??|

            // Dot-Prefixed Operators.
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

            // Collective Comparison (AND variants).
            COLLECTIVE_EQUAL,           // <==|
            COLLECTIVE_NOT_EQUAL,       // <!=|
            COLLECTIVE_LESS,            // <<|
            COLLECTIVE_LESS_EQUAL,      // <<=|
            COLLECTIVE_GREATER,         // <>|
            COLLECTIVE_GREATER_EQUAL,   // <>=|

            // Collective Comparison (OR variants).
            COLLECTIVE_OR_EQUAL,            // <||==|
            COLLECTIVE_OR_NOT_EQUAL,        // <||!=|
            COLLECTIVE_OR_LESS,             // <||<|
            COLLECTIVE_OR_LESS_EQUAL,       // <||<=|
            COLLECTIVE_OR_GREATER,          // <||>|
            COLLECTIVE_OR_GREATER_EQUAL,    // <||>=|

            TRAIN_PIPE,         // ->>
            TRAIN_PIPE_END,     // <<-

            TYPE_OF,

            UNDERSCORE,     // _
            EOL,            // End Of Line (statement terminator, from ';').

            /// <summary>
            /// An internal token representing a physical newline. Used by the lexer for accurate
            /// line counting but removed before the parsing phase.
            /// </summary>
            NEW_LINE,

            /// <summary>Represents the end of the input file.</summary>
            EOF
        }

        /// <summary>Provides a user-friendly string representation of a token.</summary>
        /// <returns>A string representing the token.</returns>
        internal string ToDisplayString()
        {
            // If the token has explicit text return that.
            if (!string.IsNullOrEmpty(Text))
            {
                return Text;
            }

            return Type switch
            {
                TokenType.L_PAREN => "(",
                TokenType.R_PAREN => ")",
                TokenType.L_BRACE => "{",
                TokenType.R_BRACE => "}",
                TokenType.L_BRACKET => "[",
                TokenType.R_BRACKET => "]",
                TokenType.COMMA => ",",
                TokenType.DOT => ".",
                TokenType.COLON => ":",
                TokenType.PLUS => "+",
                TokenType.MINUS => "-",
                TokenType.STAR => "*",
                TokenType.SLASH => "/",
                TokenType.PERCENT => "%",
                TokenType.AMPERSAND => "&",
                TokenType.PIPE_CHAR => "|",
                TokenType.CARET => "^",
                TokenType.TILDE => "~",
                TokenType.QUESTION => "?",

                TokenType.BANG => "!",
                TokenType.BANG_EQUAL => "!=",
                TokenType.EQUAL_EQUAL => "==",
                TokenType.GREATER => ">",
                TokenType.GREATER_EQUAL => ">=",
                TokenType.LESS => "<",
                TokenType.LESS_EQUAL => "<=",
                TokenType.DOT_DOT => "..",
                TokenType.BITWISE_LEFT_SHIFT => "<<",
                TokenType.BITWISE_RIGHT_SHIFT => ">>",
                TokenType.AND => "&&",
                TokenType.OR => "||",
                TokenType.INCREMENT => "++",
                TokenType.DECREMENT => "--",
                TokenType.EXPONENT => "**",
                TokenType.EQUAL => "=",
                TokenType.EQUAL_PLUS => "+=",
                TokenType.EQUAL_MINUS => "-=",
                TokenType.EQUAL_MUL => "*=",
                TokenType.EQUAL_DIV => "/=",
                TokenType.EQUAL_PERCENT => "%=",
                TokenType.EQUAL_AMPERSAND => "&=",
                TokenType.ARROW => "=>",
                TokenType.THIN_ARROW => "->",

                TokenType.BREAK => "break",
                TokenType.CONTINUE => "continue",
                TokenType.IF => "if",
                TokenType.ELSE => "else",
                TokenType.WHILE => "while",
                TokenType.LOOP => "loop",
                TokenType.FOR => "for",
                TokenType.IN => "in",
                TokenType.FUNC => "func",
                TokenType.NIL => "nil",
                TokenType.RETURN => "return",
                TokenType.TRUE => "true",
                TokenType.FALSE => "false",
                TokenType.IS => "is",
                TokenType.NOT => "not",
                TokenType.SPACE => "space",
                TokenType.USE => "use",
                TokenType.TYPE => "type",
                TokenType.STRUCT => "struct",
                TokenType.ENUM => "enum",
                TokenType.MATCH => "match",
                TokenType.SELF => "self",
                TokenType.UNTIL => "until",
                TokenType.REST => "rest",
                TokenType.SOLID => "solid",
                TokenType.TIMES => "times",
                TokenType.UNLESS => "unless",
                TokenType.AS => "as",
                TokenType.REF => "ref",
                TokenType.TRY => "try",
                TokenType.CATCH => "catch",
                TokenType.IMPL => "impl",
                TokenType.TRAIT => "trait",

                TokenType.PIPE => "|>",
                TokenType.OPTIONAL_PIPE => "|?",
                TokenType.GUARD_PIPE => "|??",
                TokenType.MAP_PIPE => "|>>",
                TokenType.REDUCER_PIPE => "|>>=",
                TokenType.SCAN_PIPE => "|~>",
                TokenType.COMPOSITION_PIPE => "~>",

                TokenType.CHAIN_ASSIGN_N => "<n|",
                TokenType.REST_ASSIGN => "<|",
                TokenType.OPTIONAL_ASSIGN_N => "<n?|",
                TokenType.OPTIONAL_REST_ASSIGN => "<?|",
                TokenType.SEQUENTIAL_REST_ASSIGN => "<~|",
                TokenType.CHAIN_N_UNIQUE_ASSIGN => "<!|",
                TokenType.OPTIONAL_CHAIN_N_UNIQUE_ASSIGN => "<!?|",
                TokenType.OPTIONAL_SEQUENTIAL_REST_ASSIGN => "<~?|",

                TokenType.GUARD_CHAIN => "<??|",
                TokenType.OR_GUARD_CHAIN => "<||??|",

                TokenType.DOT_AND_CHECK => ".or",
                TokenType.DOT_OR_CHECK => ".and",
                TokenType.DOT_INCREMENT => ".++",
                TokenType.DOT_DECREMENT => ".--",
                TokenType.DOT_PLUS_EQUAL => ".+=",
                TokenType.DOT_MINUS_EQUAL => ".-=",
                TokenType.DOT_STAR_EQUAL => ".*=",
                TokenType.DOT_SLASH_EQUAL => "./=",

                TokenType.SWAP => "><",
                TokenType.TERNARY_JOINT => "?:",
                TokenType.BOOLEAN_FLIP => "!!",

                TokenType.COLLECTIVE_EQUAL => "<==|",
                TokenType.COLLECTIVE_NOT_EQUAL => "<!=|",
                TokenType.COLLECTIVE_LESS => "<<|",
                TokenType.COLLECTIVE_LESS_EQUAL => "<<=|",
                TokenType.COLLECTIVE_GREATER => "<>|",
                TokenType.COLLECTIVE_GREATER_EQUAL => "<>=|",

                TokenType.COLLECTIVE_OR_EQUAL => "<||==|",
                TokenType.COLLECTIVE_OR_NOT_EQUAL => "<||!=|",
                TokenType.COLLECTIVE_OR_LESS => "<||<|",
                TokenType.COLLECTIVE_OR_LESS_EQUAL => "<||<=|",
                TokenType.COLLECTIVE_OR_GREATER => "<||>|",
                TokenType.COLLECTIVE_OR_GREATER_EQUAL => "<||>=|",

                TokenType.TRAIN_PIPE => "->>",
                TokenType.TRAIN_PIPE_END => "<<-",

                TokenType.TYPE_OF => "typeof",

                TokenType.UNDERSCORE => "_",

                // This method is called in the FluenceParser only, in the parser
                // EOL means ';' semicolon.
                TokenType.EOL => ";",

                TokenType.NEW_LINE => "\n",
                TokenType.EOF => "<EOF>",

                // For other token types without text, the type name is the best we can do.
                _ => Type.ToString()
            };
        }

        /// <summary>
        /// The grammatical type of the token.
        /// </summary>
        internal readonly TokenType Type;

        /// <summary>
        /// The raw string of characters from the source code that this token represents.
        /// </summary>
        internal readonly string Text;

        /// <summary>
        /// For literal tokens, this holds the actual value.
        /// For other tokens, this is typically null.
        /// </summary>
        internal readonly object Literal;

        // Line and Column indexes are stored as a short, this means that code length of a file
        // Is limited to 65,535 lines, which is more than a reasonable amount.

        /// <summary>
        /// Stores the line location of the token in the original lexer stream, for debugging and better exception context.
        /// </summary>
        internal readonly ushort LineInSourceCode;

        /// <summary>
        /// Stores the column location of the token in the original lexer stream, for debugging and better exception context.
        /// </summary>
        internal readonly ushort ColumnInSourceCode;

        /// <summary>A shared, single instance of the End-Of-Line-Lexer token.</summary>
        internal static readonly Token NEW_LINE = new Token(TokenType.NEW_LINE);

        /// <summary>A shared, single instance of the End-of-File token.</summary>
        internal static readonly Token EOF = new Token(TokenType.EOF);

        /// <summary>A shared, single instance of the No Use token.</summary>
        internal static readonly Token NoUse = new Token(TokenType.NO_USE);

        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> struct.
        /// </summary>
        /// <param name="type">The type of the token.</param>
        /// <param name="text">The raw text of the token.</param>
        /// <param name="literal">The literal value, if any.</param>
        internal Token(TokenType type = TokenType.UNKNOWN, string text = "", object literal = null!, ushort line = 0, ushort column = 0)
        {
            Type = type;
            Text = text;
            Literal = literal;
            LineInSourceCode = line;
            ColumnInSourceCode = column;
        }

        public override string ToString()
        {
            if (Literal != null) return $"{Type}: {Text} [{Literal}]";
            return string.IsNullOrEmpty(Text) ? Type.ToString() : $"{Type}: {Text}";
        }
    }
}