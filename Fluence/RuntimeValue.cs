using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using static Fluence.FluenceVirtualMachine;

namespace Fluence
{
    /// <summary>
    /// Represents a "closure" that binds an instance of an object (the receiver).
    /// </summary>
    internal sealed record class BoundMethodObject
    {
        /// <summary>
        /// The instance of the object that the method belongs to.
        /// </summary>
        internal InstanceObject Receiver { get; init; }

        /// <summary>
        /// The blueprint of the function to be called.
        /// </summary>
        internal FunctionValue Method { get; init; }

        public BoundMethodObject(InstanceObject receiver, FunctionValue method)
        {
            Receiver = receiver;
            Method = method;
        }

        public override string ToString()
        {
            return $"<bound method {Method.Name} of {Receiver}>";
        }
    }

    /// <summary>
    /// Represents the runtime instance of a function, containing all information needed
    /// to execute it.
    /// </summary>
    internal sealed record class FunctionObject
    {
        /// <summary>The name of the function.</summary>
        internal string Name { get; private set; }

        /// <summary>The number of parameters the function expects.</summary>
        internal int Arity { get; private set; }

        /// <summary>The instruction pointer address where the function's bytecode begins.</summary>
        internal int StartAddress { get; private set; }

        /// <summary>
        /// Sets the bytecode start address for this function. Called by the parser during the second pass.
        /// </summary>
        internal int StartAddressInSource { get; private set; }

        /// <summary>The names of the function's parameters.</summary>
        internal List<string> Parameters { get; private set; }

        /// <summary>The lexical scope in which the function was defined, used for resolving non-local variables.</summary>
        internal FluenceScope DefiningScope { get; private set; }

        /// <summary> A direct reference to the immutable, function symbol that defines this function. </summary>
        internal FunctionSymbol BluePrint { get; private set; }

        /// <summary>Indicates whether this function is implemented in C# (intrinsic) or Fluence bytecode.</summary>
        internal bool IsIntrinsic { get; private set; }

        /// <summary>The C# delegate that implements the body of an intrinsic function.</summary>
        internal IntrinsicMethod IntrinsicBody { get; private set; }

        internal FunctionObject(string name, int arity, List<string> parameters, int startAddress, FluenceScope definingScope)
        {
            Name = name;
            Arity = arity;
            Parameters = parameters;
            StartAddress = startAddress;
            DefiningScope = definingScope;
            IsIntrinsic = false;
        }

        // Constructor for C# intrinsic functions.
        internal FunctionObject(string name, int arity, IntrinsicMethod body, FluenceScope definingScope)
        {
            Name = name;
            Arity = arity;
            StartAddress = -1; // No bytecode address.
            Parameters = new List<string>();
            DefiningScope = definingScope;
            IsIntrinsic = true;
            IntrinsicBody = body;
        }

        /// <summary>
        /// Public parameterless constructor required for object pooling.
        /// </summary>
        public FunctionObject() { }

        internal void Initialize(string name, int arity, List<string> parameters, int startAddress, FluenceScope definingScope, FunctionSymbol symb, int lineInSource)
        {
            StartAddressInSource = lineInSource;
            Name = name;
            Arity = arity;
            Parameters = parameters;
            StartAddress = startAddress;
            DefiningScope = definingScope;
            IsIntrinsic = false;
            BluePrint = symb;
        }

        internal void Initialize(string name, int arity, IntrinsicMethod body, FluenceScope definingScope, FunctionSymbol symb)
        {
            StartAddressInSource = symb != null ? symb.StartAddressInSource : 0;
            IntrinsicBody = body;
            Name = name;
            Arity = arity;
            DefiningScope = definingScope;
            IsIntrinsic = true;
            BluePrint = symb;
        }

        internal void SetBluePrint(FunctionSymbol symb) => BluePrint = symb;

        internal void Reset()
        {
            StartAddressInSource = 0;
            BluePrint = null!;
            Name = null!;
            Arity = 0;
            Parameters = null!;
            StartAddress = 0;
            DefiningScope = null!;
            IsIntrinsic = false;
        }

        public override string ToString() => $"<function {Name}/{Arity}>";
    }

    /// <summary>
    /// An interface for built-in object types in Fluence which feature
    /// built-in intrinsic functions.
    /// </summary>
    internal interface IFluenceObject
    {
        /// <summary>
        /// Attempts to retrieve a native C# method implementation by its name.
        /// </summary>
        /// <param name="name">The name of the method being called in the script.</param>
        /// <param name="method">When this method returns, contains the C# delegate for the method if found; otherwise, null.</param>
        /// <returns><c>true</c> if an intrinsic method with the specified name was found; otherwise, <c>false</c>.</returns>
        bool TryGetIntrinsicMethod(string name, out IntrinsicRuntimeMethod method);
    }

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

        internal InstanceObject(StructSymbol symb)
        {
            Class = symb;
        }

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
            _fields[fieldName] = value;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder($"<instance of {Class.Name}>.");
            foreach (KeyValuePair<string, RuntimeValue> item in _fields)
            {
                stringBuilder.Append($"\n\t{item}");
            }
            return stringBuilder.ToString();
        }
    }

    /// <summary>
    /// Represents the runtime instance of a list, which can contain any <see cref="RuntimeValue"/>.
    /// Implements <see cref="IFluenceObject"/> to provide intrinsic functions.
    /// </summary>
    internal sealed record class ListObject : IFluenceObject
    {
        /// <summary>
        /// The elements of the list.
        /// </summary>
        internal List<RuntimeValue> Elements { get; } = new();

        public override string ToString()
        {
            return $"ListObject [{string.Join(", ", Elements)}]";
        }

        /// <summary>Implements the native 'length()' method for lists.</summary>
        private static RuntimeValue ListLength(FluenceVirtualMachine vm, RuntimeValue self)
        {
            return new RuntimeValue(self.As<ListObject>()!.Elements.Count);
        }

        /// <summary>Implements the native 'push(element)' method for lists.</summary>
        private static RuntimeValue ListPush(FluenceVirtualMachine vm, RuntimeValue self)
        {
            RuntimeValue element = vm.PopStack();
            self.As<ListObject>()!.Elements.Add(element);
            return RuntimeValue.Nil;
        }

        /// <inheritdoc/>
        public bool TryGetIntrinsicMethod(string name, out IntrinsicRuntimeMethod method)
        {
            switch (name)
            {
                case "push__1": method = ListPush; return true;
                case "length__0": method = ListLength; return true;
                default: method = null!; return false;
            }
        }
    }

    /// <summary>
    /// Represents a heap-allocated char object in the Fluence VM.
    /// </summary>
    internal sealed record class CharObject : IFluenceObject
    {
        internal char Value { get; private set; }

        internal CharObject(char value)
        {
            Value = value;
        }

        public CharObject()
        {
        }

        internal void Initialize(char value)
        {
            Value = value;
        }

        internal void Reset()
        {
            Value = default;
        }

        /// <inheritdoc/>
        public bool TryGetIntrinsicMethod(string name, out IntrinsicRuntimeMethod method)
        {
            switch (name)
            {
                default:
                    method = null!;
                    return false;
            }
        }

        public override string ToString() => Value.ToString();
    }

    /// <summary>
    /// Represents a heap-allocated string object in the Fluence VM.
    /// </summary>
    internal sealed record class StringObject : IFluenceObject
    {
        internal string Value { get; private set; }

        internal StringObject(string value) => Value = value;

        public StringObject()
        {
        }

        internal void Reset() => Value = null!;

        internal void Initialize(string str) => Value = str;

        /// <summary>Implements the native '.length' property for strings.</summary>
        private static RuntimeValue StringLength(FluenceVirtualMachine vm, RuntimeValue self)
        {
            return new RuntimeValue(self.As<StringObject>()!.Value.Length);
        }

        /// <summary>Implements the native 'ToUpper()' function for strings.</summary>
        private static RuntimeValue StringToUpper(FluenceVirtualMachine vm, RuntimeValue self)
        {
            StringObject? strObj = self.As<StringObject>();
            string upper = strObj!.Value.ToUpperInvariant();
            return vm.ResolveStringObjectRuntimeValue(upper);
        }

        /// <summary>Implements the native 'IndexOf()' function for strings.</summary>
        private static RuntimeValue StringFind(FluenceVirtualMachine vm, RuntimeValue self)
        {
            RuntimeValue charToFind = vm.PopStack();
            if (charToFind.ObjectReference is not CharObject charObj)
            {
                throw vm.ConstructRuntimeException("string.find() expects a character argument.");
            }
            int index = self.As<StringObject>()!.Value.IndexOf(charObj.Value);
            return new RuntimeValue(index);
        }

        /// <inheritdoc/>
        public bool TryGetIntrinsicMethod(string name, out IntrinsicRuntimeMethod method)
        {
            switch (name)
            {
                case "length__0": method = StringLength; return true;
                case "to_upper__0": method = StringToUpper; return true;
                case "find__1": method = StringFind; return true;
                default: method = null!; return false;
            }
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// Represents a heap-allocated range object, typically used in 'for-in' loops.
    /// </summary>
    internal sealed record class RangeObject
    {
        internal RuntimeValue Start { get; private set; }
        internal RuntimeValue End { get; private set; }

        internal RangeObject(RuntimeValue start, RuntimeValue end)
        {
            Start = start;
            End = end;
        }

        public RangeObject()
        {
        }

        internal void Reset()
        {
            Start = default;
            End = default;
        }

        internal void Initialize(RuntimeValue start, RuntimeValue end)
        {
            Start = start;
            End = end;
        }

        public override string ToString() => $"{Start}..{End}";
    }

    /// <summary>
    /// Represents the state of an ongoing iteration over an iterable object.
    /// </summary>
    internal sealed record class IteratorObject
    {
        /// <summary>The object being iterated over.</summary>
        internal object Iterable { get; private set; }

        /// <summary>The current position within the iteration./summary>
        internal int CurrentIndex { get; set; }

        internal IteratorObject(object iterable)
        {
            Iterable = iterable;
            CurrentIndex = 0;
        }

        public IteratorObject()
        {
        }

        internal void Reset()
        {
            Iterable = null!;
            CurrentIndex = 0;
        }

        internal void Initialize(object iterator)
        {
            Iterable = iterator;
            CurrentIndex = 0;
        }
    }

    /// <summary>
    /// A discriminator to identify the fundamental type of data stored in a <see cref="RuntimeValue"/>.
    /// </summary>
    internal enum RuntimeValueType : byte
    {
        Nil,
        Boolean,
        Number,
        Object // For all heap-allocated types like strings, lists, and struct instances.
    }

    /// <summary>
    /// A sub-discriminator used when <see cref="RuntimeValueType"/> is <see cref="RuntimeValueType.Number"/>.
    /// </summary>
    internal enum RuntimeNumberType
    {
        Int,
        Float,
        Double,
        Long,
        Unknown,
    }

    /// <summary>
    /// Represents any value that can exist in the Fluence VM at runtime.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal readonly record struct RuntimeValue
    {
        [FieldOffset(0)]
        internal readonly long LongValue;
        [FieldOffset(0)]
        internal readonly double DoubleValue;
        [FieldOffset(0)]
        internal readonly int IntValue;
        [FieldOffset(0)]
        internal readonly float FloatValue;

        [FieldOffset(8)]
        internal readonly object ObjectReference;
        [FieldOffset(16)]
        internal readonly RuntimeValueType Type;
        [FieldOffset(17)]
        internal readonly RuntimeNumberType NumberType;

        internal static readonly RuntimeValue Nil = new(RuntimeValueType.Nil);

        private RuntimeValue(RuntimeValueType type)
        {
            this = default;
            Type = type;
        }

        internal RuntimeValue(bool value)
        {
            this = default;
            Type = RuntimeValueType.Boolean;
            // Booleans can be stored in the IntValue field.
            IntValue = value ? 1 : 0;
        }

        internal RuntimeValue(double value)
        {
            this = default;
            Type = RuntimeValueType.Number;
            NumberType = RuntimeNumberType.Double;
            DoubleValue = value;
        }

        internal RuntimeValue(int value)
        {
            this = default;
            Type = RuntimeValueType.Number;
            NumberType = RuntimeNumberType.Int;
            IntValue = value;
        }

        internal RuntimeValue(float value)
        {
            this = default;
            Type = RuntimeValueType.Number;
            NumberType = RuntimeNumberType.Float;
            FloatValue = value;
        }

        internal RuntimeValue(long value)
        {
            this = default;
            Type = RuntimeValueType.Number;
            NumberType = RuntimeNumberType.Long;
            LongValue = value;
        }

        internal RuntimeValue(object obj)
        {
            this = default;
            Type = RuntimeValueType.Object;
            ObjectReference = obj;
        }

        internal bool Is<T>() => ObjectReference is T;
        internal bool INot<T>() => ObjectReference is not T;

        internal bool Is<T>(out T? value)
        {
            if (ObjectReference is T t)
            {
                value = t;
                return true;
            }
            value = default;
            return false;
        }

        internal bool IsNot<T>(out T? value) where T : class
        {
            if (ObjectReference is T)
            {
                value = default;
                return false;
            }

            value = ObjectReference as T;
            return true;
        }

        /// <summary>
        /// Safely casts the internal <see cref="ObjectReference"/> to the specified type.
        /// </summary>
        internal T? As<T>() where T : class
        {
            return ObjectReference as T;
        }

        internal bool As<T>(out T? value) where T : class
        {
            value = ObjectReference as T;
            return value != null;
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="RuntimeValue"/> is "truthy".
        /// In Fluence, only 'nil' and 'false' are considered falsy.
        /// </summary>
        internal bool IsTruthy => !(Type == RuntimeValueType.Nil || (Type == RuntimeValueType.Boolean && IntValue == 0));

        /// <inheritdoc/>
        public override string ToString()
        {
            return Type switch
            {
                RuntimeValueType.Nil => "nil",
                RuntimeValueType.Boolean => (IntValue != 0).ToString().ToLower(CultureInfo.InvariantCulture),
                RuntimeValueType.Number => NumberType switch
                {
                    RuntimeNumberType.Int => IntValue.ToString(),
                    RuntimeNumberType.Long => LongValue.ToString(),
                    RuntimeNumberType.Float => FloatValue.ToString(CultureInfo.InvariantCulture),
                    RuntimeNumberType.Double => DoubleValue.ToString(CultureInfo.InvariantCulture),
                    _ => "??? (Invalid NumberType)"
                },
                RuntimeValueType.Object => ObjectReference?.ToString() ?? "nil",
                _ => "??? (Undefined Value)",
            };
        }
    }
}