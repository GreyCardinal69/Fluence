using static Fluence.FluenceInterpreter;

namespace Fluence.Global
{
    /// <summary>
    /// Registers core global functions that are always available in any script.
    /// </summary>
    internal static class GlobalLibrary
    {
        internal static void Register(FluenceScope globalScope, TextOutputMethod outputLine, TextInputMethod input, TextOutputMethod output)
        {
            RuntimeValue nilResult = RuntimeValue.Nil;

            globalScope.Declare("printl__0", new FunctionSymbol("printl__0", 0, (vm, argCount) =>
            {
                outputLine("");
                return nilResult;
            }, [], globalScope));

            globalScope.Declare("printl__1", new FunctionSymbol("printl__1", 1, (vm, argCount) =>
            {
                RuntimeValue rv = vm.PopStack();
                outputLine(rv.ToString());
                return nilResult;
            }, ["content"], globalScope));

            globalScope.Declare("print__0", new FunctionSymbol("print__0", 0, (vm, argCount) =>
            {
                return nilResult;
            }, [], globalScope));

            globalScope.Declare("print__1", new FunctionSymbol("print__1", 1, (vm, argCount) =>
            {
                RuntimeValue rv = vm.PopStack();
                output(rv.ToString());
                return nilResult;
            }, ["content"], globalScope));

            globalScope.Declare("input__0", new FunctionSymbol("input__0", 0, (vm, argCount) =>
            {
                return vm.ResolveStringObjectRuntimeValue(input() ?? "");
            }, [], globalScope));

            globalScope.Declare("readAndClear__0", new FunctionSymbol("readAndClear__0", 0, (vm, argCount) =>
            {
                Console.ReadLine();
                Console.Clear();
                return nilResult;
            }, [], globalScope));

            globalScope.Declare("clear__0", new FunctionSymbol("clear__0", 0, (vm, argCount) =>
            {
                Console.Clear();
                return nilResult;
            }, [], globalScope));

            // Remove later maybe.
            globalScope.Declare("to_int__1", new FunctionSymbol("to_int__1", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw vm.ConstructRuntimeException("to_int() expects one string argument.");
                RuntimeValue val = vm.PopStack();
                return new RuntimeValue(Convert.ToInt32(((StringObject)val.ObjectReference).Value));
            }, ["Value"], globalScope));

            // StringBuilder.
            foreach (FunctionSymbol item in StringBuilderWrapper.CreateConstructors())
            {
                globalScope.Declare(item.Name, item);
            }



        }
    }
}