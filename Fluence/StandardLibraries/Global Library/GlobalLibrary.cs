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

            globalScope.Declare("printl__0".GetHashCode(), new FunctionSymbol("printl__0", 0, (vm, argCount) =>
            {
                outputLine("");
                return nilResult;
            }, globalScope, []));

            globalScope.Declare("printl__1".GetHashCode(), new FunctionSymbol("printl__1", 1, (vm, argCount) =>
            {
                RuntimeValue rv = vm.PopStack();
                outputLine(ConvertRuntimeValueToString(vm, rv));
                return nilResult;
            }, globalScope, ["content"]));

            globalScope.Declare("print__0".GetHashCode(), new FunctionSymbol("print__0", 0, (vm, argCount) =>
            {
                return nilResult;
            }, globalScope, []));

            globalScope.Declare("print__1".GetHashCode(), new FunctionSymbol("print__1", 1, (vm, argCount) =>
            {
                RuntimeValue rv = vm.PopStack();
                output(ConvertRuntimeValueToString(vm, rv));
                return nilResult;
            }, globalScope, ["content"]));

            globalScope.Declare("input__0".GetHashCode(), new FunctionSymbol("input__0", 0, (vm, argCount) =>
            {
                return vm.ResolveStringObjectRuntimeValue(input() ?? "");
            }, globalScope, []));

            globalScope.Declare("readAndClear__0".GetHashCode(), new FunctionSymbol("readAndClear__0", 0, (vm, argCount) =>
            {
                Console.ReadLine();
                Console.Clear();
                return nilResult;
            }, globalScope, []));

            globalScope.Declare("clear__0".GetHashCode(), new FunctionSymbol("clear__0", 0, (vm, argCount) =>
            {
                Console.Clear();
                return nilResult;
            }, globalScope, []));

            //
            //      ==!!==
            //      Random functions.
            //

            StructSymbol random = new StructSymbol("Random", globalScope);
            globalScope.Declare("Random".GetHashCode(), random);

            random.StaticIntrinsics.Add("between_exclusive__2", new FunctionSymbol("between_2", 2, (vm, argCount) =>
            {
                Random rand = new Random();

                RuntimeValue max = vm.PopStack();
                RuntimeValue min = vm.PopStack();

                if (min.Type != RuntimeValueType.Number || max.Type != RuntimeValueType.Number)
                {
                    return vm.SignalRecoverableErrorAndReturnNil("Random.between_exclusive expects two integer arguments for maximum and minimum.", Exceptions.RuntimeExceptionType.NonSpecific);
                }

                // min + 1 to get exclusive min, max is already exclusive.
                return new RuntimeValue(rand.Next(min.IntValue + 1, max.IntValue));
            }, globalScope, ["min", "max"]));

            random.StaticIntrinsics.Add("between_inclusive__2", new FunctionSymbol("between_2", 2, (vm, argCount) =>
            {
                Random rand = new Random();

                RuntimeValue max = vm.PopStack();
                RuntimeValue min = vm.PopStack();

                if (min.Type != RuntimeValueType.Number || max.Type != RuntimeValueType.Number)
                {
                    return vm.SignalRecoverableErrorAndReturnNil("Random.between_inclusive expects two integer arguments for maximum and minimum.", Exceptions.RuntimeExceptionType.NonSpecific);
                }

                // max + 1 to get exclusive max.
                return new RuntimeValue(rand.Next(min.IntValue, max.IntValue + 1));
            }, globalScope, ["min", "max"]));

            random.StaticIntrinsics.Add("random", new FunctionSymbol("random", 0, (vm, argCount) =>
            {
                Random rand = new Random();
                return new RuntimeValue(rand.NextSingle());
            }, globalScope, []));

            //
            //      ==!!==
            //      Time functions.
            //

            StructSymbol time = new StructSymbol("Time", globalScope);
            globalScope.Declare("Time".GetHashCode(), time);

            time.StaticIntrinsics.Add("now__0", new FunctionSymbol("now__0", 0, (vm, argCount) =>
            {
                double ms = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalMilliseconds;
                return new RuntimeValue(ms);
            }, globalScope, []));

            time.StaticIntrinsics.Add("utc_now__0", new FunctionSymbol("utc_now__0", 0, (vm, argCount) =>
            {
                return vm.ResolveStringObjectRuntimeValue(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            }, globalScope, []));

            time.StaticIntrinsics.Add("sleep__1", new FunctionSymbol("sleep__1", 1, (vm, argCount) =>
            {
                RuntimeValue msVal = vm.PopStack();
                if (msVal.Type != RuntimeValueType.Number)
                    return vm.SignalRecoverableErrorAndReturnNil("Time.sleep expects a number (milliseconds).");

                int ms = msVal.ToInt();
                if (ms > 0) Thread.Sleep(ms);

                return RuntimeValue.Nil;
            }, globalScope, ["milliseconds"]));

            foreach (FunctionSymbol item in StopwatchWrapper.CreateConstructors(globalScope))
            {
                globalScope.Declare(item.Hash, item);
            }

            //
            //      ==!!==
            //      Conversion functions.
            //

            globalScope.Declare("to_int__1".GetHashCode(), new FunctionSymbol("to_int__1", 1, (vm, argCount) =>
            {
                RuntimeValue arg = vm.PopStack();

                if (TryConvertToNumeric<int>(ref arg, out int intResult))
                {
                    return new RuntimeValue(intResult);
                }

                if (arg.ObjectReference is CharObject chr)
                {
                    return new RuntimeValue(chr.Value);
                }

                return vm.SignalRecoverableErrorAndReturnNil($"Cannot convert value '{arg}' to an integer.");
            }, globalScope, ["Value"]));

            globalScope.Declare("to_double__1".GetHashCode(), new FunctionSymbol("to_double__1", 1, (vm, argCount) =>
            {
                RuntimeValue arg = vm.PopStack();

                if (TryConvertToNumeric<double>(ref arg, out double intResult))
                {
                    return new RuntimeValue(intResult);
                }

                return vm.SignalRecoverableErrorAndReturnNil($"Cannot convert value '{arg}' to a double.");
            }, globalScope, ["Value"]));

            globalScope.Declare("to_long__1".GetHashCode(), new FunctionSymbol("to_long__1", 1, (vm, argCount) =>
            {
                RuntimeValue arg = vm.PopStack();

                if (TryConvertToNumeric<long>(ref arg, out long intResult))
                {
                    return new RuntimeValue(intResult);
                }

                return vm.SignalRecoverableErrorAndReturnNil($"Cannot convert value '{arg}' to a long.");
            }, globalScope, ["Value"]));

            globalScope.Declare("to_float__1".GetHashCode(), new FunctionSymbol("to_float__1", 1, (vm, argCount) =>
            {
                RuntimeValue arg = vm.PopStack();

                if (TryConvertToNumeric<float>(ref arg, out float intResult))
                {
                    return new RuntimeValue(intResult);
                }

                return vm.SignalRecoverableErrorAndReturnNil($"Cannot convert value '{arg}' to a float.");
            }, globalScope, ["Value"]));

            globalScope.Declare("to_string__1".GetHashCode(), new FunctionSymbol("to_string__1", 1, (vm, argCount) =>
            {
                RuntimeValue arg = vm.PopStack();

                return vm.ResolveStringObjectRuntimeValue(arg.ToString());

            }, globalScope, ["Value"]));

            globalScope.Declare("to_bool__1".GetHashCode(), new FunctionSymbol("to_bool__1", 1, (vm, argCount) =>
            {
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
                globalScope.Declare(item.Hash, item);
            }

            foreach (FunctionSymbol item in StackWrapper.CreateConstructors(globalScope))
            {
                globalScope.Declare(item.Hash, item);
            }

            foreach (FunctionSymbol item in HashSetWrapper.CreateConstructors(globalScope))
            {
                globalScope.Declare(item.Hash, item);
            }

            foreach (FunctionSymbol item in DictionaryWrapper.CreateConstructors(globalScope))
            {
                globalScope.Declare(item.Hash, item);
            }

            // Core exception class and trait.

            foreach (FunctionSymbol item in ExceptionWrapper.CreateConstructors(globalScope))
            {
                globalScope.Declare(item.Hash, item);
            }

            IntrinsicStructSymbol exceptionSymbol = new IntrinsicStructSymbol("Exception");

            int exceptionHash = "exception".GetHashCode();
            int initHash = "init__1".GetHashCode();

            exceptionSymbol.ImplementedTraits.Add(exceptionHash);
            globalScope.Declare("Exception".GetHashCode(), exceptionSymbol);

            // Base exception trait a user's custom exception class can inherit from to make it work with the 'throw' keyword.
            TraitSymbol exceptionTrait = new TraitSymbol("exception");
            exceptionTrait.FieldSignatures.Add("message".GetHashCode(), "message");
            exceptionTrait.DefaultFieldValuesAsTokens.Add("message", []);
            exceptionTrait.FunctionSignatures.Add(initHash, new TraitSymbol.FunctionSignature()
            {
                Arity = 1,
                Hash = initHash,
                Name = "init__1",
                IsAConstructor = true,
            });

            globalScope.Declare(exceptionHash, exceptionTrait);

            // Others.

            // This simply returns the name of the type, not the type metadata.
            globalScope.Declare("type_of__1".GetHashCode(), new FunctionSymbol("typeof__1", 1, (vm, argCount) =>
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