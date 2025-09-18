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

            //
            //      The "File" static struct.
            //

            StructSymbol file = new StructSymbol("File");
            ioNamespace.Declare("File", file);

            file.StaticIntrinsics.Add("write__2", new FunctionSymbol("write__2", 2, (vm, argCount) =>
            {
                RuntimeValue contentRv = vm.PopStack();
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj || contentRv.ObjectReference is not StringObject contentObj)
                    throw new FluenceRuntimeException("File.write() expects two string arguments.");

                File.WriteAllText(pathObj.Value, contentObj.Value);
                return nilResult;
            }, null!, ioNamespace));

            file.StaticIntrinsics.Add("appendText__2", new FunctionSymbol("appendText__2", 2, (vm, argCount) =>
            {
                RuntimeValue contentRv = vm.PopStack();
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj || contentRv.ObjectReference is not StringObject contentObj)
                    throw new FluenceRuntimeException("File.appendText() expects two string arguments.");

                File.AppendAllText(pathObj.Value, contentObj.Value);
                return nilResult;
            }, null!, ioNamespace));

            file.StaticIntrinsics.Add("move__2", new FunctionSymbol("move__2", 2, (vm, argCount) =>
            {
                RuntimeValue newPathRv = vm.PopStack();
                RuntimeValue oldPathRv = vm.PopStack();

                if (oldPathRv.ObjectReference is not StringObject oldPathObj || newPathRv.ObjectReference is not StringObject newPathObj)
                    throw new FluenceRuntimeException("File.move() expects two string arguments.");

                File.Move(oldPathObj.Value, newPathObj.Value);
                return nilResult;
            }, null!, ioNamespace));

            file.StaticIntrinsics.Add("read__1", new FunctionSymbol("read__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw new FluenceRuntimeException("File.read() expects a string argument.");

                return vm.ResolveStringObjectRuntimeValue(File.ReadAllText(pathObj.Value));
            }, null!, ioNamespace));

            file.StaticIntrinsics.Add("create__1", new FunctionSymbol("create__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw new FluenceRuntimeException("File.create() expects a string argument.");

                File.Create(pathObj.Value).Close();
                return nilResult;
            }, null!, ioNamespace));

            file.StaticIntrinsics.Add("delete__1", new FunctionSymbol("delete__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw new FluenceRuntimeException("File.delete() expects a string argument.");

                File.Delete(pathObj.Value);
                return nilResult;
            }, null!, ioNamespace));

            file.StaticIntrinsics.Add("exists__1", new FunctionSymbol("exists__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw new FluenceRuntimeException("File.exists() expects a string argument.");

                return new RuntimeValue(File.Exists(pathObj.Value));
            }, null!, ioNamespace));
        }
    }
}