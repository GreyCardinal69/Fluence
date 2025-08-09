using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        internal double Value;

        internal NumberValue(double literal)
        {
            Value = literal;
        }

        public override string ToString()
        {
            return $"NumberValue: {Value}";
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

    internal class ListValue : Value
    {

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