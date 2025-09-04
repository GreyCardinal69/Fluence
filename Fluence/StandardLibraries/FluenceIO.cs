using static Fluence.FluenceInterpreter;

namespace Fluence
{
    /// <summary>
    /// Registers intrinsic functions for the 'FluenceIO' namespace.
    /// </summary>
    internal static class FluenceIO
    {
        internal const string NamespaceName = "FluenceIO";

        internal static void Register(FluenceScope ioNamespace, TextOutputMethod outputLine, TextInputMethod input, TextOutputMethod output)
        {
            if (!ioNamespace.TryResolve("File", out var symbol) || symbol is not StructSymbol fileSymbol)
            {
                fileSymbol = new StructSymbol("File");
                ioNamespace.Declare("File", fileSymbol);
            }

            var printlSymbol = new FunctionSymbol("printl", 1, (args) =>
            {
                string message = (args.Count < 1) ? "" : args[0]?.ToFluenceString() ?? "nil";
                outputLine(message);
                return new NilValue();
            });

            fileSymbol.StaticIntrinsics.Add("printl", printlSymbol);
        }
    }
}