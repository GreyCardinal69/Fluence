using static Fluence.FluenceInterpreter;

namespace Fluence
{
    /// <summary>
    /// Registers core global functions that are always available in any script.
    /// </summary>
    internal static class GlobalLibrary
    {
        internal static void Register(FluenceScope globalScope, TextOutputMethod outputLine, TextInputMethod input, TextOutputMethod output)
        {
            // Arity -100 means dynamic argument count.
            globalScope.Declare("to_int", new FunctionSymbol("to_int", 1, (args) =>
            {
                return new NumberValue(Convert.ToInt32(args[0].GetValue()));
            }, null!, globalScope));

            globalScope.Declare("str", new FunctionSymbol("str", 1, (args) =>
            {
                return new StringValue(args[0].ToFluenceString());
            }, null!, globalScope));
        }
    }
}