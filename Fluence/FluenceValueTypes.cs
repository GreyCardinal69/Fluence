namespace Fluence
{
    internal abstract record class Value
    {
        public override string ToString()
        {
            return "";
        }

        internal virtual object GetValue() { return null; }
    }

    internal sealed record class CharValue : Value
    {
        internal char Value;

        internal CharValue(char value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return $"CharValue: '{Value}'";
        }
    }

    internal sealed record class StatementCompleteValue : Value
    {
        // It has no data. Its existence is its meaning.
        public override string ToString() => "StatementComplete";
        internal override object GetValue() => null;
    }

    internal sealed record class ElementAccessValue : Value
    {
        internal Value Target { get; }
        internal Value Index { get; }
        internal string TempName;
        internal object Value;

        internal ElementAccessValue(Value target, Value index, int num, string name)
        {
            TempName = $"{name}{num}";
            Target = target;
            Index = index;
        }

        public override string ToString()
        {
            return $"ElementAccessValue: {TempName}";
        }
    }

    internal sealed record class BroadcastCallTemplate : Value
    {
        // The function to be called.
        internal Value Callable { get; }
        internal List<Value> Arguments { get; }
        // The underscore used in pipes.
        internal int PlaceholderIndex { get; }

        public BroadcastCallTemplate(Value callable, List<Value> args, int placeholderIndex)
        {
            Callable = callable;
            Arguments = args;
            PlaceholderIndex = placeholderIndex;
        }
    }

    internal sealed record class StringValue : Value
    {
        internal string Text;

        internal StringValue(string text)
        {
            Text = text;
        }

        public override string ToString()
        {
            string str = Text ?? "__Null";
            str = str == "" ? "__EmptyString" : str;

            return $"StringValue: \"{str}\"";
        }
    }

    internal sealed record class NumberValue : Value
    {
        internal enum NumberType
        {
            Integer,
            Float,
            Double
        }

        internal object Value { get; }
        internal NumberType Type { get; }

        internal NumberValue(object literal, NumberType type = NumberType.Integer)
        {
            Value = literal;
            Type = type;
        }

        public static NumberValue FromToken(Token token)
        {
            string lexeme = token.Text;

            // Check for float suffix
            if (lexeme.EndsWith("f", StringComparison.OrdinalIgnoreCase))
            {
                // It's a float. Parse it as such.
                string floatStr = lexeme.Substring(0, lexeme.Length - 1);
                if (float.TryParse(floatStr, out float floatVal))
                {
                    return new NumberValue(floatVal, NumberType.Float);
                }
            }
            // Check for decimal point (but not float) -> double
            else if (lexeme.Contains('.'))
            {
                if (double.TryParse(lexeme, out double doubleVal))
                {
                    return new NumberValue(doubleVal, NumberType.Double);
                }
            }
            // Otherwise, it's an integer.
            else
            {
                if (int.TryParse(lexeme, out int intVal))
                {
                    return new NumberValue(intVal, NumberType.Integer);
                }
            }

            if (double.TryParse(lexeme, out double fallbackVal))
            {
                return new NumberValue(fallbackVal, NumberType.Double);
            }

            // SHOULD BE A PARSER ERROR, this for now.
            throw new FormatException($"Invalid number format: '{lexeme}'");
        }

        public override string ToString()
        {
            return $"NumberValue ({Type}): {Value}";
        }

        internal override object GetValue()
        {
            return Value;
        }
    }

    internal sealed record class NilValue : Value
    {
        public override string ToString()
        {
            return $"NilValue: Nil";
        }
    }

    internal sealed record class BooleanValue : Value
    {
        internal bool Value;

        internal BooleanValue(bool val)
        {
            Value = val;
        }

        public override string ToString()
        {
            return $"BooleanValue: {Value}";
        }
    }

    internal sealed record class TempValue : Value
    {
        internal string TempName;
        internal object Value;

        internal TempValue(int num)
        {
            TempName = $"__Temp{num}";
        }

        internal TempValue(int num, string name)
        {
            TempName = $"{name}{num}";
        }

        public override string ToString()
        {
            return $"TempValue: {TempName}";
        }
    }

    internal sealed record class FunctionValue : Value
    {
        // The name of the function (for debugging/stack traces).
        internal string Name { get; }
        // The number of parameters the function expects.
        internal int Arity { get; }
        // The address of the first instruction of the function's body in the bytecode.
        internal int StartAddress { get; private set; }

        internal readonly IntrinsicMethod IntrinsicBody;
        internal readonly bool IsIntrinsic;

        internal FunctionValue(string name, int arity, int startAddress)
        {
            Name = name;
            Arity = arity;
            StartAddress = startAddress;
        }

        public FunctionValue(string name, int arity, IntrinsicMethod body)
        {
            Name = name;
            Arity = arity;
            StartAddress = -1;
            IsIntrinsic = true;
            IntrinsicBody = body;
        }

        internal void SetStartAddress(int adr)
        {
            StartAddress = adr;
        }

        public override string ToString()
        {
            return $"FunctionValue: {Name} {FluenceDebug.FormatByteCodeAddress(StartAddress)}, {Arity} args.";
        }
    }

    internal sealed record class PropertyAccessValue : Value
    {
        internal Value Target;
        internal string FieldName;

        internal PropertyAccessValue(Value target, string fieldName)
        {
            Target = target;
            FieldName = fieldName;
        }

        public override string ToString()
        {
            return $"FieldAccess<{Target}:{FieldName}>";
        }
    }

    internal sealed record class VariableValue : Value
    {
        internal string IdentifierValue;

        internal VariableValue(string identifierValue)
        {
            IdentifierValue = identifierValue;
        }

        public override string ToString()
        {
            return $"VariableValue: {IdentifierValue}";
        }
    }

    internal sealed record class InstanceValue : Value
    {
        internal StructSymbol Type { get; }
        internal Dictionary<string, Value> Fields { get; } = new();

        internal InstanceValue(StructSymbol type)
        {
            Type = type;
        }

        public override string ToString()
        {
            return $"InstanceValue<{Type.Name}>";
        }
    }

    internal sealed record class EnumValue : Value
    {
        internal string EnumTypeName { get; }
        internal string MemberName;
        internal int Value;

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

        internal override object GetValue()
        {
            return Value;
        }
    }
}