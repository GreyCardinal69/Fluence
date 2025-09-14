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
        private static double AsDouble(this RuntimeValue val)
        {
            if (val.Type != RuntimeValueType.Number)
            {
                throw new FluenceRuntimeException($"Expected a number, but got {FluenceVirtualMachine.GetDetailedTypeName(val)}.");
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

            mathNamespace.Declare("cos", new FunctionSymbol("cos", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("cos() expects one numerical argument.");
                return new RuntimeValue(Math.Cos(vm.PopStack().AsDouble()));
            }, null!, mathNamespace));

            mathNamespace.Declare("sin", new FunctionSymbol("sin", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("sin() expects one numerical argument.");
                return new RuntimeValue(Math.Sin(vm.PopStack().AsDouble()));
            }, null!, mathNamespace));

            mathNamespace.Declare("abs", new FunctionSymbol("abs", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("abs() expects one numerical argument.");
                RuntimeValue val = vm.PopStack();
                if (val.Type != RuntimeValueType.Number) throw new FluenceRuntimeException("abs() expects a numerical argument.");

                return val.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Abs(val.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Abs(val.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Abs(val.FloatValue)),
                    _ => new RuntimeValue(Math.Abs(val.DoubleValue)),
                };
            }, null!, mathNamespace));

            mathNamespace.Declare("acos", new FunctionSymbol("acos", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("acos() expects one numerical argument.");
                return new RuntimeValue(Math.Acos(vm.PopStack().AsDouble()));
            }, null!, mathNamespace));

            mathNamespace.Declare("acosh", new FunctionSymbol("acosh", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("acosh() expects one numerical argument.");
                return new RuntimeValue(Math.Acosh(vm.PopStack().AsDouble()));
            }, null!, mathNamespace));

            mathNamespace.Declare("asin", new FunctionSymbol("asin", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("asin() expects one numerical argument.");
                return new RuntimeValue(Math.Asin(vm.PopStack().AsDouble()));
            }, null!, mathNamespace));

            mathNamespace.Declare("asinh", new FunctionSymbol("asinh", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("asinh() expects one numerical argument.");
                return new RuntimeValue(Math.Asinh(vm.PopStack().AsDouble()));
            }, null!, mathNamespace));

            mathNamespace.Declare("atan", new FunctionSymbol("atan", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("atan() expects one numerical argument.");
                return new RuntimeValue(Math.Atan(vm.PopStack().AsDouble()));
            }, null!, mathNamespace));

            mathNamespace.Declare("atan2", new FunctionSymbol("atan2", 2, (vm, argCount) =>
            {
                if (argCount != 2) throw new FluenceRuntimeException("atan2() expects two numerical arguments.");
                RuntimeValue val2 = vm.PopStack();
                RuntimeValue val1 = vm.PopStack();
                return new RuntimeValue(Math.Atan2(val1.AsDouble(), val2.AsDouble()));
            }, null!, mathNamespace));

            mathNamespace.Declare("atanh", new FunctionSymbol("atanh", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("atanh() expects one numerical argument.");
                return new RuntimeValue(Math.Atanh(vm.PopStack().AsDouble()));
            }, null!, mathNamespace));

            mathNamespace.Declare("ceil", new FunctionSymbol("ceil", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("ceil() expects one numerical argument.");
                RuntimeValue val = vm.PopStack();
                if (val.Type != RuntimeValueType.Number) throw new FluenceRuntimeException("ceil() expects a numerical argument.");

                return val.NumberType switch
                {
                    RuntimeNumberType.Int => val,
                    RuntimeNumberType.Long => val,
                    RuntimeNumberType.Float => new RuntimeValue(Math.Ceiling(val.FloatValue)),
                    _ => new RuntimeValue(Math.Ceiling(val.DoubleValue)),
                };
            }, null!, mathNamespace));

            mathNamespace.Declare("clamp", new FunctionSymbol("clamp", 3, (vm, argCount) =>
            {
                if (argCount != 3) throw new FluenceRuntimeException("clamp() expects three numerical arguments.");
                var max = vm.PopStack();
                var min = vm.PopStack();
                RuntimeValue val = vm.PopStack();

                // Promote all to the type of the first argument for consistency
                return val.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Clamp(val.IntValue, min.IntValue, max.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Clamp(val.LongValue, min.LongValue, max.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Clamp(val.FloatValue, min.FloatValue, max.FloatValue)),
                    _ => new RuntimeValue(Math.Clamp(val.DoubleValue, min.DoubleValue, max.DoubleValue)),
                };
            }, null!, mathNamespace));

            mathNamespace.Declare("cosh", new FunctionSymbol("cosh", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("cosh() expects one numerical argument.");
                return new RuntimeValue(Math.Cosh(vm.PopStack().AsDouble()));
            }, null!, mathNamespace));

            mathNamespace.Declare("exp", new FunctionSymbol("exp", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("exp() expects one numerical argument.");
                return new RuntimeValue(Math.Exp(vm.PopStack().AsDouble()));
            }, null!, mathNamespace));

            mathNamespace.Declare("floor", new FunctionSymbol("floor", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("floor() expects one numerical argument.");
                RuntimeValue val = vm.PopStack();
                return new RuntimeValue(Math.Floor(val.AsDouble()));
            }, null!, mathNamespace));

            mathNamespace.Declare("log", new FunctionSymbol("log", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("log() expects one numerical argument.");
                return new RuntimeValue(Math.Log(vm.PopStack().AsDouble()));
            }, null!, mathNamespace));

            mathNamespace.Declare("log10", new FunctionSymbol("log10", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("log10() expects one numerical argument.");
                return new RuntimeValue(Math.Log10(vm.PopStack().AsDouble()));
            }, null!, mathNamespace));

            mathNamespace.Declare("log2", new FunctionSymbol("log2", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("log2() expects one numerical argument.");
                return new RuntimeValue(Math.Log2(vm.PopStack().AsDouble()));
            }, null!, mathNamespace));

            mathNamespace.Declare("max", new FunctionSymbol("max", 2, (vm, argCount) =>
            {
                if (argCount != 2) throw new FluenceRuntimeException("max() expects two numerical arguments.");
                RuntimeValue val2 = vm.PopStack();
                RuntimeValue val1 = vm.PopStack();

                return val1.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Max(val1.IntValue, val2.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Max(val1.LongValue, val2.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Max(val1.FloatValue, val2.FloatValue)),
                    _ => new RuntimeValue(Math.Max(val1.DoubleValue, val2.DoubleValue)),
                };
            }, null!, mathNamespace));

            mathNamespace.Declare("min", new FunctionSymbol("min", 2, (vm, argCount) =>
            {
                if (argCount != 2) throw new FluenceRuntimeException("min() expects two numerical arguments.");
                RuntimeValue val2 = vm.PopStack();
                RuntimeValue val1 = vm.PopStack();

                return val1.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Min(val1.IntValue, val2.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Min(val1.LongValue, val2.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Min(val1.FloatValue, val2.FloatValue)),
                    _ => new RuntimeValue(Math.Min(val1.DoubleValue, val2.DoubleValue)),
                };
            }, null!, mathNamespace));

            mathNamespace.Declare("round", new FunctionSymbol("round", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("round() expects one numerical argument.");
                RuntimeValue val = vm.PopStack();
                if (val.Type != RuntimeValueType.Number) throw new FluenceRuntimeException("round() expects a numerical argument.");

                return val.NumberType switch
                {
                    RuntimeNumberType.Int => val,
                    RuntimeNumberType.Long => val,
                    RuntimeNumberType.Float => new RuntimeValue(Math.Round(val.FloatValue)),
                    _ => new RuntimeValue(Math.Round(val.DoubleValue)),
                };
            }, null!, mathNamespace));

            mathNamespace.Declare("sinh", new FunctionSymbol("sinh", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("sinh() expects one numerical argument.");
                return new RuntimeValue(Math.Sinh(vm.PopStack().AsDouble()));
            }, null!, mathNamespace));

            mathNamespace.Declare("sqrt", new FunctionSymbol("sqrt", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("sqrt() expects one numerical argument.");
                return new RuntimeValue(Math.Sqrt(vm.PopStack().AsDouble()));
            }, null!, mathNamespace));

            mathNamespace.Declare("tan", new FunctionSymbol("tan", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("tan() expects one numerical argument.");
                return new RuntimeValue(Math.Tan(vm.PopStack().AsDouble()));
            }, null!, mathNamespace));

            mathNamespace.Declare("tanh", new FunctionSymbol("tanh", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("tanh() expects one numerical argument.");
                return new RuntimeValue(Math.Tanh(vm.PopStack().AsDouble()));
            }, null!, mathNamespace));
        }
    }
}