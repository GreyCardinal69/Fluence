using Fluence.RuntimeTypes;
using static Fluence.FluenceVirtualMachine;

namespace Fluence
{
    /// <summary>
    /// Represents a "closure" that binds an instance of an object (the receiver).
    /// </summary>
    internal sealed record class BoundMethodObject(InstanceObject Receiver, FunctionValue Method)
    {
        public override string ToString() => $"<bound method {Method.Name} of {Receiver}>";
    }

    /// <summary>
    /// Represents a heap-allocated char object in the Fluence VM.
    /// </summary>
    internal sealed record class CharObject : IFluenceObject
    {
        internal char Value { get; private set; }

        internal CharObject(char value) => Value = value;

        public CharObject() { }

        internal void Initialize(char value) => Value = value;

        internal void Reset() => Value = default;

        /// <inheritdoc/>
        public bool TryGetIntrinsicMethod(string name, out IntrinsicRuntimeMethod method)
        {
            method = name switch
            {
                _ => null!
            };
            return method != null;
        }

        public override string ToString() => Value.ToString();
    }

}