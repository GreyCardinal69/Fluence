using Fluence.RuntimeTypes;

namespace Fluence
{
    /// <summary>
    /// The abstract base type for all values that can be represented in bytecode.
    /// These objects are used by the parser to build an abstract representation of the code.
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
        public override string ToString() => $"CharValue: {Value}";
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

        internal override object GetValue() => Value;
        internal override string ToFluenceString() => Value ? "true" : "false";
        public override string ToString() => $"BooleanValue: {Value}";
    }

    /// <summary>Represents the nil (null) value.</summary>
    internal sealed record class NilValue : Value
    {
        internal static readonly NilValue NilInstance = new NilValue();

        internal override object GetValue() => null!;
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

        internal override object GetValue() => Value;
        internal override string ToFluenceString() => Value.ToString()!;
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
        internal override string ToFluenceString()
        {
            return $"<internal: reference_value__{Reference}";
        }

        public override string ToString()
        {
            return $"ReferenceValue: {Reference}";
        }
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
        public override string ToString() => "BroadcastTemplateValue";
    }

    /// <summary>
    /// A descriptor representing a temporary variable generated by the parser.
    /// This should be resolved by the VM and never seen by the user.
    /// </summary>
    internal sealed record class TempValue : Value
    {
        internal string TempName { get; init; }

        internal TempValue(int num) => TempName = $"__Temp{num}";
        internal TempValue(int num, string name) => TempName = $"__{name}{num}";

        internal override string ToFluenceString() => "<internal: temp_register>";
        public override string ToString() => $"TempValue: {TempName}";
    }

    /// <summary>
    /// Represents a simple list.
    /// </summary>
    internal sealed record class ListValue : Value
    {
        internal List<Value> Elements { get; init; }

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
        /// <summary>
        /// The name of the function.
        /// </summary>
        internal string Name { get; private set; }

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
        /// The arguments of the function passed by reference by name.
        /// </summary>
        internal HashSet<string> ArgumentsByRef { get; init; }

        /// <summary>
        /// The C# intrinsic body of the function, if it is intrinsic.
        /// </summary>
        internal readonly IntrinsicMethod IntrinsicBody;

        /// <summary>
        /// Is the function intrinsic?
        /// </summary>
        internal bool IsIntrinsic { get; init; }

        /// <summary>
        /// Sets the bytecode start address for this function. Called by the parser during the second pass.
        /// </summary>
        internal int StartAddressInSource { get; init; }

        /// <summary>
        /// The scope (namespace) the function belongs to.
        /// </summary>
        internal FluenceScope FunctionScope { get; private set; }

        /// <summary>
        /// The struct the function is defined in, if in any.
        /// </summary>
        internal StructSymbol Class { get; private set; }

        internal FunctionValue()
        {
        }

        internal FunctionValue(string name, int arity, int startAddress, int lineInSource, List<string> arguments = null!, HashSet<string> argsByRef = null!)
        {
            Name = name;
            Arity = arity;
            StartAddress = startAddress;
            Arguments = arguments;
            ArgumentsByRef = argsByRef;
            StartAddressInSource = lineInSource;
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

        internal void SetScope(FluenceScope scope) => FunctionScope = scope;

        internal void SetStartAddress(int adr) => StartAddress = adr;

        internal void SetName(string name) => Name = name;

        internal void SetClass(StructSymbol structSymbol) => Class = structSymbol;

        internal override string ToFluenceString() => $"<internal: function__{Name}/{Arity}>";

        public override string ToString()
        {
            string args = (Arguments == null || Arguments.Count == 0) ? "None" : string.Join(", ", Arguments);
            string argsRef = (ArgumentsByRef == null || ArgumentsByRef.Count == 0) ? "None" : string.Join(", ", ArgumentsByRef);
            return $"FunctionValue: {Name} {FluenceDebug.FormatByteCodeAddress(StartAddress)}, #{Arity} args: {args}. refArgs: {argsRef}";
        }
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
        internal bool IsReadOnly { get; set; }

        internal VariableValue(string identifierValue, bool readOnly = false)
        {
            IsReadOnly = readOnly;
            Name = identifierValue;
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

        internal override object GetValue() => Value;
        internal override string ToFluenceString() => $"{EnumTypeName}.{MemberName}";
        public override string ToString() => $"EnumValue: {EnumTypeName}.{MemberName}";
    }

    /// <summary>
    /// Represents a struct instance.
    /// </summary>
    internal sealed record class StructValue : Value
    {
        internal StructSymbol Struct { get; init; }
        internal Dictionary<string, RuntimeValue> Fields { get; init; }

        internal StructValue(StructSymbol structSym, Dictionary<string, RuntimeValue> fields)
        {
            Struct = structSym;
            Fields = fields;
        }

        internal override string ToFluenceString() => $"<internal: struct_value>";
        public override string ToString() => $"{Struct} + {Fields}";
    }
}