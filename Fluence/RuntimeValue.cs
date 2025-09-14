using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using static Fluence.FluenceVirtualMachine;

namespace Fluence
{
    //
    //      ==!!==
    //      Built-in types, if they have intrinsics, must return RuntimeValue and not Value
    //      unlike normal namespace or global intrinsic functions.


    /// <summary>
    /// Represents a "closure" that binds an instance of an object (the receiver).
    /// </summary>
    internal sealed record class BoundMethodObject
    {
        /// <summary>
        /// The instance of the object that the method belongs to. This will be passed as 'self'.
        /// </summary>
        internal InstanceObject Receiver { get; }

        /// <summary>
        /// The compile-time blueprint of the function to be called.
        /// </summary>
        internal FunctionValue Method { get; }

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
    /// to execute it, including its bytecode address and lexical scope.
    /// </summary>
    internal sealed record class FunctionObject
    {
        /// <summary>The name of the function.</summary>
        internal string Name { get; private set; }

        /// <summary>The number of parameters the function expects.</summary>
        internal int Arity { get; private set; }

        /// <summary>The instruction pointer address where the function's bytecode begins.</summary>
        internal int StartAddress { get; private set; }

        /// <summary>The names of the function's parameters.</summary>
        internal List<string> Parameters { get; private set; }

        /// <summary>The lexical scope in which the function was defined, used for resolving non-local variables.</summary>
        internal FluenceScope DefiningScope { get; private set; }

        /// <summary>Indicates whether this function is implemented in C# or Fluence bytecode.</summary>
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

        public FunctionObject()
        {
        }

        internal void Initialize(string name, int arity, List<string> parameters, int startAddress, FluenceScope definingScope)
        {
            Name = name;
            Arity = arity;
            Parameters = parameters;
            StartAddress = startAddress;
            DefiningScope = definingScope;
            IsIntrinsic = false;
        }

        internal void Initialize(string name, int arity, IntrinsicMethod body, FluenceScope definingScope)
        {
            IntrinsicBody = body;
            Name = name;
            Arity = arity;
            DefiningScope = definingScope;
            IsIntrinsic = true;
        }

        internal void Reset()
        {
            Name = null!;
            Arity = 0;
            Parameters = null!;
            StartAddress = 0;
            DefiningScope = null!;
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

        public override string ToString() => $"<function {Name}/{Arity}>";
    }

    /// <summary>
    /// Defines an interface for native C# objects that can expose their own methods
    /// directly to the Fluence VM, bypassing the standard bytecode call mechanism for performance.
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
        internal StructSymbol Class { get; }

        /// <summary>
        /// A dictionary storing the state of this specific instance. Each instance gets its own fields.
        /// </summary>
        internal readonly Dictionary<string, RuntimeValue> _fields = new();

        internal InstanceObject(StructSymbol @class)
        {
            Class = @class;
        }

        /// <summary>
        /// Gets the value of a field or method from the instance.
        /// The lookup order is: 1. Instance Fields, 2. Class Methods.
        /// </summary>
        /// <param name="fieldName">The name of the property or method to access.</param>
        /// <returns>The <see cref="RuntimeValue"/> of the field or a <see cref="BoundMethodObject"/> for a method.</returns>
        /// <exception cref="FluenceRuntimeException">Thrown if the property or method is not defined.</exception>
        internal RuntimeValue GetField(string fieldName)
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

            throw new FluenceRuntimeException($"Undefined property or method '{fieldName}' on struct '{Class.Name}'.");
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
    /// Implements <see cref="IFluenceObject"/> to provide fast, native C# methods.
    /// </summary>
    internal sealed record class ListObject : IFluenceObject
    {
        internal readonly List<RuntimeValue> Elements = new();

        public override string ToString()
        {
            return $"ListObject [{string.Join(", ", Elements)}]";
        }

        /// <summary>Implements the native 'length()' method for lists.</summary>
        /// <remarks>The calling convention for intrinsics is that args[0] is always 'self'.</remarks>
        private static RuntimeValue ListLengthIntrinsic(IReadOnlyList<RuntimeValue> args)
        {
            // For a method call, 'self' is always passed as the first argument.
            ListObject? self = args[0].As<ListObject>();
            return new RuntimeValue(self!.Elements.Count);
        }

        /// <summary>Implements the native 'push(element)' method for lists.</summary>
        private static RuntimeValue ListPushElementIntrinsic(IReadOnlyList<RuntimeValue> args)
        {
            ListObject? self = args[0].As<ListObject>();
            self!.Elements.Add(args[1]);
            return new RuntimeValue(null!);
        }

        /// <inheritdoc/>
        public bool TryGetIntrinsicMethod(string name, out IntrinsicRuntimeMethod method)
        {
            switch (name)
            {
                case "push":
                    method = ListPushElementIntrinsic;
                    return true;
                case "length":
                    method = ListLengthIntrinsic;
                    return true;
                default:
                    method = null!;
                    return false;
            }
        }
    }

    /// <summary>
    /// Represents a heap-allocated char object in the Fluence VM.
    /// </summary>
    internal sealed record class CharObject : IFluenceObject
    {
        internal char Value { get; private set; }

        internal CharObject(char value) { Value = value; }

        public CharObject()
        {
        }

        internal void Initialize(object value)
        {
            Value = (char)value;
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

        /// <summary>Implements the native length property for strings.</summary>
        private static RuntimeValue StringLengthIntrinsic(IReadOnlyList<RuntimeValue> args)
        {
            StringObject self = (StringObject)args[0].ObjectReference;
            return new RuntimeValue(self.Value.Length);
        }

        /// <summary>Implements the native ToUpper function for strings.</summary>
        private static RuntimeValue StringToUpperIntrinsic(IReadOnlyList<RuntimeValue> args)
        {
            StringObject self = (StringObject)args[0].ObjectReference;
            self.Value = self.Value.ToUpperInvariant();
            return new RuntimeValue(self);
        }

        /// <summary>Implements the native IndexOf function for strings.</summary>
        private static RuntimeValue StringIntrinsicFind(IReadOnlyList<RuntimeValue> args)
        {
            StringObject self = (StringObject)args[0].ObjectReference;
            if (args.Count < 2 || args[1].ObjectReference is not CharObject charToFind)
            {
                throw new FluenceRuntimeException($"Runtime Error: string.find() expects a character as an argument.");
            }
            return new RuntimeValue(self.Value.IndexOf(charToFind.Value));
        }

        /// <inheritdoc/>
        public bool TryGetIntrinsicMethod(string name, out IntrinsicRuntimeMethod method)
        {
            switch (name)
            {
                case "length":
                    method = StringLengthIntrinsic;
                    return true;
                case "to_upper":
                    method = StringToUpperIntrinsic;
                    return true;
                case "find":
                    method = StringIntrinsicFind;
                    return true;
                default:
                    method = null!;
                    return false;
            }
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// Represents a heap-allocated range object, typically used in for-in loops.
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
    /// Represents the state of an ongoing iteration over an iterable object (like a list or range).
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