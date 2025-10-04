using Fluence.RuntimeTypes;
using System.Globalization;
using System.Numerics;
using static Fluence.FluenceInterpreter;
using static Fluence.IntrinsicHelpers;

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

            //
            //      ==!!==
            //      Console functions.
            //

            globalScope.Declare("printl__0", new FunctionSymbol("printl__0", 0, (vm, argCount) =>
            {
                outputLine("");
                return nilResult;
            }, globalScope, []));

            globalScope.Declare("printl__1", new FunctionSymbol("printl__1", 1, (vm, argCount) =>
            {
                RuntimeValue rv = vm.PopStack();
                outputLine(ConvertRuntimeValueToString(vm, rv));
                return nilResult;
            }, globalScope, ["content"]));

            globalScope.Declare("print__0", new FunctionSymbol("print__0", 0, (vm, argCount) =>
            {
                return nilResult;
            }, globalScope, []));

            globalScope.Declare("print__1", new FunctionSymbol("print__1", 1, (vm, argCount) =>
            {
                RuntimeValue rv = vm.PopStack();
                output(ConvertRuntimeValueToString(vm, rv));
                return nilResult;
            }, globalScope, ["content"]));

            globalScope.Declare("input__0", new FunctionSymbol("input__0", 0, (vm, argCount) =>
            {
                return vm.ResolveStringObjectRuntimeValue(input() ?? "");
            }, globalScope, []));

            globalScope.Declare("readAndClear__0", new FunctionSymbol("readAndClear__0", 0, (vm, argCount) =>
            {
                Console.ReadLine();
                Console.Clear();
                return nilResult;
            }, globalScope, []));

            globalScope.Declare("clear__0", new FunctionSymbol("clear__0", 0, (vm, argCount) =>
            {
                Console.Clear();
                return nilResult;
            }, globalScope, []));

            //
            //      ==!!==
            //      Conversion functions.
            //

            globalScope.Declare("to_int__1", new FunctionSymbol("to_int__1", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw vm.ConstructRuntimeException("to_int() expects one string argument.");
                RuntimeValue arg = vm.PopStack();

                if (TryConvertToNumeric<int>(ref arg, out int intResult))
                {
                    return new RuntimeValue(intResult);
                }

                throw vm.ConstructRuntimeException($"Cannot convert value '{arg}' to an integer.");

            }, globalScope, ["Value"]));

            globalScope.Declare("to_double__1", new FunctionSymbol("to_double__1", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw vm.ConstructRuntimeException("to_double() expects one string argument.");
                RuntimeValue arg = vm.PopStack();

                if (TryConvertToNumeric<double>(ref arg, out double intResult))
                {
                    return new RuntimeValue(intResult);
                }

                throw vm.ConstructRuntimeException($"Cannot convert value '{arg}' to a double.");

            }, globalScope, ["Value"]));

            globalScope.Declare("to_long__1", new FunctionSymbol("to_long__1", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw vm.ConstructRuntimeException("to_long() expects one string argument.");
                RuntimeValue arg = vm.PopStack();

                if (TryConvertToNumeric<long>(ref arg, out long intResult))
                {
                    return new RuntimeValue(intResult);
                }

                throw vm.ConstructRuntimeException($"Cannot convert value '{arg}' to a long.");

            }, globalScope, ["Value"]));

            globalScope.Declare("to_float__1", new FunctionSymbol("to_float__1", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw vm.ConstructRuntimeException("to_float() expects one string argument.");
                RuntimeValue arg = vm.PopStack();

                if (TryConvertToNumeric<float>(ref arg, out float intResult))
                {
                    return new RuntimeValue(intResult);
                }

                throw vm.ConstructRuntimeException($"Cannot convert value '{arg}' to a float.");

            }, globalScope, ["Value"]));

            globalScope.Declare("to_string__1", new FunctionSymbol("to_string__1", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw vm.ConstructRuntimeException("to_string() expects one argument.");
                RuntimeValue arg = vm.PopStack();

                return vm.ResolveStringObjectRuntimeValue(arg.ToString());

            }, globalScope, ["Value"]));

            globalScope.Declare("to_bool__1", new FunctionSymbol("to_bool__1", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw vm.ConstructRuntimeException("to_bool() expects one numeric or string argument.");
                RuntimeValue arg = vm.PopStack();

                if (arg.ObjectReference is StringObject str)
                {
                    return new RuntimeValue(Convert.ToBoolean(str.Value));
                }

                return new RuntimeValue(Convert.ToBoolean(arg.IntValue));
            }, globalScope, ["Value"]));

            // Other classes.

            foreach (FunctionSymbol item in StringBuilderWrapper.CreateConstructors(globalScope))
            {
                globalScope.Declare(item.Name, item);
            }

            foreach (FunctionSymbol item in StackWrapper.CreateConstructors(globalScope))
            {
                globalScope.Declare(item.Name, item);
            }

            foreach (FunctionSymbol item in HashSetWrapper.CreateConstructors(globalScope))
            {
                globalScope.Declare(item.Name, item);
            }

            foreach (FunctionSymbol item in DictionaryWrapper.CreateConstructors(globalScope))
            {
                globalScope.Declare(item.Name, item);
            }

            // Others

            // This simply returns the name of the type, not the type metadata.
            globalScope.Declare("type_of__1", new FunctionSymbol("typeof__1", 1, (vm, argCount) =>
            {
                RuntimeValue rv = vm.PopStack();
                return vm.ResolveStringObjectRuntimeValue(GetRuntimeTypeName(rv));
            }, globalScope, ["object"]));
        }

        private static bool TryConvertToNumeric<T>(ref RuntimeValue value, out T result) where T : struct, INumber<T>
        {
            if (value.ObjectReference is StringObject str)
            {
                return T.TryParse(str.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
            }
            else if (value.ObjectReference is CharObject chr)
            {
                return T.TryParse(chr.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
            }
            else if (value.Type == RuntimeValueType.Number)
            {
                result = value.NumberType switch
                {
                    RuntimeNumberType.Int => T.CreateChecked(value.IntValue),
                    RuntimeNumberType.Long => T.CreateChecked(value.LongValue),
                    RuntimeNumberType.Double => T.CreateChecked(value.DoubleValue),
                    RuntimeNumberType.Float => T.CreateChecked(value.FloatValue),
                    _ => default
                };
                return true;
            }

            result = default;
            return false;
        }
    }
}