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

            // Core Fluence types like string, list and their methods.
            RegisterCoreTypes();
        }

        /// <summary>
        /// Registers functions that should be available in the global scope.
        /// </summary>
        private void RegisterGlobalIntrinsics()
        {
            var global = _parser.CurrentParserStateGlobalScope;

            global.Declare("print", new FunctionSymbol("print", 1, (args) =>
            {
                if (args.Count < 1)
                {
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(args[0]?.ToString() ?? "nil");
                }
                return new NilValue();
            }));

            global.Declare("input", new FunctionSymbol("input", 0, (args) =>
            {
                return new StringValue(Console.ReadLine() ?? string.Empty);
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

        /// <summary>
        /// Creates and registers intrinsic struct types such as list, string and their intrinsic methods.
        /// </summary>
        private void RegisterCoreTypes()
        {
            var global = _parser.CurrentParserStateGlobalScope;

            var listSymbol = new StructSymbol("List");

            listSymbol.Functions.Add("length", new FunctionValue("length", 0, (args) =>
            {
                if (args[0] is ListValue list)
                {
                    return new NumberValue(list.Elements.Count);
                }
                return new NumberValue(0);
            }));
        }
    }
}