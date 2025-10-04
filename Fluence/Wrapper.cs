using Fluence.RuntimeTypes;
using static Fluence.FluenceVirtualMachine;

namespace Fluence
{
    /// <summary>
    /// A generic wrapper that allows a native C# object
    /// to be exposed and used within the Fluence runtime.
    /// </summary>
    internal sealed class Wrapper : IFluenceObject
    {
        /// <summary>The actual C# object being wrapped.</summary>
        internal object Instance { get; }

        /// <summary>
        /// A dictionary of "intrinsic methods" that maps a Fluence method name
        /// to a C# delegate that can be called by the VM.
        /// </summary>
        private readonly Dictionary<string, IntrinsicRuntimeMethod> _methods;

        internal Wrapper(object instance, Dictionary<string, IntrinsicRuntimeMethod> methods)
        {
            Instance = instance;
            _methods = methods;
        }

        public bool TryGetIntrinsicMethod(string name, out IntrinsicRuntimeMethod method)
        {
            return _methods.TryGetValue(name, out method!);
        }

        public override string ToString() => Instance.ToString();

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            return ((Wrapper)obj).Instance.Equals(Instance);
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(Instance);
            return hash.ToHashCode();
        }
    }
}