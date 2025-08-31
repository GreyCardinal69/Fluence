using Fluence;

/// <summary>
/// Registers intrinsic functions for the 'FluenceMath' namespace.
/// </summary>
internal static class FluenceMath
{
    internal const string NamespaceName = "FluenceMath";

    internal static void Register(FluenceScope mathNamespace)
    {
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
    }
}