using Fluence.RuntimeTypes;
using Fluence.VirtualMachine;

namespace Fluence
{
    /// <summary>
    /// The default intrinsic namespace for common mathematical operations.
    /// </summary>
    internal static class FluenceMath
    {
        internal const string NamespaceName = "FluenceMath";

        /// <summary>
        /// A helper extension method to safely convert any numeric RuntimeValue to a double.
        /// </summary>
        private static double AsDouble(this RuntimeValue val, FluenceVirtualMachine vm)
        {
            if (val.Type != RuntimeValueType.Number)
            {
                return vm.SignalError<double>($"Expected a number, but got {FluenceVirtualMachine.GetDetailedTypeName(val)}.");
            }

            return val.NumberType switch
            {
                RuntimeNumberType.Int => val.IntValue,
                RuntimeNumberType.Long => val.LongValue,
                RuntimeNumberType.Float => val.FloatValue,
                _ => val.DoubleValue,
            };
        }

        /// <summary>
        /// Registers intrinsic functions for the 'FluenceMath' namespace.
        /// </summary>
        internal static void Register(FluenceScope mathNamespace)
        {
            mathNamespace.Declare("Pi", new VariableSymbol("Pi", new NumberValue(Math.PI, NumberValue.NumberType.Double), true));
            mathNamespace.Declare("E", new VariableSymbol("E", new NumberValue(Math.E, NumberValue.NumberType.Double), true));
            mathNamespace.Declare("Tau", new VariableSymbol("Tau", new NumberValue(Math.Tau, NumberValue.NumberType.Double), true));

            mathNamespace.Declare("cos__1", new FunctionSymbol("cos__1", 1, (vm, argCount) =>
            {
                return new RuntimeValue(Math.Cos(vm.PopStack().AsDouble(vm)));
            }, mathNamespace, ["angle"]));

            mathNamespace.Declare("sin__1", new FunctionSymbol("sin__1", 1, (vm, argCount) =>
            {
                return new RuntimeValue(Math.Sin(vm.PopStack().AsDouble(vm)));
            }, mathNamespace, ["angle"]));

            mathNamespace.Declare("abs__1", new FunctionSymbol("abs__1", 1, (vm, argCount) =>
            {
                RuntimeValue val = vm.PopStack();
                if (val.Type != RuntimeValueType.Number) return vm.SignalRecoverableErrorAndReturnNil("abs() expects a numerical argument.");

                return val.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Abs(val.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Abs(val.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Abs(val.FloatValue)),
                    _ => new RuntimeValue(Math.Abs(val.DoubleValue)),
                };
            }, mathNamespace, ["number"]));

            mathNamespace.Declare("acos__1", new FunctionSymbol("acos__1", 1, (vm, argCount) =>
            {
                return new RuntimeValue(Math.Acos(vm.PopStack().AsDouble(vm)));
            }, mathNamespace, ["value"]));

            mathNamespace.Declare("acosh__1", new FunctionSymbol("acosh__1", 1, (vm, argCount) =>
            {
                return new RuntimeValue(Math.Acosh(vm.PopStack().AsDouble(vm)));
            }, mathNamespace, ["value"]));

            mathNamespace.Declare("asin__1", new FunctionSymbol("asin__1", 1, (vm, argCount) =>
            {
                return new RuntimeValue(Math.Asin(vm.PopStack().AsDouble(vm)));
            }, mathNamespace, ["value"]));

            mathNamespace.Declare("asinh__1", new FunctionSymbol("asinh__1", 1, (vm, argCount) =>
            {
                return new RuntimeValue(Math.Asinh(vm.PopStack().AsDouble(vm)));
            }, mathNamespace, ["value"]));

            mathNamespace.Declare("atan__1", new FunctionSymbol("atan__1", 1, (vm, argCount) =>
            {
                return new RuntimeValue(Math.Atan(vm.PopStack().AsDouble(vm)));
            }, mathNamespace, ["value"]));

            mathNamespace.Declare("atan2__2", new FunctionSymbol("atan2__2", 2, (vm, argCount) =>
            {
                RuntimeValue x = vm.PopStack(); // Note: Atan2(y, x)
                RuntimeValue y = vm.PopStack();
                return new RuntimeValue(Math.Atan2(y.AsDouble(vm), x.AsDouble(vm)));
            }, mathNamespace, ["y", "x"]));

            mathNamespace.Declare("atanh__1", new FunctionSymbol("atanh__1", 1, (vm, argCount) =>
            {
                return new RuntimeValue(Math.Atanh(vm.PopStack().AsDouble(vm)));
            }, mathNamespace, ["value"]));

            mathNamespace.Declare("ceil__1", new FunctionSymbol("ceil__1", 1, (vm, argCount) =>
            {
                RuntimeValue val = vm.PopStack();
                if (val.Type != RuntimeValueType.Number) return vm.SignalRecoverableErrorAndReturnNil("ceil() expects a numerical argument.");

                return val.NumberType switch
                {
                    RuntimeNumberType.Int => val,
                    RuntimeNumberType.Long => val,
                    _ => new RuntimeValue(Math.Ceiling(val.AsDouble(vm))),
                };
            }, mathNamespace, ["number"]));

            mathNamespace.Declare("clamp__3", new FunctionSymbol("clamp__3", 3, (vm, argCount) =>
            {
                RuntimeValue max = vm.PopStack();
                RuntimeValue min = vm.PopStack();
                RuntimeValue val = vm.PopStack();
                return val.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Clamp(val.IntValue, min.IntValue, max.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Clamp(val.LongValue, min.LongValue, max.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Clamp(val.FloatValue, min.FloatValue, max.FloatValue)),
                    _ => new RuntimeValue(Math.Clamp(val.DoubleValue, min.DoubleValue, max.DoubleValue)),
                };
            }, mathNamespace, ["value", "min", "max"]));

            mathNamespace.Declare("cosh__1", new FunctionSymbol("cosh__1", 1, (vm, argCount) =>
            {
                return new RuntimeValue(Math.Cosh(vm.PopStack().AsDouble(vm)));
            }, mathNamespace, ["value"]));

            mathNamespace.Declare("exp__1", new FunctionSymbol("exp__1", 1, (vm, argCount) =>
            {
                return new RuntimeValue(Math.Exp(vm.PopStack().AsDouble(vm)));
            }, mathNamespace, ["number"]));

            mathNamespace.Declare("floor__1", new FunctionSymbol("floor__1", 1, (vm, argCount) =>
            {
                return new RuntimeValue(Math.Floor(vm.PopStack().AsDouble(vm)));
            }, mathNamespace, ["number"]));

            mathNamespace.Declare("log__1", new FunctionSymbol("log__1", 1, (vm, argCount) =>
            {
                return new RuntimeValue(Math.Log(vm.PopStack().AsDouble(vm)));
            }, mathNamespace, ["number"]));

            mathNamespace.Declare("log10__1", new FunctionSymbol("log10__1", 1, (vm, argCount) =>
            {
                return new RuntimeValue(Math.Log10(vm.PopStack().AsDouble(vm)));
            }, mathNamespace, ["number"]));

            mathNamespace.Declare("log2__1", new FunctionSymbol("log2__1", 1, (vm, argCount) =>
            {
                return new RuntimeValue(Math.Log2(vm.PopStack().AsDouble(vm)));
            }, mathNamespace, ["number"]));

            mathNamespace.Declare("max__2", new FunctionSymbol("max__2", 2, (vm, argCount) =>
            {
                RuntimeValue val2 = vm.PopStack();
                RuntimeValue val1 = vm.PopStack();
                return val1.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Max(val1.IntValue, val2.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Max(val1.LongValue, val2.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Max(val1.FloatValue, val2.FloatValue)),
                    _ => new RuntimeValue(Math.Max(val1.DoubleValue, val2.DoubleValue)),
                };
            }, mathNamespace, ["val1", "val2"]));

            mathNamespace.Declare("min__2", new FunctionSymbol("min__2", 2, (vm, argCount) =>
            {
                RuntimeValue val2 = vm.PopStack();
                RuntimeValue val1 = vm.PopStack();
                return val1.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Min(val1.IntValue, val2.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Min(val1.LongValue, val2.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Min(val1.FloatValue, val2.FloatValue)),
                    _ => new RuntimeValue(Math.Min(val1.DoubleValue, val2.DoubleValue)),
                };
            }, mathNamespace, ["val1", "val2"]));

            mathNamespace.Declare("min__3", new FunctionSymbol("min__3", 3, (vm, argCount) =>
            {
                RuntimeValue val3 = vm.PopStack();
                RuntimeValue val2 = vm.PopStack();
                RuntimeValue val1 = vm.PopStack();
                return val1.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Min(Math.Min(val1.IntValue, val2.IntValue), val3.IntValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Min(Math.Min(val1.FloatValue, val2.FloatValue), val3.FloatValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Min(Math.Min(val1.LongValue, val2.LongValue), val3.LongValue)),
                    _ => new RuntimeValue(Math.Min(Math.Min(val1.DoubleValue, val2.DoubleValue), val3.DoubleValue)),
                };
            }, mathNamespace, ["val1", "val2", "val3"]));

            mathNamespace.Declare("round__1", new FunctionSymbol("round__1", 1, (vm, argCount) =>
            {
                RuntimeValue val = vm.PopStack();
                if (val.Type != RuntimeValueType.Number) return vm.SignalRecoverableErrorAndReturnNil("round() expects a numerical argument.");

                return val.NumberType switch
                {
                    RuntimeNumberType.Int => val,
                    RuntimeNumberType.Long => val,
                    RuntimeNumberType.Float => new RuntimeValue(Math.Round(val.FloatValue)),
                    _ => new RuntimeValue(Math.Round(val.DoubleValue)),
                };
            }, mathNamespace, ["number"]));

            mathNamespace.Declare("sinh__1", new FunctionSymbol("sinh__1", 1, (vm, argCount) =>
            {
                return new RuntimeValue(Math.Sinh(vm.PopStack().AsDouble(vm)));
            }, mathNamespace, ["value"]));

            mathNamespace.Declare("sqrt__1", new FunctionSymbol("sqrt__1", 1, (vm, argCount) =>
            {
                return new RuntimeValue(Math.Sqrt(vm.PopStack().AsDouble(vm)));
            }, mathNamespace, ["number"]));

            mathNamespace.Declare("tan__1", new FunctionSymbol("tan__1", 1, (vm, argCount) =>
            {
                return new RuntimeValue(Math.Tan(vm.PopStack().AsDouble(vm)));
            }, mathNamespace, ["angle"]));

            mathNamespace.Declare("tanh__1", new FunctionSymbol("tanh__1", 1, (vm, argCount) =>
            {
                return new RuntimeValue(Math.Tanh(vm.PopStack().AsDouble(vm)));
            }, mathNamespace, ["value"]));
        }
    }
}