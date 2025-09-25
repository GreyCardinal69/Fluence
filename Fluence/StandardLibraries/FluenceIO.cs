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
            // --- File Static Struct ---
            //
            StructSymbol file = new StructSymbol("File");
            ioNamespace.Declare("File", file);

            file.StaticIntrinsics.Add("write__2", new FunctionSymbol("write__2", 2, (vm, argCount) =>
            {
                (string path, string content) = IntrinsicHelpers.GetTwoStringArgs(vm, "File.write()");
                File.WriteAllText(path, content);
                return nilResult;
            }, ["path", "content"], ioNamespace));

            file.StaticIntrinsics.Add("append_text__2", new FunctionSymbol("append_text__2", 2, (vm, argCount) =>
            {
                (string path, string content) = IntrinsicHelpers.GetTwoStringArgs(vm, "File.append_text()");
                File.AppendAllText(path, content);
                return nilResult;
            }, ["path", "content"], ioNamespace));

            file.StaticIntrinsics.Add("move__2", new FunctionSymbol("move__2", 2, (vm, argCount) =>
            {
                (string newPath, string oldPath) = IntrinsicHelpers.GetTwoStringArgs(vm, "File.move()");
                File.Move(oldPath, newPath);
                return nilResult;
            }, ["old_path", "new_path"], ioNamespace));

            file.StaticIntrinsics.Add("read__1", new FunctionSymbol("read__1", 1, (vm, argCount) =>
            {
                string path = IntrinsicHelpers.GetStringArg(vm, "File.read()");
                return vm.ResolveStringObjectRuntimeValue(File.ReadAllText(path));
            }, ["path"], ioNamespace));

            file.StaticIntrinsics.Add("create__1", new FunctionSymbol("create__1", 1, (vm, argCount) =>
            {
                string path = IntrinsicHelpers.GetStringArg(vm, "File.create()");
                File.Create(path).Close();
                return nilResult;
            }, ["path"], ioNamespace));

            file.StaticIntrinsics.Add("delete__1", new FunctionSymbol("delete__1", 1, (vm, argCount) =>
            {
                string path = IntrinsicHelpers.GetStringArg(vm, "File.delete()");
                File.Delete(path);
                return nilResult;
            }, ["path"], ioNamespace));

            file.StaticIntrinsics.Add("exists__1", new FunctionSymbol("exists__1", 1, (vm, argCount) =>
            {
                string path = IntrinsicHelpers.GetStringArg(vm, "File.exists()");
                return new RuntimeValue(File.Exists(path));
            }, ["path"], ioNamespace));

            //
            // --- Path Static Struct ---
            //
            StructSymbol pathStruct = new StructSymbol("Path");
            ioNamespace.Declare("Path", pathStruct);

            pathStruct.StaticIntrinsics.Add("dir_sep_char__0", new FunctionSymbol("dir_sep_char__0", 0, (vm, argCount) =>
            {
                return vm.ResolveCharObjectRuntimeValue(Path.DirectorySeparatorChar);
            }, [], ioNamespace));

            pathStruct.StaticIntrinsics.Add("has_extension__1", new FunctionSymbol("has_extension__1", 1, (vm, argCount) =>
            {
                string path = IntrinsicHelpers.GetStringArg(vm, "Path.has_extension()");
                return new RuntimeValue(Path.HasExtension(path));
            }, ["path"], ioNamespace));

            pathStruct.StaticIntrinsics.Add("change_extension__2", new FunctionSymbol("change_extension__2", 2, (vm, argCount) =>
            {
                (string newExtension, string path) = IntrinsicHelpers.GetTwoStringArgs(vm, "Path.change_extension()");
                return vm.ResolveStringObjectRuntimeValue(Path.ChangeExtension(path, newExtension));
            }, ["path", "new_extension"], ioNamespace));

            pathStruct.StaticIntrinsics.Add("exists__1", new FunctionSymbol("exists__1", 1, (vm, argCount) =>
            {
                string path = IntrinsicHelpers.GetStringArg(vm, "Path.exists()");
                return new RuntimeValue(Path.Exists(path));
            }, ["path"], ioNamespace));

            pathStruct.StaticIntrinsics.Add("get_dir_name__1", new FunctionSymbol("get_dir_name__1", 1, (vm, argCount) =>
            {
                string path = IntrinsicHelpers.GetStringArg(vm, "Path.get_dir_name()");
                string dirName = Path.GetDirectoryName(path);
                return dirName is null ? RuntimeValue.Nil : vm.ResolveStringObjectRuntimeValue(dirName);
            }, ["path"], ioNamespace));

            pathStruct.StaticIntrinsics.Add("get_file_name__1", new FunctionSymbol("get_file_name__1", 1, (vm, argCount) =>
            {
                string path = IntrinsicHelpers.GetStringArg(vm, "Path.get_file_name()");
                return vm.ResolveStringObjectRuntimeValue(Path.GetFileName(path));
            }, ["path"], ioNamespace));

            pathStruct.StaticIntrinsics.Add("get_file_name_raw__1", new FunctionSymbol("get_file_name_raw__1", 1, (vm, argCount) =>
            {
                string path = IntrinsicHelpers.GetStringArg(vm, "Path.get_file_name_raw()");
                return vm.ResolveStringObjectRuntimeValue(Path.GetFileNameWithoutExtension(path));
            }, ["path"], ioNamespace));

            pathStruct.StaticIntrinsics.Add("get_full_path__1", new FunctionSymbol("get_full_path__1", 1, (vm, argCount) =>
            {
                string path = IntrinsicHelpers.GetStringArg(vm, "Path.get_full_path()");
                string fullPath = Path.GetFullPath(path);
                return fullPath is null ? RuntimeValue.Nil : vm.ResolveStringObjectRuntimeValue(fullPath);
            }, ["path"], ioNamespace));

            pathStruct.StaticIntrinsics.Add("get_temp_path__0", new FunctionSymbol("get_temp_path__0", 0, (vm, argCount) =>
            {
                return vm.ResolveStringObjectRuntimeValue(Path.GetTempPath());
            }, [], ioNamespace));

            //
            // --- Directory Static Struct ---
            //
            StructSymbol dir = new StructSymbol("Dir");
            ioNamespace.Declare("Dir", dir);

            dir.StaticIntrinsics.Add("create__1", new FunctionSymbol("create__1", 1, (vm, argCount) =>
            {
                string path = IntrinsicHelpers.GetStringArg(vm, "Dir.create()");
                Directory.CreateDirectory(path);
                return nilResult;
            }, ["path"], ioNamespace));

            dir.StaticIntrinsics.Add("delete__1", new FunctionSymbol("delete__1", 1, (vm, argCount) =>
            {
                string path = IntrinsicHelpers.GetStringArg(vm, "Dir.delete()");
                Directory.Delete(path);
                return nilResult;
            }, ["path"], ioNamespace));

            dir.StaticIntrinsics.Add("exists__1", new FunctionSymbol("exists__1", 1, (vm, argCount) =>
            {
                string path = IntrinsicHelpers.GetStringArg(vm, "Dir.exists()");
                return new RuntimeValue(Directory.Exists(path));
            }, ["path"], ioNamespace));

            dir.StaticIntrinsics.Add("get_dirs__1", new FunctionSymbol("get_dirs__1", 1, (vm, argCount) =>
            {
                string path = IntrinsicHelpers.GetStringArg(vm, "Dir.get_dirs()");
                ListObject list = new ListObject();
                foreach (string item in Directory.GetDirectories(path))
                {
                    list.Elements.Add(vm.ResolveStringObjectRuntimeValue(item));
                }
                return new RuntimeValue(list);
            }, ["path"], ioNamespace));

            dir.StaticIntrinsics.Add("get_files__1", new FunctionSymbol("get_files__1", 1, (vm, argCount) =>
            {
                string path = IntrinsicHelpers.GetStringArg(vm, "Dir.get_files()");
                ListObject list = new ListObject();
                foreach (string item in Directory.GetFiles(path))
                {
                    list.Elements.Add(vm.ResolveStringObjectRuntimeValue(item));
                }
                return new RuntimeValue(list);
            }, ["path"], ioNamespace));

            dir.StaticIntrinsics.Add("move__2", new FunctionSymbol("move__2", 2, (vm, argCount) =>
            {
                (string newPath, string path) = IntrinsicHelpers.GetTwoStringArgs(vm, "Dir.move()");
                Directory.Move(path, newPath);
                return nilResult;
            }, ["path", "new_path"], ioNamespace));

            dir.StaticIntrinsics.Add("get_current__0", new FunctionSymbol("get_current__0", 0, (vm, argCount) =>
            {
                return vm.ResolveStringObjectRuntimeValue(Directory.GetCurrentDirectory());
            }, [], ioNamespace));
        }
    }
}