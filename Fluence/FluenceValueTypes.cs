using Fluence;
using System.Text;

namespace Fluence
{
    internal abstract class Value
    {
        internal string Name = "";

        public override string ToString()
        {
            return "";
        }

        internal virtual object GetValue() { return null; }
    }

    internal class CharValue : Value
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

    internal sealed class StatementCompleteValue : Value
    {
        // It has no data. Its existence is its meaning.
        public override string ToString() => "StatementComplete";
        internal override object GetValue() => null;
    }

    internal sealed class ElementAccessValue : Value
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

    internal sealed class BroadcastCallTemplate : Value
    {
        // The function to be called (e.g., VariableValue("print"))
        internal Value Callable { get; }
        // The list of arguments, with one being a special placeholder.
        internal List<Value> Arguments { get; }
        // The index of the placeholder in the argument list.
        internal int PlaceholderIndex { get; }

        public BroadcastCallTemplate(Value callable, List<Value> args, int placeholderIndex)
        {
            Callable = callable;
            Arguments = args;
            PlaceholderIndex = placeholderIndex;
        }
    }

    internal class StringValue : Value
    {
        internal string Text;

        internal StringValue(string text)
        {
            Text = text;
        }

        public override string ToString()
        {
            string str = Text == null ? "__Null" : Text;
            str = str == "" ? "__EmptyString" : str;

            return $"StringValue: \"{str}\"";
        }
    }

    internal class NumberValue : Value
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

    internal sealed class NilValue : Value
    {
        public override string ToString()
        {
            return $"NilValue: Nil";
        }
    }

    internal class BooleanValue : Value
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

    internal class TempValue : Value
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

    internal sealed class ListValue : Value
    {
        internal readonly List<Value> List = new List<Value>();

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            int i = 0;
            sb.Append($"List Elements:\n");
            foreach (var val in List)
            {
                sb.Append($"{i}.\r{val.ToString()}\n");
                i++;
            }

            return sb.ToString();
        }
    }

    internal class FunctionValue : Value
    {
        // The name of the function (for debugging/stack traces).
        internal string Name { get; }
        // The number of parameters the function expects.
        internal int Arity { get; }
        // The address of the first instruction of the function's body in the bytecode.
        internal int StartAddress { get; private set;  }
        // Debug full address.
        internal string FullAddress { get; private set; }

        internal FunctionValue(string name, int arity, int startAddress, string fullAddress)
        {
            Name = name;
            Arity = arity;
            StartAddress = startAddress;
            FullAddress = fullAddress;
        }

        internal void SetStartAddress(int adr, string formatted)
        {
            StartAddress = adr;
            FullAddress = formatted;
        }

        public override string ToString()
        {
            return $"FunctionValue: {Name} {FullAddress}, {Arity} args.";
        }
    }

    internal class VariableValue : Value
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

    internal sealed class InstanceValue : Value
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

    internal sealed class EnumValue : Value
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