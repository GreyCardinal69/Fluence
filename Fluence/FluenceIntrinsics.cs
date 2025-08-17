namespace Fluence
{
    internal class FluenceIntrinsics
    {
        internal FluenceParser _parser;

        internal FluenceIntrinsics(FluenceParser parser)
        {
            _parser = parser;
        }

        internal void RegisterIntrinsics()
        {
            FluenceScope global = _parser.CurrentParserStateGlobalScope;

            global.Declare("print", new FunctionSymbol("print", 1, (args) =>
            {
                if (args.Count > 0)
                {
                    Console.WriteLine(args[0]?.ToString() ?? "nil");
                }
                return new NilValue();
            }));

            var mathNs = new FluenceScope(global, "FluidMath");
            _parser.AddNameSpace(mathNs);

            mathNs.Declare("cos", new FunctionSymbol("cos", 1, (args) =>
            {
                if (args.Count > 0 && args[0] is NumberValue num)
                {
                    double radians = Convert.ToDouble(num.Value);
                    return new NumberValue(Math.Cos(radians));
                }
                return new NilValue();
            }));
        }
    }
}