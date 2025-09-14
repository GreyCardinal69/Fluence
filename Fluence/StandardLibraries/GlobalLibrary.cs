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
            globalScope.Declare("to_int", new FunctionSymbol("to_int", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("to_int() expects one string argument.");
                RuntimeValue val = vm.PopStack();
                return new RuntimeValue(Convert.ToInt32(((StringObject)val.ObjectReference).Value));
            }, null!, globalScope));
        }
    }
}