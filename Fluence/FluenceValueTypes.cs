using System.Collections.ObjectModel;

namespace Fluence
{
    /// <summary>
    /// The abstract base type for all values that can be represented in bytecode.
    /// These objects are used by the parser to build an abstract representation of the code.
    /// All value types are immutable records.
    /// </summary>
    internal abstract record class Value
    {
        internal virtual object GetValue()
        {
            return null!;
        }

        /// <summary>
        /// Provides a user-facing string representation of the value, as it would appear
        /// when printed within the Fluence language itself.
        /// </summary>
        internal abstract string ToFluenceString();
    }

    /// <summary>Represents a single character literal.</summary>
    internal sealed record class CharValue(char Value) : Value
    {
        internal override object GetValue() => Value;
        internal override string ToFluenceString() => Value.ToString();

        public override string ToString()
        {
            return $"CharValue: {Value}";
        }
    }

    /// <summary>Represents a string literal.</summary>
    internal sealed record class StringValue(string Value) : Value
    {
        internal override object GetValue() => Value;
        internal override string ToFluenceString() => Value;

        public override string ToString()
        {
            // First 15 chars are enough.
            string end = Value.Length > 15 ? "...\"" : "\"";
            return $"StringValue: \"{Value[..Math.Min(15, Value.Length)]}{end}";
        }
    }

    /// <summary>Represents a boolean literal (true or false).</summary>
    internal sealed record class BooleanValue(bool Value) : Value
    {
        internal override object GetValue() => Value;
        internal override string ToFluenceString() => Value ? "true" : "false";

        public override string ToString()
        {
            return $"BooleanValue: {Value}";
        }
    }

    /// <summary>Represents the nil value.</summary>
    internal sealed record class NilValue : Value
    {
        internal static readonly NilValue NilInstance = new NilValue();

        internal override object GetValue() => null!;
        internal override string ToFluenceString() => "nil";

        public override string ToString()
        {
            return $"NilValue";
        }
    }

    /// <summary>Represents a range literal, e.g., `1..10`.</summary>
    internal sealed record class RangeValue(Value Start, Value End) : Value
    {
        internal override string ToFluenceString() => $"{Start.ToFluenceString()}..{End.ToFluenceString()}";

        public override string ToString()
        {
            return $"RangeValue: From {Start.ToFluenceString()} To {End}";
        }
    }

    /// <summary>Represents a numerical literal, which can be an Integer, Float, Long, or Double.</summary>
    internal sealed record class NumberValue : Value
    {
        internal enum NumberType
        {
            Integer,
            Float,
            Double,
            Long,
        }

        internal static readonly NumberValue One = new NumberValue(1);
        internal static readonly NumberValue Zero = new NumberValue(0);

        internal object Value { get; private set; }
        internal NumberType Type { get; private set; }

        internal NumberValue(object literal)
        {
            Value = literal;

            AssignNumberType(literal);
        }

        internal NumberValue(object literal, NumberType type)
        {
            Value = literal;
            Type = type;
        }

        internal override object GetValue() => Value;
        internal override string ToFluenceString() => Value.ToString()!;

        private void AssignNumberType(object literal)
        {
            if (literal is int)
            {
                Type = NumberType.Integer;
                return;
            }

            if (literal is float) Type = NumberType.Float;
            if (literal is double) Type = NumberType.Double;
            if (literal is long) Type = NumberType.Long;
        }

        internal void ReAssign(object newValue)
        {
            Value = newValue;
            AssignNumberType(newValue);
        }

        internal static NumberValue FromToken(Token token)
        {
            string lexeme = token.Text;
            if (lexeme.EndsWith('f') && float.TryParse(lexeme[..^1], out float floatVal))
            {
                return new NumberValue(floatVal, NumberType.Float);
            }
            if (lexeme.Contains('.', StringComparison.OrdinalIgnoreCase) && double.TryParse(lexeme, out double doubleVal))
            {
                return new NumberValue(doubleVal, NumberType.Double);
            }
            if (int.TryParse(lexeme, out int intVal))
            {
                return new NumberValue(intVal, NumberType.Integer);
            }
            if (long.TryParse(lexeme, out long longVal))
            {
                return new NumberValue(longVal, NumberType.Long);
            }
            if (double.TryParse(lexeme, out double fallbackVal))
            {
                return new NumberValue(fallbackVal, NumberType.Double);
            }
            throw new FormatException($"Invalid number format: '{lexeme}'");
        }

        public override string ToString()
        {
            return $"NumberValue ({Type}): {Value}";
        }
    }

    /// <summary>A special value indicating a statement completed but produced no result, or the result should be ignored.</summary>
    internal sealed record class StatementCompleteValue : Value
    {
        internal static readonly StatementCompleteValue StatementCompleted = new StatementCompleteValue();

        internal override string ToFluenceString() => "<internal: statement_complete>";

        public override string ToString()
        {
            return $"StatementCompleteValue";
        }
    }

    /// <summary>
    /// A descriptor representing an access to a struct's static function, or a static and solid field.
    /// </summary>
    internal sealed record class StaticStructAccess : Value
    {
        internal readonly StructSymbol Struct;

        /// <summary>
        /// The name of the static solid field, or the static function.
        /// </summary>
        internal readonly string Name;

        internal StaticStructAccess(StructSymbol structType, string name)
        {
            Struct = structType;
            Name = name;
        }

        internal override string ToFluenceString()
        {
            return $"<internal: static_struct> <{Struct}__{Name}>";
        }

        public override string ToString()
        {
            return $"StaticStructAccessValue: <{Struct}__{Name}>";
        }
    }

    /// <summary>
    /// A descriptor representing an element access operation.
    /// The parser resolves this into GetElement or SetElement bytecode.
    /// </summary>
    internal sealed record class ElementAccessValue : Value
    {
        internal Value Target { get; init; }
        internal Value Index { get; init; }

        internal ElementAccessValue(Value target, Value index)
        {
            Target = target;
            Index = index;
        }

        internal override string ToFluenceString() => "<internal: element_access>";

        public override string ToString()
        {
            return $"ElementAccessValue";
        }
    }

    /// <summary>A descriptor for a broadcast call template, used in chain assignments.</summary>
    internal sealed record class BroadcastCallTemplate : Value
    {
        /// <summary>
        /// The function to be called.
        /// </summary>
        internal Value Callable { get; init; }

        internal List<Value> Arguments { get; init; }

        /// <summary>
        /// The underscore used in pipes.
        /// </summary>
        internal int PlaceholderIndex { get; init; }

        public BroadcastCallTemplate(Value callable, List<Value> args, int placeholderIndex)
        {
            Callable = callable;
            Arguments = args;
            PlaceholderIndex = placeholderIndex;
        }

        internal override string ToFluenceString() => "<internal: broadcast_template>";

        public override string ToString()
        {
            return "BroadcastTemplateValue";
        }
    }

    /// <summary>
    /// A descriptor representing a temporary variable generated by the parser.
    /// This should be resolved by the VM and never seen by the user.
    /// </summary>
    internal sealed record class TempValue : Value
    {
        internal readonly string TempName;

        internal TempValue(int num) => TempName = $"__Temp{num}";
        internal TempValue(int num, string name) => TempName = $"__{name}{num}";

        internal override string ToFluenceString() => "<internal: temp>";

        public override string ToString()
        {
            return $"TempValue: {TempName}";
        }
    }

    /// <summary>
    /// Represents a simple list.
    /// </summary>
    internal sealed record class ListValue : Value
    {
        internal readonly List<Value> Elements;

        internal ListValue()
        {
            Elements = new List<Value>();
        }

        internal ListValue(List<Value> elements)
        {
            Elements = elements;
        }

        /// <summary>
        /// Provides a user-friendly string representation of the list.
        /// </summary>
        /// <returns>A string in the format "[element1, element2, ...]".</returns>
        public override string ToString()
        {
            // Limit the number of elements shown for very large lists to avoid flooding the console.
            const int maxElementsToShow = 20;
            IEnumerable<Value> elementsToShow = Elements.Take(maxElementsToShow);
            string formattedElements = string.Join(", ", elementsToShow);

            if (Elements.Count > maxElementsToShow)
            {
                formattedElements += $", ... ({Elements.Count - maxElementsToShow} more)";
            }

            return $"[{formattedElements}]";
        }

        internal override string ToFluenceString()
        {
            return $"{string.Join(",", Elements)}";
        }
    }

    /// <summary>Represents a function's compile-time blueprint, including its bytecode address or native implementation.</summary>
    internal sealed record class FunctionValue : Value
    {
        /// <summary>
        /// The name of the function (for debugging/stack traces).
        /// </summary>
        internal string Name { get; init; }

        /// <summary>
        /// The number of parameters the function expects.
        /// </summary>
        internal int Arity { get; init; }

        /// <summary>
        /// The address of the first instruction of the function's body in the bytecode.
        /// </summary>
        internal int StartAddress { get; private set; }

        /// <summary>
        /// The arguments of the function by name.
        /// </summary>
        internal List<string> Arguments { get; init; }

        /// <summary>
        /// The C# intrinsic body of the function, if it is intrinsic.
        /// </summary>
        internal readonly IntrinsicMethod IntrinsicBody;

        /// <summary>
        /// Is the function intrinsic?
        /// </summary>
        internal readonly bool IsIntrinsic;

        /// <summary>
        /// The scope (namespace) the function belongs to.
        /// </summary>
        internal FluenceScope FunctionScope { get; private set; }

        internal FunctionValue()
        {
        }

        internal FunctionValue(string name, int arity, int startAddress, List<string> arguments = null!)
        {
            Name = name;
            Arity = arity;
            StartAddress = startAddress;
            Arguments = arguments;
        }

        public FunctionValue(string name, int arity, IntrinsicMethod body)
        {
            Name = name;
            Arity = arity;
            StartAddress = -1;
            IsIntrinsic = true;
            IntrinsicBody = body;
        }

        public FunctionValue(string name, int arity, IntrinsicMethod body, List<string> args)
        {
            Name = name;
            Arity = arity;
            StartAddress = -1;
            IsIntrinsic = true;
            IntrinsicBody = body;
            Arguments = args;
        }

        internal void SetScope(FluenceScope scope)
        {
            FunctionScope = scope;
        }

        internal void SetStartAddress(int adr)
        {
            StartAddress = adr;
        }

        internal override string ToFluenceString() => $"<function {Name}/{Arity}>";

        public override string ToString()
        {
            string args = (Arguments == null || Arguments.Count == 0) ? "None" : string.Join(",", Arguments);
            return $"FunctionValue: {Name} {FluenceDebug.FormatByteCodeAddress(StartAddress)}, #{Arity} args: {args}.";
        }
    }

    /// <summary>
    /// A descriptor representing a property access operation.
    /// The parser resolves this into GetField or SetField bytecode.
    /// </summary>
    internal sealed record class PropertyAccessValue : Value
    {
        internal readonly Value Target;
        internal readonly string FieldName;

        internal PropertyAccessValue(Value target, string fieldName)
        {
            Target = target;
            FieldName = fieldName;
        }

        internal override string ToFluenceString() => "<internal: property_access>";

        public override string ToString()
        {
            return $"FieldAccess<{Target}:{FieldName}>";
        }
    }

    /// <summary>
    /// Represents a variable by its name. The VM resolves this to a value in the current scope.
    /// </summary>
    internal sealed record class VariableValue : Value
    {
        internal readonly string Name;
        internal bool IsReadOnly;

        internal VariableValue(string identifierValue, bool readOnly = false)
        {
            IsReadOnly = readOnly;
            Name = identifierValue;
        }

        internal override string ToFluenceString() => $"<internal: variable_{(IsReadOnly ? "solid" : "fluid")}>";

        public override string ToString()
        {
            return $"VariableValue: {Name}:{(IsReadOnly ? "solid" : "fluid")}";
        }
    }

    /// <summary>Represents a specific member of an enum, holding both its name and integer value.</summary>
    internal sealed record class EnumValue : Value
    {
        internal string EnumTypeName { get; init; }
        internal readonly string MemberName;
        internal readonly int Value;

        internal override object GetValue() => Value;
        internal override string ToFluenceString() => $"{EnumTypeName}.{MemberName}";

        internal EnumValue(string enumTypeName, string memberName, int value)
        {
            EnumTypeName = enumTypeName;
            MemberName = memberName;
            Value = value;
        }

        public override string ToString()
        {
            return $"EnumValue: {EnumTypeName}.{MemberName}";
        }
    }

    /// <summary>
    /// Represents a struct instance.
    /// </summary>
    internal sealed record class StructValue : Value
    {
        internal readonly StructSymbol Struct;
        internal readonly Dictionary<string, RuntimeValue> Fields;

        internal StructValue(StructSymbol structSym, Dictionary<string, RuntimeValue> fields)
        {
            Struct = structSym;
            Fields = fields;
        }

        public override string ToString()
        {
            return $"{Struct} + {Fields}";
        }

        internal override string ToFluenceString()
        {
            return $"<internal: struct_value>";
        }
    }
}