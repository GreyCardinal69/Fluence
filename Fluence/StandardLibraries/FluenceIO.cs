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
            RuntimeValue nilResult = RuntimeValue.Nil;

            // Basic console methods.
            ioNamespace.Declare("printl", new FunctionSymbol("printl", 1, (vm, argCount) =>
            {
                if (argCount > 0)
                {
                    RuntimeValue rv = vm.PopStack();
                    outputLine(rv.ToString());
                }
                else
                {
                    outputLine(Environment.NewLine);
                }
                return nilResult;
            }, null!, ioNamespace));

            ioNamespace.Declare("print", new FunctionSymbol("print", 1, (vm, argCount) =>
            {
                if (argCount > 0)
                {
                    RuntimeValue rv = vm.PopStack();
                    output(rv.ToString());
                }
                return nilResult;
            }, null!, ioNamespace));

            ioNamespace.Declare("input", new FunctionSymbol("input", 0, (vm, argCount) =>
            {
                return vm.ResolveStringObjectRuntimeValue(input() ?? "");
            }, null!, ioNamespace));

            ioNamespace.Declare("readAndClear", new FunctionSymbol("readAndClear", 0, (vm, argCount) =>
            {
                Console.ReadLine();
                Console.Clear();
                return nilResult;
            }, null!, ioNamespace));

            ioNamespace.Declare("clear", new FunctionSymbol("clear", 0, (vm, argCount) =>
            {
                Console.Clear();
                return nilResult;
            }, null!, ioNamespace));

            //
            //      The "File" static struct managing File creation, read/write and other miscellanea operations.
            //

            StructSymbol file = new StructSymbol("File");
            ioNamespace.Declare("File", file);

            file.StaticIntrinsics.Add("write", new FunctionSymbol("write", 2, (vm, argCount) =>
            {
                if (argCount != 2) throw new FluenceRuntimeException("File.write() expects exactly two arguments: path (string) and content (string).");

                RuntimeValue contentRv = vm.PopStack();
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj || contentRv.ObjectReference is not StringObject contentObj)
                    throw new FluenceRuntimeException("File.write() expects two string arguments.");

                File.WriteAllText(pathObj.Value, contentObj.Value);
                return nilResult;
            }, null!, ioNamespace));

            file.StaticIntrinsics.Add("appendText", new FunctionSymbol("appendText", 2, (vm, argCount) =>
            {
                if (argCount != 2) throw new FluenceRuntimeException("File.appendText() expects exactly two arguments: path (string) and content (string).");

                RuntimeValue contentRv = vm.PopStack();
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj || contentRv.ObjectReference is not StringObject contentObj)
                    throw new FluenceRuntimeException("File.appendText() expects two string arguments.");

                File.AppendAllText(pathObj.Value, contentObj.Value);
                return nilResult;
            }, null!, ioNamespace));

            file.StaticIntrinsics.Add("move", new FunctionSymbol("move", 2, (vm, argCount) =>
            {
                if (argCount != 2) throw new FluenceRuntimeException("File.move() expects exactly two arguments: old path (string) and new path (string).");

                RuntimeValue newPathRv = vm.PopStack();
                RuntimeValue oldPathRv = vm.PopStack();

                if (oldPathRv.ObjectReference is not StringObject oldPathObj || newPathRv.ObjectReference is not StringObject newPathObj)
                    throw new FluenceRuntimeException("File.move() expects two string arguments.");

                File.Move(oldPathObj.Value, newPathObj.Value);
                return nilResult;
            }, null!, ioNamespace));

            file.StaticIntrinsics.Add("read", new FunctionSymbol("read", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("File.read() expects exactly one argument: path (string).");
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw new FluenceRuntimeException("File.read() expects a string argument.");

                return vm.ResolveStringObjectRuntimeValue(File.ReadAllText(pathObj.Value));
            }, null!, ioNamespace));

            file.StaticIntrinsics.Add("create", new FunctionSymbol("create", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("File.create() expects exactly one argument: path (string).");
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw new FluenceRuntimeException("File.create() expects a string argument.");

                File.Create(pathObj.Value).Close();
                return nilResult;
            }, null!, ioNamespace));

            file.StaticIntrinsics.Add("delete", new FunctionSymbol("delete", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("File.delete() expects exactly one argument: path (string).");
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw new FluenceRuntimeException("File.delete() expects a string argument.");

                File.Delete(pathObj.Value);
                return nilResult;
            }, null!, ioNamespace));

            file.StaticIntrinsics.Add("exists", new FunctionSymbol("exists", 1, (vm, argCount) =>
            {
                if (argCount != 1) throw new FluenceRuntimeException("File.exists() expects exactly one argument: path (string).");
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw new FluenceRuntimeException("File.exists() expects a string argument.");

                return new RuntimeValue(File.Exists(pathObj.Value));
            }, null!, ioNamespace));
        }
    }
}