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

            file.StaticIntrinsics.Add("append_text__2", new FunctionSymbol("append_text__2", 2, (vm, argCount) =>
            {
                RuntimeValue contentRv = vm.PopStack();
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj || contentRv.ObjectReference is not StringObject contentObj)
                    throw vm.ConstructRuntimeException("File.append_text() expects two string arguments.");

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

            //
            //      The "Path" static struct.
            //

            StructSymbol path = new StructSymbol("Path");
            ioNamespace.Declare("Path", path);

            path.StaticIntrinsics.Add("dir_sep_char__0", new FunctionSymbol("dir_sep_char__0", 0, (vm, argCount) =>
            {
                return vm.ResolveCharObjectRuntimeValue(Path.DirectorySeparatorChar);
            }, [], ioNamespace));

            path.StaticIntrinsics.Add("has_extension__1", new FunctionSymbol("has_extension__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw vm.ConstructRuntimeException("Path.has_extension() expects a string argument.");

                if (string.IsNullOrEmpty(pathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid path, can not be null or empty.");
                }

                return new RuntimeValue(Path.HasExtension(pathObj.Value));
            }, ["path"], ioNamespace));

            path.StaticIntrinsics.Add("change_extension__2", new FunctionSymbol("change_extension__2", 2, (vm, argCount) =>
            {
                RuntimeValue newExtension = vm.PopStack();
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw vm.ConstructRuntimeException("Path.change_extension() expects a string path argument.");

                if (newExtension.ObjectReference is not StringObject extObj)
                    throw vm.ConstructRuntimeException("Path.change_extension() expects a string argument for the file extension.");

                if (string.IsNullOrEmpty(pathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid path, can not be null or empty.");
                }

                if (string.IsNullOrEmpty(extObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid extension, can not be null or empty.");
                }

                return vm.ResolveStringObjectRuntimeValue(Path.ChangeExtension(pathObj.Value, extObj.Value));
            }, ["path", "new_extension"], ioNamespace));

            path.StaticIntrinsics.Add("exists__1", new FunctionSymbol("exists__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw vm.ConstructRuntimeException("Path.exists() expects a string argument.");

                if (string.IsNullOrEmpty(pathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid path, can not be null or empty.");
                }

                return new RuntimeValue(Path.Exists(pathObj.Value));
            }, ["path"], ioNamespace));

            path.StaticIntrinsics.Add("get_dir_name__1", new FunctionSymbol("get_dir_name__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw vm.ConstructRuntimeException("Path.get_dir_name() expects a string argument.");

                if (string.IsNullOrEmpty(pathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid path, can not be null or empty.");
                }

                return vm.ResolveStringObjectRuntimeValue(Path.GetDirectoryName(pathObj.Value)!);
            }, ["path"], ioNamespace));

            path.StaticIntrinsics.Add("get_file_name__1", new FunctionSymbol("get_file_name__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw vm.ConstructRuntimeException("Path.get_file_name() expects a string argument.");

                if (string.IsNullOrEmpty(pathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid path, can not be null or empty.");
                }

                return vm.ResolveStringObjectRuntimeValue(Path.GetFileName(pathObj.Value));
            }, ["path"], ioNamespace));

            path.StaticIntrinsics.Add("get_file_name_raw__1", new FunctionSymbol("get_file_name_raw__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw vm.ConstructRuntimeException("Path.get_file_name_raw() expects a string argument.");

                if (string.IsNullOrEmpty(pathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid path, can not be null or empty.");
                }

                return vm.ResolveStringObjectRuntimeValue(Path.GetFileNameWithoutExtension(pathObj.Value));
            }, ["path"], ioNamespace));

            path.StaticIntrinsics.Add("get_full_path__1", new FunctionSymbol("get_full_path__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw vm.ConstructRuntimeException("Path.get_full_path() expects a string argument.");

                if (string.IsNullOrEmpty(pathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid path, can not be null or empty.");
                }

                return vm.ResolveStringObjectRuntimeValue(Path.GetFullPath(pathObj.Value!));
            }, ["path"], ioNamespace));

            path.StaticIntrinsics.Add("get_temp_path__0", new FunctionSymbol("get_temp_path__0", 0, (vm, argCount) =>
            {
                return vm.ResolveStringObjectRuntimeValue(Path.GetTempPath());
            }, [], ioNamespace));

            //
            //      The "Directory" static struct.
            //

            StructSymbol dir = new StructSymbol("Dir");
            ioNamespace.Declare("Dir", dir);

            dir.StaticIntrinsics.Add("create__1", new FunctionSymbol("create__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw vm.ConstructRuntimeException("Dir.create() expects a string argument.");

                if (string.IsNullOrEmpty(pathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid path, can not be null or empty.");
                }

                Directory.CreateDirectory(pathObj.Value);
                return RuntimeValue.Nil;
            }, ["path"], ioNamespace));

            dir.StaticIntrinsics.Add("delete__1", new FunctionSymbol("delete__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw vm.ConstructRuntimeException("Dir.delete() expects a string argument.");

                if (string.IsNullOrEmpty(pathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid path, can not be null or empty.");
                }

                Directory.Delete(pathObj.Value);
                return RuntimeValue.Nil;
            }, ["path"], ioNamespace));

            dir.StaticIntrinsics.Add("exists__1", new FunctionSymbol("exists__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw vm.ConstructRuntimeException("Dir.exists() expects a string argument.");

                if (string.IsNullOrEmpty(pathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid path, can not be null or empty.");
                }

                return new RuntimeValue(Directory.Exists(pathObj.Value));
            }, ["path"], ioNamespace));

            dir.StaticIntrinsics.Add("get_dirs__1", new FunctionSymbol("get_dirs__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw vm.ConstructRuntimeException("Dir.get_dirs() expects a string argument.");

                if (string.IsNullOrEmpty(pathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid path, can not be null or empty.");
                }

                ListObject list = new ListObject();

                foreach (var item in Directory.GetDirectories(pathObj.Value))
                {
                    list.Elements.Add(vm.ResolveStringObjectRuntimeValue(item));
                }
                return new RuntimeValue(list);
            }, ["path"], ioNamespace));

            dir.StaticIntrinsics.Add("get_files__1", new FunctionSymbol("get_files__1", 1, (vm, argCount) =>
            {
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw vm.ConstructRuntimeException("Dir.get_files() expects a string argument.");

                if (string.IsNullOrEmpty(pathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid path, can not be null or empty.");
                }

                ListObject list = new ListObject();

                foreach (var item in Directory.GetFiles(pathObj.Value))
                {
                    list.Elements.Add(vm.ResolveStringObjectRuntimeValue(item));
                }
                return new RuntimeValue(list);
            }, ["path"], ioNamespace));

            dir.StaticIntrinsics.Add("move__2", new FunctionSymbol("move__2", 2, (vm, argCount) =>
            {
                RuntimeValue newExtension = vm.PopStack();
                RuntimeValue pathRv = vm.PopStack();

                if (pathRv.ObjectReference is not StringObject pathObj)
                    throw vm.ConstructRuntimeException("Path.move() expects a string path argument.");

                if (newExtension.ObjectReference is not StringObject extObj)
                    throw vm.ConstructRuntimeException("Path.move() expects a string argument for the move destination extension.");

                if (string.IsNullOrEmpty(pathObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid path, can not be null or empty.");
                }

                if (string.IsNullOrEmpty(extObj.Value))
                {
                    throw vm.ConstructRuntimeException("Invalid move path, can not be null or empty.");
                }

                Directory.Move(pathObj.Value, extObj.Value);
                return RuntimeValue.Nil;
            }, ["path", "new_path"], ioNamespace));

            dir.StaticIntrinsics.Add("get_current__0", new FunctionSymbol("get_current__0", 0, (vm, argCount) =>
            {
                return vm.ResolveStringObjectRuntimeValue(Directory.GetCurrentDirectory());
            }, [], ioNamespace));
        }
    }
}