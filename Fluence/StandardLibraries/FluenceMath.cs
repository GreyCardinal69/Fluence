namespace Fluence
{
    /// <summary>
    /// Registers intrinsic functions for the 'FluenceMath' namespace.
    /// </summary>
    internal static class FluenceMath
    {
        internal const string NamespaceName = "FluenceMath";

        internal static void Register(FluenceScope mathNamespace)
        {
            mathNamespace.Declare("Pi", new VariableSymbol("Pi", new NumberValue(Math.PI, NumberValue.NumberType.Double), true));

            mathNamespace.Declare("E", new VariableSymbol("E", new NumberValue(Math.E, NumberValue.NumberType.Double), true));

            mathNamespace.Declare("Tau", new VariableSymbol("Tau", new NumberValue(Math.Tau, NumberValue.NumberType.Double), true));

            mathNamespace.Declare("cos", new FunctionSymbol("cos", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("cos() expects one numerical argument.");

                return new NumberValue(Math.Cos(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double);
            }));

            mathNamespace.Declare("sin", new FunctionSymbol("sin", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("sin() expects one numerical argument.");

                return new NumberValue(Math.Sin(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double);
            }));

            mathNamespace.Declare("abs", new FunctionSymbol("abs", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("abs() expects one numerical argument.");

                return num.Type switch
                {
                    NumberValue.NumberType.Integer => new NumberValue(Math.Abs(Convert.ToInt32(num.Value))),
                    NumberValue.NumberType.Float => new NumberValue(Math.Abs(Convert.ToSingle(num.Value)), NumberValue.NumberType.Float),
                    _ => new NumberValue(Math.Abs(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double),
                };
            }));

            mathNamespace.Declare("acos", new FunctionSymbol("acos", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("acos() expects one numerical argument.");

                return new NumberValue(Math.Acos(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double);
            }));

            mathNamespace.Declare("acosh", new FunctionSymbol("acosh", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("acosh() expects one numerical argument.");

                return new NumberValue(Math.Acosh(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double);
            }));

            mathNamespace.Declare("asin", new FunctionSymbol("asin", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("asin() expects one numerical argument.");

                return new NumberValue(Math.Asin(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double);
            }));

            mathNamespace.Declare("asinh", new FunctionSymbol("asinh", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("asinh() expects one numerical argument.");

                return new NumberValue(Math.Asinh(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double);
            }));

            mathNamespace.Declare("atan", new FunctionSymbol("atan", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("atan() expects one numerical argument.");

                return new NumberValue(Math.Atan(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double);
            }));

            mathNamespace.Declare("atan2", new FunctionSymbol("atan2", 2, (args) =>
            {
                if (args.Count < 2 || args[0] is not NumberValue num || args[1] is not NumberValue num2)
                    throw new FluenceRuntimeException("atan2() expects two numerical argumenta.");

                return new NumberValue(Math.Atan2(Convert.ToDouble(num.Value), Convert.ToDouble(num2)), NumberValue.NumberType.Double);
            }));

            mathNamespace.Declare("atanh", new FunctionSymbol("atanh", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("atanh() expects one numerical argument.");

                return new NumberValue(Math.Atanh(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double);
            }));

            mathNamespace.Declare("ceil", new FunctionSymbol("ceil", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("ceil() expects one numerical argument.");

                return num.Type switch
                {
                    NumberValue.NumberType.Integer => num,
                    NumberValue.NumberType.Float => new NumberValue(Math.Ceiling(Convert.ToSingle(num.Value)), NumberValue.NumberType.Float),
                    _ => new NumberValue(Math.Ceiling(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double),
                };
            }));

            mathNamespace.Declare("clamp", new FunctionSymbol("clamp", 3, (args) =>
            {
                if (args.Count < 3 || args[0] is not NumberValue num || args[1] is not NumberValue num2 || args[2] is not NumberValue num3)
                    throw new FluenceRuntimeException("clamp() expects three numerical argumenta.");

                return num.Type switch
                {
                    NumberValue.NumberType.Integer => new NumberValue(Math.Clamp(Convert.ToInt32(num.Value), Convert.ToInt32(num2.Value), Convert.ToInt32(num3.Value))),
                    NumberValue.NumberType.Float => new NumberValue(Math.Clamp(Convert.ToSingle(num.Value), Convert.ToSingle(num2.Value), Convert.ToSingle(num3.Value)), NumberValue.NumberType.Float),
                    _ => new NumberValue(Math.Clamp(Convert.ToDouble(num.Value), Convert.ToDouble(num2.Value), Convert.ToDouble(num3.Value)), NumberValue.NumberType.Double),
                };
            }));

            mathNamespace.Declare("cosh", new FunctionSymbol("cosh", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("cosh() expects one numerical argument.");

                return new NumberValue(Math.Cosh(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double);
            }));

            mathNamespace.Declare("exp", new FunctionSymbol("exp", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("exp() expects one numerical argument.");

                return new NumberValue(Math.Exp(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double);
            }));

            mathNamespace.Declare("floor", new FunctionSymbol("floor", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("floor() expects one numerical argument.");

                return new NumberValue(Math.Floor(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double);
            }));

            mathNamespace.Declare("log", new FunctionSymbol("log", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("log() expects one numerical argument.");

                return new NumberValue(Math.Log(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double);
            }));

            mathNamespace.Declare("log10", new FunctionSymbol("log10", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("log10() expects one numerical argument.");

                return new NumberValue(Math.Log10(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double);
            }));

            mathNamespace.Declare("log2", new FunctionSymbol("log2", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("log2() expects one numerical argument.");

                return new NumberValue(Math.Log2(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double);
            }));

            mathNamespace.Declare("max", new FunctionSymbol("max", 2, (args) =>
            {
                if (args.Count < 2 || args[0] is not NumberValue num || args[1] is not NumberValue num2)
                    throw new FluenceRuntimeException("max() expects three numerical argumenta.");

                return num.Type switch
                {
                    NumberValue.NumberType.Integer => new NumberValue(Math.Max(Convert.ToInt32(num.Value), Convert.ToInt32(num2.Value))),
                    NumberValue.NumberType.Float => new NumberValue(Math.Max(Convert.ToSingle(num.Value), Convert.ToSingle(num2.Value)), NumberValue.NumberType.Float),
                    _ => new NumberValue(Math.Max(Convert.ToDouble(num.Value), Convert.ToDouble(num2.Value)), NumberValue.NumberType.Double),
                };
            }));

            mathNamespace.Declare("min", new FunctionSymbol("min", 2, (args) =>
            {
                if (args.Count < 2 || args[0] is not NumberValue num || args[1] is not NumberValue num2)
                    throw new FluenceRuntimeException("min() expects three numerical argumenta.");

                return num.Type switch
                {
                    NumberValue.NumberType.Integer => new NumberValue(Math.Min(Convert.ToInt32(num.Value), Convert.ToInt32(num2.Value))),
                    NumberValue.NumberType.Float => new NumberValue(Math.Min(Convert.ToSingle(num.Value), Convert.ToSingle(num2.Value)), NumberValue.NumberType.Float),
                    _ => new NumberValue(Math.Min(Convert.ToDouble(num.Value), Convert.ToDouble(num2.Value)), NumberValue.NumberType.Double),
                };
            }));

            mathNamespace.Declare("round", new FunctionSymbol("round", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("round() expects one numerical argument.");

                return num.Type switch
                {
                    NumberValue.NumberType.Integer => num,
                    NumberValue.NumberType.Float => new NumberValue(Math.Round(Convert.ToSingle(num.Value)), NumberValue.NumberType.Float),
                    _ => new NumberValue(Math.Round(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double),
                };
            }));

            mathNamespace.Declare("sinh", new FunctionSymbol("sinh", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("sinh() expects one numerical argument.");

                return new NumberValue(Math.Sinh(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double);
            }));

            mathNamespace.Declare("sqrt", new FunctionSymbol("sqrt", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("sqrt() expects one numerical argument.");

                return new NumberValue(Math.Sqrt(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double);
            }));

            mathNamespace.Declare("tan", new FunctionSymbol("tan", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("tan() expects one numerical argument.");

                return new NumberValue(Math.Tan(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double);
            }));

            mathNamespace.Declare("tanh", new FunctionSymbol("tanh", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                    throw new FluenceRuntimeException("tanh() expects one numerical argument.");

                return new NumberValue(Math.Tanh(Convert.ToDouble(num.Value)), NumberValue.NumberType.Double);
            }));
        }
    }
}