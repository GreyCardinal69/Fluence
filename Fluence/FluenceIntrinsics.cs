namespace Fluence
{
    /// <summary>
    /// Manages the registration of all built-in (intrinsic) functions and namespaces for the Fluence language.
    /// This class populates the initial scopes with the standard library.
    /// </summary>
    internal sealed class FluenceIntrinsics
    {
        private readonly FluenceParser _parser;

        /// <summary>
        /// Initializes a new instance of the <see cref="FluenceIntrinsics"/> class.
        /// </summary>
        internal FluenceIntrinsics(FluenceParser parser)
        {
            _parser = parser;
        }

        /// <summary>
        /// Registers all standard library functions and namespaces into the appropriate scopes.
        /// This method should be called once before parsing begins.
        /// </summary>
        internal void Register()
        {
            RegisterGlobalIntrinsics();

            // Fluence namespace oriented around something specific.
            RegisterMathNamespace();

            // For built in objects like list, their intrinsics like Length(), or Push() are inside their
            // Definition inside RuntimeValue.cs
        }

        /// <summary>
        /// Registers functions that should be available in the global scope.
        /// </summary>
        private void RegisterGlobalIntrinsics()
        {
            var global = _parser.CurrentParserStateGlobalScope;

            /// !! -100 is a placeholder value, it tell that this method, can accept a dynamic amount of arguments.

            global.Declare("printl", new FunctionSymbol("printl", -100, (args) =>
            {
                if (args.Count < 1)
                {
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(args[0]?.ToFluenceString() ?? "nil");
                }
                return new NilValue();
            }));

            global.Declare("input", new FunctionSymbol("input", 0, (args) =>
            {
                return new StringValue(Console.ReadLine()!);
            }));

            global.Declare("to_int", new FunctionSymbol("to_int", 1, (args) =>
            {
                return new NumberValue(Convert.ToInt32(args[0].GetValue()));
            }));

            global.Declare("str", new FunctionSymbol("str", 1, (args) =>
            {
                return new StringValue(args[0].ToFluenceString());
            }));
        }

        /// <summary>
        /// Creates and registers the 'FluenceMath' intrinsic namespace.
        /// </summary>
        private void RegisterMathNamespace()
        {
            var mathNs = new FluenceScope(_parser.CurrentParserStateGlobalScope, "FluenceMath");
            _parser.AddNameSpace(mathNs);

            mathNs.Declare("cos", new FunctionSymbol("cos", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                {
                    throw new FluenceRuntimeException("cos() expects one numerical argument.");
                }

                double radians = Convert.ToDouble(num.Value);
                return new NumberValue(Math.Cos(radians));
            }));

            mathNs.Declare("sin", new FunctionSymbol("sin", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not NumberValue num)
                {
                    throw new FluenceRuntimeException("sin() expects one numerical argument.");
                }

                double radians = Convert.ToDouble(num.Value);
                return new NumberValue(Math.Sin(radians));
            }));
        }
    }
}