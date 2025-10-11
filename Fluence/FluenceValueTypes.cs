using System.Globalization;

namespace Fluence
{
    /// <summary>
    /// The abstract base type for all values that can be represented in bytecode.
    /// These objects are used by the parser to build an abstract representation of the code.
    /// </summary>
    internal abstract record class Value
    {
        /// <summary>
        /// Provides a user-facing string representation of the value, as it would appear
        /// when printed within the Fluence language itself.
        /// </summary>
        internal abstract string ToFluenceString();
    }

    /// <summary>Represents a single character literal.</summary>
    internal sealed record class CharValue(char Value) : Value
    {
        internal override string ToFluenceString() => Value.ToString();
        public override string ToString() => $"CharValue: {Value}";
    }

    /// <summary>Represents a string literal.</summary>
    internal sealed record class StringValue(string Value) : Value
    {
        internal override string ToFluenceString() => Value;

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Value)) return "StringValue: \"\"";

            // First 15 chars are enough.
            string end = Value.Length > 15 ? "...\"" : "\"";
            return $"StringValue: \"{Value[..Math.Min(15, Value.Length)]}{end}";
        }
    }

    /// <summary>Represents a boolean literal.</summary>
    internal sealed record class BooleanValue : Value
    {
        internal bool Value { get; init; }

        internal static readonly BooleanValue True = new BooleanValue(true);
        internal static readonly BooleanValue False = new BooleanValue(false);

        internal BooleanValue(bool value)
        {
            Value = value;
        }

        internal override string ToFluenceString() => Value ? "true" : "false";
        public override string ToString() => $"BooleanValue: {Value}";
    }

    /// <summary>Represents the nil (null) value.</summary>
    internal sealed record class NilValue : Value
    {
        internal static readonly NilValue NilInstance = new NilValue();

        internal override string ToFluenceString() => "nil";
        public override string ToString() => $"NilValue";
    }

    /// <summary>Represents a range literal.</summary>
    internal sealed record class RangeValue(Value Start, Value End) : Value
    {
        internal override string ToFluenceString() => $"<internal: range__{Start.ToFluenceString()}..{End.ToFluenceString()}>";
        public override string ToString() => $"RangeValue: From {Start.ToFluenceString()} To {End}";
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

        private void AssignNumberType(object literal)
        {
            Type = literal switch
            {
                int => NumberType.Integer,
                float => NumberType.Float,
                double => NumberType.Double,
                long => NumberType.Long,
                _ => NumberType.Double,
            };
        }

        internal void ReAssign(object newValue)
        {
            Value = newValue;
            AssignNumberType(newValue);
        }

        internal static NumberValue FromToken(Token token)
        {
            ReadOnlySpan<char> lexemeSpan = token.Text.AsSpan();

            if (lexemeSpan.EndsWith("f", StringComparison.OrdinalIgnoreCase) && float.TryParse(lexemeSpan[..^1], out float floatVal))
            {
                return new NumberValue(floatVal, NumberType.Float);
            }
            if ((lexemeSpan.Contains('.') || lexemeSpan.Contains('e') || lexemeSpan.Contains('E')) && double.TryParse(lexemeSpan, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleVal))
            {
                return new NumberValue(doubleVal, NumberType.Double);
            }
            if (int.TryParse(lexemeSpan, NumberStyles.Any, CultureInfo.InvariantCulture, out int intVal))
            {
                return new NumberValue(intVal, NumberType.Integer);
            }
            if (long.TryParse(lexemeSpan, NumberStyles.Any, CultureInfo.InvariantCulture, out long longVal))
            {
                return new NumberValue(longVal, NumberType.Long);
            }
            if (double.TryParse(lexemeSpan, NumberStyles.Any, CultureInfo.InvariantCulture, out double fallbackVal))
            {
                return new NumberValue(fallbackVal, NumberType.Double);
            }
            throw new FormatException($"Invalid number format: '{lexemeSpan}'");
        }

        internal override string ToFluenceString() => Value.ToString();
        public override string ToString() => $"NumberValue ({Type}): {Value}";
    }

    /// <summary>A special value indicating a completed statement with no return value..</summary>
    internal sealed record class StatementCompleteValue : Value
    {
        internal static readonly StatementCompleteValue StatementCompleted = new StatementCompleteValue();

        internal override string ToFluenceString() => "<internal: statement_complete>";
        public override string ToString() => $"StatementCompletedValue";
    }

    /// <summary>
    /// Represents a variable passed by reference.
    /// </summary>
    /// <param name="Reference">The variable to pass by reference.</param>
    internal sealed record class ReferenceValue(VariableValue Reference) : Value
    {
        internal override string ToFluenceString() => $"<internal: reference_value__{Reference}";
        public override string ToString() => $"ReferenceValue: {Reference}";
    }

    /// <summary>
    /// A descriptor representing an access to a struct's static function, or a static and solid field.
    /// </summary>
    internal sealed record class StaticStructAccess : Value
    {
        internal StructSymbol Struct { get; init; }

        /// <summary>
        /// The name of the static solid field, or the static function.
        /// </summary>
        internal string Name { get; init; }

        internal StaticStructAccess(StructSymbol structType, string name)
        {
            Struct = structType;
            Name = name;
        }

        internal override string ToFluenceString() => $"<internal: static_struct__<{Struct}__{Name}>";
        public override string ToString() => $"StaticStructAccessValue: <{Struct}__{Name}>";
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
        public override string ToString() => $"ElementAccessValue";
    }

    /// <summary>A descriptor for a broadcast call template, used in chain assignments.</summary>
    internal sealed record class BroadcastCallTemplate : Value
    {
        /// <summary>The function to be called.</summary>
        internal Value Callable { get; init; }

        internal List<Value> Arguments { get; init; }

        /// <summary>The underscore used in pipes.</summary>
        internal int PlaceholderIndex { get; init; }

        public BroadcastCallTemplate(Value callable, List<Value> args, int placeholderIndex)
        {
            Callable = callable;
            Arguments = args;
            PlaceholderIndex = placeholderIndex;
        }

        internal override string ToFluenceString() => "<internal: broadcast_template>";
        public override string ToString() => "BroadcastTemplateValue";
    }

    /// <summary>
    /// A descriptor representing a temporary variable generated by the parser.
    /// This should be resolved by the VM and never seen by the user.
    /// </summary>
    internal sealed record class TempValue : Value
    {
        internal string TempName { get; init; }
        internal int Hash { get; init; }

        internal TempValue(int num)
        {
            TempName = $"__Temp{num}";
            Hash = TempName.GetHashCode();
            HashTable.Register(TempName, Hash);
        }

        internal TempValue(int num, string name)
        {
            TempName = $"__{name}{num}";
            Hash = TempName.GetHashCode();
        }

        internal override string ToFluenceString() => "<internal: temp_register>";
        public override string ToString() => $"TempValue: {TempName}";
    }

    /// <summary>
    /// Represents a lambda function, holding a reference to its function body.
    /// </summary>
    /// <param name="Function">The function body of the lambda.</param>
    internal sealed record class LambdaValue(FunctionValue Function) : Value
    {
        internal override string ToFluenceString() => $"<internal: lambda__{Function.Name}__{Function.Arity}>";
        public override string ToString() => $"LambdaValue: {Function.Name}__{Function.Arity}";
    }

    /// <summary>Represents a function's compile-time blueprint, including its bytecode address or native implementation.</summary>
    internal sealed record class FunctionValue : Value
    {
        /// <summary>The name of the function.</summary>
        internal string Name { get; private set; }

        /// <summary>The number of parameters the function expects.</summary>
        internal int Arity { get; init; }

        /// <summary>
        /// The address of the first instruction of the function's body in the bytecode.
        /// </summary>
        internal int StartAddress { get; private set; }

        /// <summary>The arguments of the function by name.</summary>
        internal List<string> Arguments { get; init; }

        /// <summary>
        /// The arguments of the function passed by reference by name.
        /// </summary>
        internal HashSet<string> ArgumentsByRef { get; init; }

        /// <summary>
        /// The C# intrinsic body of the function, if it is intrinsic.
        /// </summary>
        internal readonly IntrinsicMethod IntrinsicBody;

        /// <summary>
        /// Sets the bytecode start address for this function. Called by the parser during the second pass.
        /// </summary>
        internal int StartAddressInSource { get; init; }

        /// <summary>The scope (namespace) the function belongs to.</summary>
        internal FluenceScope FunctionScope { get; private set; }

        internal FunctionValue(string name, int arity, int startAddress, int lineInSource, List<string> arguments, HashSet<string> argsByRef)
        {
            Name = name;
            Arity = arity;
            StartAddress = startAddress;
            Arguments = arguments;
            ArgumentsByRef = argsByRef;
            StartAddressInSource = lineInSource;
        }

        internal void SetScope(FluenceScope scope) => FunctionScope = scope;

        internal void SetStartAddress(int adr) => StartAddress = adr;

        internal void SetName(string name) => Name = name;

        internal override string ToFluenceString() => $"<internal: function__{Name}/{Arity}>";

        public override string ToString()
        {
            string args = (Arguments == null || Arguments.Count == 0) ? "None" : string.Join(", ", Arguments);
            string argsRef = (ArgumentsByRef == null || ArgumentsByRef.Count == 0) ? "None" : string.Join(", ", ArgumentsByRef);
            return $"FunctionValue: {Name} {FluenceDebug.FormatByteCodeAddress(StartAddress)}, #{Arity} args: {args}. refArgs: {argsRef}";
        }
    }

    internal sealed record class TryCatchValue : Value
    {
        internal int TryGoToIndex { get; set; }
        internal int CatchGoToIndex { get; set; }
        internal string ExceptionAsVar { get; init; }
        internal bool HasExceptionVar { get; init; }
        internal bool CaughtException { get; set; }

        internal TryCatchValue(int tryGoToIndex, string? exceptionAsVar, int catchGoToIndex, bool hasExceptionVar)
        {
            TryGoToIndex = tryGoToIndex;
            ExceptionAsVar = exceptionAsVar ?? "";
            HasExceptionVar = hasExceptionVar;
            CatchGoToIndex = catchGoToIndex;
        }

        internal override string ToFluenceString() => "<internal: try_catch__value>";
        public override string ToString() => $"TryCatchValue: TryJmp: {TryGoToIndex}, CatchJmp: {CatchGoToIndex}.";
    }

    /// <summary>
    /// A descriptor representing a property access operation.
    /// The parser resolves this into GetField or SetField bytecode.
    /// </summary>
    internal sealed record class PropertyAccessValue : Value
    {
        internal Value Target { get; init; }
        internal string FieldName { get; init; }

        internal PropertyAccessValue(Value target, string fieldName)
        {
            Target = target;
            FieldName = fieldName;
        }

        internal override string ToFluenceString() => "<internal: property_access>";
        public override string ToString() => $"FieldAccess<{Target}:{FieldName}>";
    }

    /// <summary>
    /// Represents a variable by its name. The VM resolves this to a value in the current scope.
    /// </summary>
    internal sealed record class VariableValue : Value
    {
        internal string Name { get; init; }
        internal int Hash { get; init; }

        // Whether it is readonly can't always be identified in the parser at the point of creation.
        // Hence we keep this as mutable.
        internal bool IsReadOnly { get; set; }

        internal VariableValue(string identifierValue, bool readOnly = false)
        {
            IsReadOnly = readOnly;
            Name = identifierValue;
            Hash = Name.GetHashCode();
            HashTable.Register(Name, Hash);
        }

        internal override string ToFluenceString() => $"<internal: variable_{(IsReadOnly ? "solid" : "fluid")}>";
        public override string ToString() => $"VariableValue: {Name}:{(IsReadOnly ? "solid" : "fluid")}";
    }

    /// <summary>Represents a specific member of an enum, holding both its name and integer value.</summary>
    internal sealed record class EnumValue : Value
    {
        internal string EnumTypeName { get; init; }
        internal string MemberName { get; init; }
        internal int Value { get; init; }

        internal EnumValue(string enumTypeName, string memberName, int value)
        {
            EnumTypeName = enumTypeName;
            MemberName = memberName;
            Value = value;
        }

        internal override string ToFluenceString() => $"{EnumTypeName}.{MemberName}";
        public override string ToString() => $"EnumValue: {EnumTypeName}.{MemberName}";
    }
}