using Fluence.RuntimeTypes;

namespace Fluence
{
    internal static class IntrinsicHelpers
    {
        internal static string GetStringArg(FluenceVirtualMachine vm, string funcName)
        {
            RuntimeValue pathRv = vm.PopStack();
            if (pathRv.ObjectReference is not StringObject pathObj || string.IsNullOrEmpty(pathObj.Value))
            {
                throw vm.ConstructRuntimeException($"Invalid path for function: \"{funcName}\". Argument must be a non-empty string.");
            }
            return pathObj.Value;
        }

        internal static (string, string) GetTwoStringArgs(FluenceVirtualMachine vm, string funcName)
        {
            RuntimeValue arg2Rv = vm.PopStack();
            RuntimeValue arg1Rv = vm.PopStack();

            if (arg1Rv.ObjectReference is not StringObject arg1Obj || string.IsNullOrEmpty(arg1Obj.Value) ||
                arg2Rv.ObjectReference is not StringObject arg2Obj || string.IsNullOrEmpty(arg2Obj.Value))
            {
                throw vm.ConstructRuntimeException($"Invalid arguments for function: \"{funcName}\". Both arguments must be non-empty strings.");
            }
            return (arg1Obj.Value, arg2Obj.Value);
        }
    }
}