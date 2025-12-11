using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fluence
{
    /// <summary>
    /// The abstract base type for all values that can be represented in bytecode.
    /// These objects are used by the parser to build an abstract representation of the code.
    /// </summary>
    public abstract record class Value
    {
        internal virtual int Hash { get; init; }

        internal Value()
        {
            Hash = GetHashCode();
        }

        /// <summary>
        /// Provides a user-facing string representation of the value, as it would appear
        /// when printed within the Fluence language itself.
        /// </summary>
        internal abstract string ToFluenceString();

        /// <summary>
        /// Returns a compact representation optimized for bytecode debugging display.
        /// Designed to prevent column overflow in instruction listings.
        /// </summary>
        internal virtual string ToByteCodeString() => ToString();
    }

    /// <summary>Represents a single character literal.</summary>
    public sealed record class CharValue(char Value) : Value()
    {
        internal override string ToFluenceString() => Value.ToString();
        public override string ToString() => $"CharValue: {Value}";
    }

    /// <summary>Represents a string literal.</summary>
    public sealed record class StringValue(string Value) : Value()
    {
        /// <summary>
        /// Truncates long strings for bytecode display while preserving readability.
        /// </summary>
        private const int MaxDisplayLength = 15;

        internal override string ToFluenceString() => $"\"{Value}\"";

        public override string ToString() => string.IsNullOrEmpty(Value)
            ? "StringValue: \"\""
            : $"StringValue: \"{Value[..Math.Min(MaxDisplayLength, Value.Length)]}{(Value.Length > MaxDisplayLength ? "..." : "")}\"";
    }

    /// <summary>Represents a boolean literal.</summary>
    public sealed record class BooleanValue : Value
    {
        internal bool Value { get; init; }

        internal static readonly BooleanValue True = new BooleanValue(true);
        internal static readonly BooleanValue False = new BooleanValue(false);

        internal BooleanValue(bool value) : base()
        {
            Value = value;
        }

        internal override string ToFluenceString() => Value ? "true" : "false";
        public override string ToString() => $"BooleanValue: {Value}";
    }

    /// <summary>Represents the nil (null) value.</summary>
    public sealed record class NilValue : Value
    {
        internal static readonly NilValue NilInstance = new NilValue();

        internal override string ToFluenceString() => "nil";
        public override string ToString() => $"NilValue";
    }

    /// <summary>
    /// Represents a range expression with start and end bounds.
    /// </summary>
    /// <param name="Start">The inclusive start bound.</param>
    /// <param name="End">The inclusive end bound.</param>
    internal sealed record class RangeValue(Value Start, Value End) : Value()
    {
        internal override string ToFluenceString() =>
                    $"<internal: range_{Start.ToFluenceString()}..{End.ToFluenceString()}>";

        internal override string ToByteCodeString() =>
                    $"Range[{Start.ToByteCodeString()}..{End.ToByteCodeString()}]";

        public override string ToString() =>
                    $"RangeValue: {Start.ToFluenceString()}..{End.ToFluenceString()}";
    }

    /// <summary>
    /// Holds a non boxed integer value for the use in the GoTo family of instructions.
    /// </summary>
    internal sealed record class GoToValue : Value
    {
        internal int Address { get; set; }

        internal GoToValue(int address) : base()
        {
            Address = address;
        }

        internal override string ToFluenceString() =>
                    $"<internal: goto_{Address}";

        internal override string ToByteCodeString() =>
                    $"GoTo {Address}";

        public override string ToString() =>
                    $"GoToValue: {Address}";
    }

    /// <summary>Represents a numerical literal, which can be an Integer, Float, Long, or Double.</summary>
    public sealed record class NumberValue : Value
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

        /// <summary>
        /// Stores already parsed instances of integer values to avoid creating new instances of <see cref="NumberValue"/>s for those integers.
        /// </summary>
        internal static readonly Dictionary<int, NumberValue> ParsedIntegerNumbers = new Dictionary<int, NumberValue>();

        internal object Value { get; private set; }
        internal NumberType Type { get; private set; }

        internal NumberValue(object literal) : base()
        {
            Value = literal;

            AssignNumberType(literal);
        }

        internal NumberValue(object literal, NumberType type) : base()
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
                _ => NumberType.Integer,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static NumberValue FromInt(int value)
        {
            switch (value)
            {
                case 0: return Zero;
                case 1: return One;
                default:
                    ref NumberValue parsed = ref CollectionsMarshal.GetValueRefOrNullRef(ParsedIntegerNumbers, value);

                    if (!Unsafe.IsNullRef(ref parsed))
                    {
                        return parsed;
                    }

                    NumberValue newNum = new NumberValue(value, NumberType.Integer);
                    ParsedIntegerNumbers.Add(value, newNum);
                    return newNum;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static NumberValue FromToken(Token token)
        {
            ReadOnlySpan<char> lexemeSpan = token.Text.AsSpan();

            if (int.TryParse(lexemeSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intVal))
            {
                return FromInt(intVal);
            }
            if (long.TryParse(lexemeSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longVal))
            {
                return new NumberValue(longVal, NumberType.Long);
            }
            if (double.TryParse(lexemeSpan, NumberStyles.Any, CultureInfo.InvariantCulture, out double fallbackVal))
            {
                return new NumberValue(fallbackVal, NumberType.Double);
            }
            if ((lexemeSpan.Contains('.') || lexemeSpan.Contains("e", StringComparison.OrdinalIgnoreCase)) && double.TryParse(lexemeSpan, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleVal))
            {
                return new NumberValue(doubleVal, NumberType.Double);
            }
            if (lexemeSpan.EndsWith("f", StringComparison.OrdinalIgnoreCase) && float.TryParse(lexemeSpan[..^1], out float floatVal))
            {
                return new NumberValue(floatVal, NumberType.Float);
            }
            throw new FormatException($"Invalid number format: '{lexemeSpan}'");
        }

        private string GetTypeShorthand() => Type switch
        {
            NumberType.Double => "Double",
            NumberType.Integer => "Int",
            NumberType.Long => "Long",
            NumberType.Float => "Float",
            _ => throw new NotImplementedException()
        };

        internal override string ToFluenceString() => Value.ToString();
        internal override string ToByteCodeString() => $"{GetTypeShorthand()}_{Value}";
        public override string ToString() => $"NumberValue ({Type}): {Value}";
    }

    /// <summary>A special value indicating a completed statement with no return value..</summary>
    internal sealed record class StatementCompleteValue : Value
    {
        internal static readonly StatementCompleteValue StatementCompleted = new StatementCompleteValue();

        internal StatementCompleteValue() : base() { }

        internal override string ToFluenceString() => "<internal: statement_complete>";
        public override string ToString() => $"StatementCompletedValue";
    }

    /// <summary>
    /// Represents a variable passed by reference.
    /// </summary>
    /// <param name="Reference">The variable to pass by reference.</param>
    internal sealed record class ReferenceValue(VariableValue Reference) : Value()
    {
        internal override string ToFluenceString() => $"<internal: reference_value__{Reference}";
        internal override string ToByteCodeString() => $"Ref__{Reference.ToByteCodeString()}";
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

        internal StaticStructAccess(StructSymbol structType, string name) : base()
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

        internal ElementAccessValue(Value target, Value index) : base()
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

        public BroadcastCallTemplate(Value callable, List<Value> args, int placeholderIndex) : base()
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
        internal int TempIndex { get; init; }

        /// <summary>
        /// The runtime register index (-1 = unallocated).
        /// </summary>
        internal int RegisterIndex { get; set; } = -1;

        internal TempValue(int num)
        {
            TempIndex = num;
            Hash = TempIndex.GetHashCode();
        }

        internal override string ToFluenceString() => "<internal: temp_register>";
        internal override string ToByteCodeString() => $"__Temp{TempIndex}_{RegisterIndex}";
        public override string ToString() => $"TempValue: {TempIndex}, Index: {RegisterIndex}";
    }

    /// <summary>
    /// Represents a variable by its name. The VM resolves this to a value in the current scope.
    /// </summary>
    internal sealed record class VariableValue : Value
    {
        internal string Name { get; init; }

        /// <summary>
        /// Mutable readonly flag - finalized during semantic analysis.
        /// Allows deferred readonly determination across parser phases.
        /// </summary>
        internal bool IsReadOnly { get; set; }

        /// <summary>
        /// The runtime register index (-1 = unallocated).
        /// </summary>
        internal int RegisterIndex { get; set; } = -1;

        /// <summary>
        /// Indicates whether this variable is a global variable in the scope it is defined, outside of any function.
        /// </summary>
        internal bool IsGlobal { get; set; }

        internal static readonly VariableValue SelfVariable = new VariableValue("self");

        internal VariableValue(string identifierValue)
        {
            Name = identifierValue;
            Hash = Name.GetHashCode();
        }

        internal override string ToFluenceString() => $"<internal: variable_{(IsReadOnly ? "solid" : "fluid")}>";
        internal override string ToByteCodeString() => $"Var_{Name}_{RegisterIndex}_{IsGlobal}";
        public override string ToString() => $"VariableValue: {Name}:{(IsReadOnly ? "solid" : "fluid")}, Index: {RegisterIndex}, IsGlobal: {IsGlobal}";
    }

    /// <summary>
    /// Represents a lambda function, holding a reference to its function body.
    /// </summary>
    /// <param name="Function">The function body of the lambda.</param>
    internal sealed record class LambdaValue(FunctionValue Function) : Value()
    {
        internal override string ToFluenceString() => $"<internal: lambda__{Function.Name}__{Function.Arity}>";
        public override string ToString() => $"LambdaValue: {Function.Name}__{Function.Arity}_{Function.StartAddress}";
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

        /// <summary>The hash codes of the function's arguments.</summary>
        internal List<int> ArgumentHashCodes { get; init; }

        /// <summary>
        /// The arguments of the function passed by reference by name.
        /// </summary>
        internal HashSet<string> ArgumentsByRef { get; init; }

        /// <summary>
        /// Sets the bytecode start address for this function. Called by the parser during the second pass.
        /// </summary>
        internal int StartAddressInSource { get; init; }

        /// <summary>The scope (namespace) the function belongs to.</summary>
        internal FluenceScope DefiningScope { get; init; }

        /// <summary>
        /// The total amount of register slots this function requires to execute its bytecode.
        /// </summary>
        internal int TotalRegisterSlots { get; set; }

        /// <summary>
        /// Indicates whether the function is an instance or a static method of some struct type.
        /// </summary>
        internal bool BelongsToAStruct { get; init; }

        internal FunctionValue(string name, bool inStruct, int arity, int startAddress, int lineInSource, List<string> arguments, HashSet<string> argsByRef, FluenceScope scope)
        {
            BelongsToAStruct = inStruct;
            Name = name;
            Arity = arity;
            StartAddress = startAddress;
            Arguments = arguments;

            ArgumentHashCodes = new List<int>();
            for (int i = 0; i < Arguments.Count; i++)
            {
                ArgumentHashCodes.Add(Arguments[i].GetHashCode());
            }

            ArgumentsByRef = argsByRef;
            StartAddressInSource = lineInSource;
            DefiningScope = scope;

            Hash = name.GetHashCode();
        }

        /// <summary>
        /// Sets the bytecode start address for this function. Called by the parser during the second pass.
        /// </summary>
        internal void SetStartAddress(int addr) => StartAddress = addr;

        internal void SetName(string name) => Name = name;

        internal override string ToFluenceString() => $"<internal: function__{Name}/{Arity}, RegSize: {TotalRegisterSlots}>";

        internal override string ToByteCodeString() => $"Func_{Name}_{Arity}_{TotalRegisterSlots}_{DefiningScope}_{StartAddress}";

        public override string ToString()
        {
            string args = (Arguments == null || Arguments.Count == 0) ? "None" : string.Join(", ", Arguments);
            string argsRef = (ArgumentsByRef == null || ArgumentsByRef.Count == 0) ? "None" : string.Join(", ", ArgumentsByRef);
            return $"FunctionValue: {Name} {FluenceDebug.FormatByteCodeAddress(StartAddress)}, #{Arity} args: {args}. refArgs: {argsRef}, RegSize: {TotalRegisterSlots}, Scope: {DefiningScope}";
        }
    }

    internal sealed record class TryCatchValue : Value
    {
        internal int TryGoToIndex { get; set; }
        internal int CatchGoToIndex { get; set; }
        internal string? ExceptionVarName { get; set; }
        internal int ExceptionAsVarRegisterIndex { get; set; }
        internal bool HasExceptionVar { get; init; }
        internal bool CaughtException { get; set; }

        internal TryCatchValue(int tryGoToIndex, string exceptionVarName, int exceptionVarRegisterIndex, int catchGoToIndex, bool hasExceptionVar) : base()
        {
            ExceptionVarName = exceptionVarName;
            TryGoToIndex = tryGoToIndex;
            ExceptionAsVarRegisterIndex = exceptionVarRegisterIndex;
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

        internal PropertyAccessValue(Value target, string fieldName) : base()
        {
            Target = target;
            FieldName = fieldName;
        }

        internal override string ToFluenceString() => "<internal: property_access>";
        public override string ToString() => $"PropertyAccessValue<{Target}:{FieldName}>";
    }

    /// <summary>Represents a specific member of an enum, holding both its name and integer value.</summary>
    public sealed record class EnumValue : Value
    {
        internal string EnumTypeName { get; init; }
        internal string MemberName { get; init; }
        internal int Value { get; init; }

        internal EnumValue(string enumTypeName, string memberName, int value) : base()
        {
            EnumTypeName = enumTypeName;
            MemberName = memberName;
            Value = value;
        }

        internal override string ToFluenceString() => $"{EnumTypeName}.{MemberName}";
        public override string ToString() => $"EnumValue: {EnumTypeName}.{MemberName}";
    }
}