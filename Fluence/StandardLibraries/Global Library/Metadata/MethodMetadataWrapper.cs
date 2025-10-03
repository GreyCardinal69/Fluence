using Fluence.RuntimeTypes;
using static Fluence.FluenceVirtualMachine;

namespace Fluence.Global
{
    internal static class MethodMetadataWrapper
    {
        private static readonly Dictionary<string, IntrinsicRuntimeMethod> _instanceMethods = new();

        static MethodMetadataWrapper()
        {
            _instanceMethods["name__0"] = GetName;
            _instanceMethods["mangled_name__0"] = GetMangledName;
            _instanceMethods["arity__0"] = GetArity;
            _instanceMethods["get_parameters__0"] = GetParameters;
            _instanceMethods["is_ctor__0"] = IsConstructor;
            _instanceMethods["signature__0"] = GetSignature;
        }

        internal static RuntimeValue Create(FluenceVirtualMachine vm, MethodMetadata metadata)
        {
            Wrapper wrapper = new Wrapper(metadata, _instanceMethods);
            return new RuntimeValue(wrapper);
        }

        private static RuntimeValue GetName(FluenceVirtualMachine vm, RuntimeValue self)
        {
            MethodMetadata metadata = (MethodMetadata)self.As<Wrapper>().Instance;
            return vm.ResolveStringObjectRuntimeValue(metadata.BaseName);
        }

        private static RuntimeValue GetMangledName(FluenceVirtualMachine vm, RuntimeValue self)
        {
            MethodMetadata metadata = (MethodMetadata)self.As<Wrapper>().Instance;
            return vm.ResolveStringObjectRuntimeValue(metadata.MangledName);
        }

        private static RuntimeValue GetArity(FluenceVirtualMachine vm, RuntimeValue self)
        {
            MethodMetadata metadata = (MethodMetadata)self.As<Wrapper>().Instance;
            return new RuntimeValue(metadata.Arity);
        }

        private static RuntimeValue IsConstructor(FluenceVirtualMachine vm, RuntimeValue self)
        {
            MethodMetadata metadata = (MethodMetadata)self.As<Wrapper>().Instance;
            return metadata.IsCtor ? RuntimeValue.True : RuntimeValue.False;
        }

        private static RuntimeValue GetSignature(FluenceVirtualMachine vm, RuntimeValue self)
        {
            MethodMetadata metadata = (MethodMetadata)self.As<Wrapper>().Instance;
            return vm.ResolveStringObjectRuntimeValue(metadata.GetSignature());
        }

        private static RuntimeValue GetParameters(FluenceVirtualMachine vm, RuntimeValue self)
        {
            MethodMetadata metadata = (MethodMetadata)self.As<Wrapper>().Instance;

            ListObject list = new ListObject();

            foreach (string parameter in metadata.Parameters)
            {
                list.Elements.Add(ParameterMetadataWrapper.Create(vm, new ParameterMetadata()
                {
                    ByRef = metadata.RefParameters.Contains(parameter),
                    Name = parameter
                    // TO DO - default values.
                }));
            }

            return new RuntimeValue(list);
        }
    }
}