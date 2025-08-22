namespace Fluence
{
    internal sealed class ListObject
    {
        public readonly List<RuntimeValue> Elements = new();

        public override string ToString()
        {
            return $"[{string.Join(", ", Elements)}]";
        }
    }

    /// <summary>
    /// Represents a heap-allocated string object in the Fluence VM.
    /// </summary>
    internal sealed class StringObject
    {
        public readonly string Value;

        public StringObject(string value)
        {
            Value = value;
        }

        public override string ToString() => Value;
        public override int GetHashCode() => Value.GetHashCode();
        public override bool Equals(object obj) => obj is StringObject other && Value.Equals(other.Value);
    }

    /// <summary>
    /// Represents a heap-allocated range object.
    /// </summary>
    internal sealed class RangeObject
    {
        public RuntimeValue Start { get; }
        public RuntimeValue End { get; }

        public RangeObject(RuntimeValue start, RuntimeValue end)
        {
            Start = start;
            End = end;
        }

        public override string ToString() => $"{Start}..{End}";
    }

    /// <summary>
    /// Represents the state of an ongoing iteration over an iterable object ( a range in a for in loop ).
    /// </summary>
    internal sealed class IteratorObject
    {
        public object Iterable { get; }
        public int CurrentIndex { get; set; }

        public IteratorObject(object iterable)
        {
            Iterable = iterable;
            CurrentIndex = 0;
        }
    }

    /// <summary>
    /// An enum that acts as a discriminator to identify the type of data stored in a RuntimeValue.
    /// </summary>
    internal enum RuntimeValueType : byte
    {
        Nil,
        Boolean,
        Number,
        Object // For all heap-allocated types like strings, lists, and struct instances.
    }

    /// <summary>
    /// Represents any value that can exist in the Fluence VM at runtime.
    /// </summary>
    internal readonly record struct RuntimeValue
    {
        public readonly double NumberValue; // For all numbers (int, float, double) and booleans (0 or 1).
        public readonly object ObjectReference; // A reference to a heap object (String, ListObject, InstanceObject, etc.).
        public readonly RuntimeValueType Type;

        public static readonly RuntimeValue Nil = new(RuntimeValueType.Nil);

        private RuntimeValue(RuntimeValueType type)
        {
            ObjectReference = null!;
            NumberValue = 0;
            Type = type;
        }

        public RuntimeValue(bool value)
        {
            ObjectReference = null!;
            NumberValue = value ? 1 : 0;
            Type = RuntimeValueType.Boolean;
        }

        public RuntimeValue(double value)
        {
            ObjectReference = null!;
            NumberValue = value;
            Type = RuntimeValueType.Number;
        }

        public RuntimeValue(int value)
        {
            ObjectReference = null!;
            NumberValue = value;
            Type = RuntimeValueType.Number;
        }

        public RuntimeValue(float value)
        {
            ObjectReference = null!;
            NumberValue = value;
            Type = RuntimeValueType.Number;
        }

        // This constructor is for all heap-allocated types.
        public RuntimeValue(object obj)
        {
            NumberValue = 0;
            ObjectReference = obj;
            Type = RuntimeValueType.Object;
        }

        /// <summary>
        /// Checks the "truthiness" of the value.
        /// Only nil and false are falsy.
        /// </summary>
        public bool IsTruthy => !(Type == RuntimeValueType.Nil || (Type == RuntimeValueType.Boolean && NumberValue == 0));

        /// <summary>
        /// A convenient way to get the value as a boolean.
        /// </summary>
        public bool AsBoolean => Type == RuntimeValueType.Boolean && NumberValue != 0;

        public object AsObject => Type == RuntimeValueType.Object ? ObjectReference : null;

        public override string ToString() => Type switch
        {
            RuntimeValueType.Nil => "nil",
            RuntimeValueType.Boolean => NumberValue != 0 ? "true" : "false",
            RuntimeValueType.Number => NumberValue.ToString(),
            RuntimeValueType.Object => ObjectReference?.ToString() ?? "nil",
            _ => "??? (Undefined Value)"
        };

        public override int GetHashCode()
        {
            return Type switch
            {
                RuntimeValueType.Nil => (int)Type,
                RuntimeValueType.Boolean => NumberValue.GetHashCode() ^ Type.GetHashCode(),
                RuntimeValueType.Number => NumberValue.GetHashCode() ^ Type.GetHashCode(),
                RuntimeValueType.Object => ObjectReference?.GetHashCode() ?? 0 ^ Type.GetHashCode(),
                _ => 0
            };
        }
    }
}