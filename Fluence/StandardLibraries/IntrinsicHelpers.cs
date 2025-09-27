using Fluence.RuntimeTypes;
using static Fluence.FluenceVirtualMachine;

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

        internal static string ConvertRuntimeValueToString(FluenceVirtualMachine vm, RuntimeValue val)
        {
            if (val.ObjectReference is IFluenceObject fluenceObject)
            {
                if (fluenceObject.TryGetIntrinsicMethod("to_string__0", out IntrinsicRuntimeMethod? intrinsicMethod))
                {
                    return intrinsicMethod(vm, val).ToString();
                }
            }

            if (val.ObjectReference is InstanceObject instance && instance.Class.Functions.TryGetValue("to_string__0", out FunctionValue func))
            {
                RuntimeValue result = vm.ExecuteManualMethodCall(instance, func);

                // The struct's to_string() must return a string object. If not, it's a runtime error.
                if (result.ObjectReference is not StringObject stringResult)
                {
                    throw vm.ConstructRuntimeException($"Runtime Error: The 'to_string' method for '{instance.Class.Name}' must return a string, but it returned a '{GetDetailedTypeName(result)}'.");
                }
                return stringResult.Value;
            }

            return val.ToString();
        }
    }
}