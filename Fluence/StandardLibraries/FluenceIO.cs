using Fluence.RuntimeTypes;
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
                    throw vm.ConstructRuntimeException("File.write() expects two string arguments.");

                if (string.IsNullOrEmpty(pathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid path, can not be null or empty.");
                }

                File.WriteAllText(pathObj.Value, contentObj.Value);
                return nilResult;
            }, ["path", "content"], ioNamespace));

            file.StaticIntrinsics.Add("appendText__2", new FunctionSymbol("appendText__2", 2, (vm, argCount) =>
            {
                RuntimeValue contentRv = vm.PopStack();
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj || contentRv.ObjectReference is not StringObject contentObj)
                    throw vm.ConstructRuntimeException("File.appendText() expects two string arguments.");

                if (string.IsNullOrEmpty(pathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid path, can not be null or empty.");
                }

                File.AppendAllText(pathObj.Value, contentObj.Value);
                return nilResult;
            }, ["path", "content"], ioNamespace));

            file.StaticIntrinsics.Add("move__2", new FunctionSymbol("move__2", 2, (vm, argCount) =>
            {
                RuntimeValue newPathRv = vm.PopStack();
                RuntimeValue oldPathRv = vm.PopStack();

                if (oldPathRv.ObjectReference is not StringObject oldPathObj || newPathRv.ObjectReference is not StringObject newPathObj)
                    throw vm.ConstructRuntimeException("File.move() expects two string arguments.");

                if (string.IsNullOrEmpty(oldPathObj.Value) || string.IsNullOrEmpty(newPathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid new or old file path(s), can not be null or empty.");
                }

                File.Move(oldPathObj.Value, newPathObj.Value);
                return nilResult;
            }, ["old_path", "new_path"], ioNamespace));

            file.StaticIntrinsics.Add("read__1", new FunctionSymbol("read__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw vm.ConstructRuntimeException("File.read() expects a string argument.");

                if (string.IsNullOrEmpty(pathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid path, can not be null or empty.");
                }

                return vm.ResolveStringObjectRuntimeValue(File.ReadAllText(pathObj.Value));
            }, ["path"], ioNamespace));

            file.StaticIntrinsics.Add("create__1", new FunctionSymbol("create__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw vm.ConstructRuntimeException("File.create() expects a string argument.");

                if (string.IsNullOrEmpty(pathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid path, can not be null or empty.");
                }

                File.Create(pathObj.Value).Close();
                return nilResult;
            }, ["path"], ioNamespace));

            file.StaticIntrinsics.Add("delete__1", new FunctionSymbol("delete__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw vm.ConstructRuntimeException("File.delete() expects a string argument.");

                if (string.IsNullOrEmpty(pathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid path, can not be null or empty.");
                }

                File.Delete(pathObj.Value);
                return nilResult;
            }, ["path"], ioNamespace));

            file.StaticIntrinsics.Add("exists__1", new FunctionSymbol("exists__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw vm.ConstructRuntimeException("File.exists() expects a string argument.");

                return new RuntimeValue(File.Exists(pathObj.Value));
            }, ["path"], ioNamespace));
        }
    }
}