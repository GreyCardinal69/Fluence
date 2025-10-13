using Fluence.Global;
using static Fluence.FluenceInterpreter;

namespace Fluence
{
    /// <summary>
    /// Manages the registration of standard library modules (intrinsics) for Fluence.
    /// It handles on-demand loading of libraries when 'use' statements are encountered by the parser.
    /// </summary>
    internal sealed class FluenceIntrinsics
    {
        /// <summary>
        /// A dictionary mapping namespace names to their registration actions.
        /// </summary>
        private readonly Dictionary<string, Action<FluenceScope>> _libraryRegistry = new();
        private readonly FluenceParser _parser;

        private readonly TextOutputMethod _outputLine;
        private readonly TextOutputMethod _output;
        private readonly TextInputMethod _input;

        internal FluenceIntrinsics(FluenceParser parser, TextOutputMethod outputLine, TextInputMethod input, TextOutputMethod output)
        {
            _parser = parser;
            _outputLine = outputLine;
            _input = input;
            _output = output;

            // Pre-register all known standard libraries.
            _libraryRegistry[FluenceMath.NamespaceName] = FluenceMath.Register;
            _libraryRegistry[FluenceIO.NamespaceName] = FluenceIO.Register;

            _libraryRegistry[FluenceUnsafe.NamespaceName] = (scope) =>
            {
                FluenceUnsafe.Register(scope, _outputLine, _input, _output);
            };
        }

        /// <summary>
        /// Registers the core global functions that are always available.
        /// This should be called once when the parser is initialized.
        /// </summary>
        internal void RegisterCoreGlobals()
        {
            GlobalLibrary.Register(_parser.CurrentParserStateGlobalScope, _outputLine, _input, _output);
        }

        /// <summary>
        /// Attempts to find and register a standard library namespace.
        /// This method is called by the parser when it encounters a 'use' statement.
        /// </summary>
        /// <param name="namespaceName">The name of the namespace to load.</param>
        /// <returns>The newly created and populated scope if the library was found, otherwise null.</returns>
        internal FluenceScope? Use(string namespaceName)
        {
            if (_libraryRegistry.TryGetValue(namespaceName, out Action<FluenceScope>? registrationAction))
            {
                if (_parser.CurrentParserStateGlobalScope.DeclaredSymbolNames.Contains(namespaceName.GetHashCode()))
                {
                    return null;
                }

                FluenceScope newNamespaceScope = new FluenceScope(_parser.CurrentParserStateGlobalScope, namespaceName);
                registrationAction(newNamespaceScope);
                _parser.AddNameSpace(newNamespaceScope);

                return newNamespaceScope;
            }

            // No standard library with that name was found.
            return null;
        }
    }
}