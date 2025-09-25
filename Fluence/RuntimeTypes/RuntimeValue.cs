using System.Globalization;
using System.Runtime.InteropServices;

namespace Fluence.RuntimeTypes
{

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
        internal readonly object? ObjectReference;
        [FieldOffset(16)]
        internal readonly RuntimeValueType Type;
        [FieldOffset(17)]
        internal readonly RuntimeNumberType NumberType;

        internal static readonly RuntimeValue Nil = new RuntimeValue(RuntimeValueType.Nil);
        internal static readonly RuntimeValue True = new RuntimeValue(true);
        internal static readonly RuntimeValue False = new RuntimeValue(false);

        private RuntimeValue(RuntimeValueType type)
        {
            this = default;
            Type = type;
        }

        internal RuntimeValue(bool value) : this(RuntimeValueType.Boolean)
        {
            IntValue = value ? 1 : 0;
        }

        internal RuntimeValue(double value) : this(RuntimeValueType.Number)
        {
            NumberType = RuntimeNumberType.Double;
            DoubleValue = value;
        }

        internal RuntimeValue(int value) : this(RuntimeValueType.Number)
        {
            NumberType = RuntimeNumberType.Int;
            IntValue = value;
        }

        internal RuntimeValue(float value) : this(RuntimeValueType.Number)
        {
            NumberType = RuntimeNumberType.Float;
            FloatValue = value;
        }

        internal RuntimeValue(long value) : this(RuntimeValueType.Number)
        {
            NumberType = RuntimeNumberType.Long;
            LongValue = value;
        }

        internal RuntimeValue(object? obj) : this(RuntimeValueType.Object)
        {
            ObjectReference = obj;
        }

        internal bool Is<T>() where T : class => ObjectReference is T;

        internal bool IsNot<T>() where T : class => ObjectReference is not T;

        internal bool Is<T>(out T? value) where T : class
        {
            value = ObjectReference as T;
            return value != null;
        }

        internal bool IsNot<T>(out T? value) where T : class
        {
            if (ObjectReference is T)
            {
                value = null;
                return false;
            }
            value = ObjectReference as T;
            return true;
        }

        /// <summary>
        /// Safely casts the internal <see cref="ObjectReference"/> to the specified type.
        /// </summary>
        internal T As<T>() where T : class => ObjectReference as T;

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
                RuntimeValueType.Boolean => IntValue != 0 ? "true" : "false",
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