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
                if (argCount != 1)
                    throw new FluenceRuntimeException("to_int() expects exactly one argument.");

                var val = vm.ToValue(vm.PopStack());

                try
                {
                    return new NumberValue(Convert.ToInt32(val.GetValue()));
                }
                catch (FormatException)
                {
                    throw new FluenceRuntimeException($"Cannot convert value '{val.ToFluenceString()}' to an integer.");
                }
                catch (InvalidCastException)
                {
                    throw new FluenceRuntimeException($"Cannot convert type '{val.GetType().Name}' to an integer.");
                }
            }, null!, globalScope));

            globalScope.Declare("str", new FunctionSymbol("str", 1, (vm, argCount) =>
            {
                if (argCount != 1)
                    throw new FluenceRuntimeException("str() expects exactly one argument.");

                RuntimeValue rv = vm.PopStack();

                return new StringValue(rv.ToString());
            }, null!, globalScope));
        }
    }
}