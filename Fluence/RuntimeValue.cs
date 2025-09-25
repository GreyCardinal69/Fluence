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
    /// Represents the runtime instance of a list, which can contain any <see cref="RuntimeValue"/>.
    /// Implements <see cref="IFluenceObject"/> to provide intrinsic functions.
    /// </summary>
    internal sealed record class ListObject : IFluenceObject
    {
        /// <summary>The elements of the list.</summary>
        internal List<RuntimeValue> Elements { get; } = new();

        /// <summary>Implements the native 'length()' method for lists.</summary>
        private static RuntimeValue Length(FluenceVirtualMachine vm, RuntimeValue self)
        {
            return new RuntimeValue(self.As<ListObject>()?.Elements.Count ?? 0);
        }

        /// <summary>Implements the native 'push(element)' method for lists.</summary>
        private static RuntimeValue Push(FluenceVirtualMachine vm, RuntimeValue self)
        {
            RuntimeValue element = vm.PopStack();
            self.As<ListObject>()?.Elements.Add(element);
            return RuntimeValue.Nil;
        }

        /// <inheritdoc/>
        public bool TryGetIntrinsicMethod(string name, out IntrinsicRuntimeMethod method)
        {
            method = name switch
            {
                "push__1" => Push,
                "length__0" => Length,
                _ => null!
            };
            return method != null;
        }

        public override string ToString() => $"ListObject [{string.Join(", ", Elements)}]";
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