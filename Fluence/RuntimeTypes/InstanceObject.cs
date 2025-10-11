using Fluence.VirtualMachine;

namespace Fluence.RuntimeTypes
{
    /// <summary>
    /// Represents a runtime instance of a user-defined 'struct'. It holds a reference
    /// to its class blueprint (the StructSymbol) and its own set of instance fields.
    /// </summary>
    internal sealed record class InstanceObject
    {
        /// <summary>
        /// The compile-time "class" or blueprint that defines the structure and methods for this instance.
        /// </summary>
        internal StructSymbol Class { get; init; }

        /// <summary>
        /// A dictionary storing the state of this specific instance.
        /// </summary>
        private readonly Dictionary<string, RuntimeValue> _fields = new();

        internal InstanceObject(StructSymbol symb) => Class = symb;

        /// <summary>
        /// Gets the value of a field or method from the instance.
        /// The lookup order is: 1. Instance Fields, 2. Class Methods.
        /// </summary>
        /// <param name="fieldName">The name of the property or method to access.</param>
        /// <returns>The <see cref="RuntimeValue"/> of the field or a <see cref="BoundMethodObject"/> for a method.</returns>
        /// <exception cref="FluenceRuntimeException">Thrown if the property or method is not defined.</exception>
        internal RuntimeValue GetField(string fieldName, FluenceVirtualMachine vm)
        {
            if (_fields.TryGetValue(fieldName, out RuntimeValue value))
            {
                return value;
            }

            if (Class.StaticFields.TryGetValue(fieldName, out RuntimeValue value2))
            {
                return value2;
            }

            if (Class.Functions.TryGetValue(fieldName, out FunctionValue? method))
            {
                BoundMethodObject boundMethod = new BoundMethodObject(this, method);
                return new RuntimeValue(boundMethod);
            }

            throw vm.ConstructRuntimeException($"Undefined property or method '{fieldName}' on struct '{Class.Name}'.");
        }

        /// <summary>
        /// Sets the value of a field on the instance.
        /// </summary>
        /// <param name="fieldName">The name of the field to set.</param>
        /// <param name="value">The new value for the field.</param>
        internal void SetField(string fieldName, RuntimeValue value)
        {
            ArgumentNullException.ThrowIfNull(fieldName);
            _fields[fieldName] = value;
        }

        public override string ToString()
        {
            return $"<instance of {Class.Name}>. Fields: {string.Join(", ", _fields.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}";
        }
    }
}