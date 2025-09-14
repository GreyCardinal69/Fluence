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
            ioNamespace.Declare("printl", new FunctionSymbol("printl", 1, (vm, argCount) =>
            {
                RuntimeValue rv = vm.PopStack();
                Value val = vm.ToValue(rv);

                string message = (argCount < 1) ? "" : val?.ToFluenceString() ?? "nil";
                outputLine(message);
                return new NilValue();
            }, null!, ioNamespace));

            ioNamespace.Declare("print", new FunctionSymbol("print", 1, (vm, argCount) =>
            {
                RuntimeValue rv = vm.PopStack();
                Value val = vm.ToValue(rv);

                string message = (argCount < 1) ? "" : val?.ToFluenceString() ?? "nil";
                output(message);
                return new NilValue();
            }, null!, ioNamespace));

            ioNamespace.Declare("input", new FunctionSymbol("input", 0, (vm, argCount) =>
            {
                return new StringValue(input() ?? "");
            }, null!, ioNamespace));

            ioNamespace.Declare("readAndClear", new FunctionSymbol("readAndClear", 0, (vm, argCount) =>
            {
                Console.ReadLine();
                Console.Clear();
                return new NilValue();
            }, null!, ioNamespace));

            ioNamespace.Declare("clear", new FunctionSymbol("clear", 0, (vm, argCount) =>
            {
                Console.Clear();
                return new NilValue();
            }, null!, ioNamespace));

            //
            //      The "File" static struct managing File creation, read/write and other miscellanea operations.
            //

            StructSymbol file = new StructSymbol("File");
            ioNamespace.Declare("File", file);

            file.StaticIntrinsics.Add("write", new FunctionSymbol("write", 2, (vm, argCount) =>
            {
                Value contentVal = vm.ToValue(vm.PopStack());
                Value pathVal = vm.ToValue(vm.PopStack());

                if (argCount < 2 || pathVal is not StringValue path || contentVal is not StringValue content)
                    throw new FluenceRuntimeException("File.write() expects two arguments: path (string) and content (string).");

                File.WriteAllText(path.Value, content.Value);
                return new NilValue();
            }, null!, ioNamespace));

            file.StaticIntrinsics.Add("appendText", new FunctionSymbol("appendText", 2, (vm, argCount) =>
            {
                Value contentVal = vm.ToValue(vm.PopStack());
                Value pathVal = vm.ToValue(vm.PopStack());

                if (argCount < 2 || pathVal is not StringValue path || contentVal is not StringValue content)
                    throw new FluenceRuntimeException("File.appendText() expects two arguments: path (string) and content (string).");

                File.AppendAllText(path.Value, content.Value);
                return new NilValue();
            }, null!, ioNamespace));

            file.StaticIntrinsics.Add("move", new FunctionSymbol("move", 2, (vm, argCount) =>
            {
                Value newPathVal = vm.ToValue(vm.PopStack());
                Value oldPathVal = vm.ToValue(vm.PopStack());

                if (argCount < 2 || oldPathVal is not StringValue oldPath || newPathVal is not StringValue newPath)
                    throw new FluenceRuntimeException("File.move() expects two arguments: old path (string) and new path (string).");

                File.Move(oldPath.Value, newPath.Value);
                return new NilValue();
            }, null!, ioNamespace));

            file.StaticIntrinsics.Add("read", new FunctionSymbol("read", 1, (vm, argCount) =>
            {
                Value pathVal = vm.ToValue(vm.PopStack());

                if (argCount < 1 || pathVal is not StringValue path)
                    throw new FluenceRuntimeException("File.read() expects one argument: path (string).");

                return new StringValue(File.ReadAllText(path.Value));
            }, null!, ioNamespace));

            file.StaticIntrinsics.Add("create", new FunctionSymbol("create", 1, (vm, argCount) =>
            {
                Value pathVal = vm.ToValue(vm.PopStack());

                if (argCount < 1 || pathVal is not StringValue path)
                    throw new FluenceRuntimeException("File.create() expects one argument: path (string).");

                // File.Create returns a FileStream which must be closed.
                File.Create(path.Value).Close();
                return new NilValue();
            }, null!, ioNamespace));

            file.StaticIntrinsics.Add("delete", new FunctionSymbol("delete", 1, (vm, argCount) =>
            {
                Value pathVal = vm.ToValue(vm.PopStack());

                if (argCount < 1 || pathVal is not StringValue path)
                    throw new FluenceRuntimeException("File.delete() expects one argument: path (string).");

                File.Delete(path.Value);
                return new NilValue();
            }, null!, ioNamespace));

            file.StaticIntrinsics.Add("exists", new FunctionSymbol("exists", 1, (vm, argCount) =>
            {
                Value pathVal = vm.ToValue(vm.PopStack());

                if (argCount < 1 || pathVal is not StringValue path)
                    throw new FluenceRuntimeException("File.exists() expects one argument: path (string).");

                return new BooleanValue(File.Exists(path.Value));
            }, null!, ioNamespace));
        }
    }
}