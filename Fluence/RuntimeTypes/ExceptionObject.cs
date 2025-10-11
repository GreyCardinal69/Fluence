using Fluence.RuntimeTypes;
using Fluence.VirtualMachine;
using static Fluence.VirtualMachine.FluenceVirtualMachine;

namespace Fluence
{
    /// <summary>
    /// Represents a non-fatal exception that has occured and has been caught in the Fluence VM.
    /// </summary>
    internal sealed record class ExceptionObject : IFluenceObject
    {
        internal string Value { get; private set; }

        internal ExceptionObject(string value) => Value = value;

        private RuntimeValue ToString(FluenceVirtualMachine vm, RuntimeValue self) => vm.ResolveStringObjectRuntimeValue(Value);

        /// <inheritdoc/>
        public bool TryGetIntrinsicMethod(string name, out IntrinsicRuntimeMethod method)
        {
            method = name switch
            {
                "to_string__0" => ToString,
                _ => null!
            };
            return method != null;
        }

        public override string ToString() => $"Exception";
    }
}