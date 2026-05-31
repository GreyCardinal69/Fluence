namespace Fluence
{
    /// <summary>
    /// The single source of truth for the Fluence runtime version.
    public static class FluenceVersion
    {
        /// <summary>Breaking changes to the language or embedding API.</summary>
        public const int Major = 0;

        /// <summary>New features, backward-compatible.</summary>
        public const int Minor = 1;

        /// <summary>Bug fixes and performance improvements only.</summary>
        public const int Patch = 4;

        /// <summary>
        /// Release label.
        /// </summary>
        public const string PreRelease = "alpha";

        /// <summary>
        /// The full version string.
        /// </summary>
        public static readonly string Full = PreRelease.Length > 0
            ? $"{Major}.{Minor}.{Patch}-{PreRelease}"
            : $"{Major}.{Minor}.{Patch}";

        /// <summary>
        /// Bare version without pre-release label.
        /// </summary>
        public static readonly string Short = $"{Major}.{Minor}.{Patch}";
    }
}
