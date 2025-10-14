using static Fluence.FluenceInterpreter;

namespace Fluence
{
    /// <summary>
    /// A standard library that provides functionality for the viewing and the direct manipulation of the Virtual Machine and other state.
    /// </summary>
    internal static class FluenceUnsafe
    {
        internal const string NamespaceName = "FluenceUnsafe";

        internal static void Register(FluenceScope unsafeScope, TextOutputMethod outputLine, TextInputMethod input, TextOutputMethod output)
        {
            StructSymbol byteCode = new StructSymbol("ByteCode", unsafeScope);
            unsafeScope.Declare("ByteCode".GetHashCode(), byteCode);
        }
    }
}