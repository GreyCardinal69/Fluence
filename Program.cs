namespace Fluence
{
    class Program
    {
        static int Main(string[] args)
        {
#if DEBUG
            string source = File.ReadAllText($@"{Directory.GetCurrentDirectory()}\test.fl");

            FluenceInterpreter fluenceInterpreter = new FluenceInterpreter();

            if (fluenceInterpreter.Compile(source, true))
            {
                fluenceInterpreter.RunUntilDone();
            }

            return 1;
#endif
            if (args.Length == 0)
            {
                Console.WriteLine("Fluence Interpreter");
                Console.WriteLine("Usage: fluence -run <filepath.fl>");
                return 1;
            }

            if (args.Length == 2 && args[0].Equals("-run", StringComparison.OrdinalIgnoreCase))
            {
                string filePath = args[1];
                return RunFile(filePath);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Invalid command-line arguments.");
                Console.WriteLine("Usage: fluence -run <filepath.fl>");
                Console.ResetColor();
                return 1;
            }
        }

        private static int RunFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: The file '{filePath}' was not found.");
                Console.ResetColor();
                return 1;
            }

            try
            {
                string sourceCode = File.ReadAllText(filePath);

                var interpreter = new FluenceInterpreter();
                bool success = interpreter.Compile(sourceCode, true);

                if (success)
                {
                    interpreter.RunUntilDone();
                    return 0;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Script terminated due to compilation errors.");
                    Console.ResetColor();
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("An unexpected error occurred:");
                Console.WriteLine(ex.ToString());
                Console.ResetColor();
                return 1;
            }
        }
    }
}