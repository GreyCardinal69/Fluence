namespace Fluence
{
    internal abstract record class ObjectValue : Value;

    internal sealed record class ListValue(List<Value> Elements) : ObjectValue
    {
        public override string ToString()
        {
            return $"ListValue: [{string.Join(", ", Elements)}]";
        }
    }
}