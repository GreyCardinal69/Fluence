using static Fluence.FluenceInterpreter;

namespace Fluence
{
    /// <summary>
    /// Registers intrinsic functions for the 'FluenceIO' namespace.
    /// </summary>
    internal static class FluenceIO
    {
        internal const string NamespaceName = "FluenceIO";

        internal static void Register(FluenceScope ioNamespace, TextOutputMethod outputLine, TextInputMethod input, TextOutputMethod output)
        {
            // Basic console methods.
            ioNamespace.Declare("printl", new FunctionSymbol("printl", 1, (args) =>
            {
                string message = (args.Count < 1) ? "" : args[0]?.ToFluenceString() ?? "nil";
                outputLine(message);
                return new NilValue();
            }));

            ioNamespace.Declare("print", new FunctionSymbol("print", 1, (args) =>
            {
                string message = (args.Count < 1) ? "" : args[0]?.ToFluenceString() ?? "nil";
                output(message);
                return new NilValue();
            }));

            ioNamespace.Declare("input", new FunctionSymbol("input", 0, (args) =>
            {
                return new StringValue(input() ?? "");
            }));

            ioNamespace.Declare("readAndClear", new FunctionSymbol("readAndClear", 0, (args) =>
            {
                Console.ReadLine();
                Console.Clear();
                return new NilValue();
            }));

            ioNamespace.Declare("clear", new FunctionSymbol("clear", 0, (args) =>
            {
                Console.Clear();
                return new NilValue();
            }));


            //
            //      The "File" static struct managing File creation, read/write and other miscellanea operations.
            //

            StructSymbol file = new StructSymbol("File");
            ioNamespace.Declare("File", file);

            file.StaticIntrinsics.Add("write", new FunctionSymbol("write", 2, (args) =>
            {
                if (args.Count < 2 || args[0] is not StringValue path || args[1] is not StringValue content)
                    throw new FluenceRuntimeException("File.write() expects two arguments, a path (string) and optional content (string).");

                File.WriteAllText(path.Value, content.Value);
                return new NilValue();
            }));

            file.StaticIntrinsics.Add("appendText", new FunctionSymbol("appendText", 2, (args) =>
            {
                if (args.Count < 2 || args[0] is not StringValue path || args[1] is not StringValue content)
                    throw new FluenceRuntimeException("File.appendText() expects two arguments, a path (string) and optional content (string).");

                File.AppendAllText(path.Value, content.Value);
                return new NilValue();
            }));

            file.StaticIntrinsics.Add("move", new FunctionSymbol("move", 2, (args) =>
            {
                if (args.Count < 2 || args[0] is not StringValue path || args[1] is not StringValue path2)
                    throw new FluenceRuntimeException("File.move() expects two arguments, the old path (string) and new path (string).");

                File.Move(path.Value, path2.Value);
                return new NilValue();
            }));

            file.StaticIntrinsics.Add("read", new FunctionSymbol("read", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not StringValue path)
                    throw new FluenceRuntimeException("File.read() expects one argument, a path (string).");

                return new StringValue(File.ReadAllText(path.Value));
            }));

            file.StaticIntrinsics.Add("create", new FunctionSymbol("create", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not StringValue path)
                    throw new FluenceRuntimeException("File.create() expects one argument, a path (string).");

                File.Create(path.Value);
                return new NilValue();
            }));

            file.StaticIntrinsics.Add("delete", new FunctionSymbol("delete", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not StringValue path)
                    throw new FluenceRuntimeException("File.delete() expects one argument, a path (string).");

                File.Delete(path.Value);
                return new NilValue();
            }));

            file.StaticIntrinsics.Add("exists", new FunctionSymbol("exists", 1, (args) =>
            {
                if (args.Count < 1 || args[0] is not StringValue path)
                    throw new FluenceRuntimeException("File.exists() expects one argument, a path (string).");

                return new BooleanValue(File.Exists(path.Value));
            }));
        }
    }
}