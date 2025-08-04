namespace Testing_Chamber.Fluid
{
    internal static class FluenceKeywords
    {
        private readonly static string[] _keyWords =
        {
            "break",
            "continue",
            "if",
            "else",
            "for",
            "while",
            "loop",
            "in",
            "func",
            "nil",
            "return",
            "true",
            "false",
            "is",
            "not",
            "space",
            "use",
            "type",
            "struct",
            "enum"
        };

        internal static bool IsAKeyword( string key ) => _keyWords.Contains( key );
    }
}