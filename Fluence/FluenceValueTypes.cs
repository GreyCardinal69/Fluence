using System;
using System.Text;

namespace Fluence
{
    internal abstract class Value
    {
        public override string ToString()
        {
            return "";
        }

        internal virtual object GetValue() { return null; }
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
            return $"StringValue: {Text}";
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


        internal NumberValue(object literal, NumberType type)
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

    internal class NilValue : Value
    {

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

        internal TempValue(int num)
        {
            TempName = $"Temp{num}";
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
}