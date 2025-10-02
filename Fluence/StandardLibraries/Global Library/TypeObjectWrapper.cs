using Fluence.RuntimeTypes;
using static Fluence.FluenceVirtualMachine;

namespace Fluence.Global
{
    internal static class TypeObjectLibrary
    {
        private static readonly Dictionary<string, IntrinsicRuntimeMethod> _instanceMethods = new();

        static TypeObjectLibrary()
        {
            _instanceMethods["name__0"] = GetName;
            _instanceMethods["fields__0"] = GetFields;
            _instanceMethods["methods__0"] = GetMethods;
        }

        public static RuntimeValue Create(FluenceVirtualMachine vm, TypeMetadata metadata)
        {
            Wrapper foreignObject = new Wrapper(metadata, _instanceMethods);
            return new RuntimeValue(foreignObject);
        }

        private static RuntimeValue GetName(FluenceVirtualMachine vm, RuntimeValue self)
        {
            TypeMetadata metadata = (TypeMetadata)self.As<Wrapper>().Instance;
            return vm.ResolveStringObjectRuntimeValue(metadata.Name);
        }

        private static RuntimeValue GetFields(FluenceVirtualMachine vm, RuntimeValue self)
        {
            TypeMetadata metadata = (TypeMetadata)self.As<Wrapper>().Instance;
            ListObject list = new ListObject();
            foreach (string field in metadata.Fields)
            {
                list.Elements.Add(vm.ResolveStringObjectRuntimeValue(field));
            }
            return new RuntimeValue(list);
        }

        private static RuntimeValue GetMethods(FluenceVirtualMachine vm, RuntimeValue self)
        {
            TypeMetadata metadata = (TypeMetadata)self.As<Wrapper>().Instance;
            ListObject list = new ListObject();
            foreach (string method in metadata.Methods)
            {
                list.Elements.Add(vm.ResolveStringObjectRuntimeValue(method));
            }
            return new RuntimeValue(list);
        }
    }
}