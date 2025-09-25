namespace Fluence.RuntimeTypes
{
    /// <summary>
    /// Represents a "closure" that binds an instance of an object (the receiver).
    /// </summary>
    internal sealed record class BoundMethodObject(InstanceObject Receiver, FunctionValue Method)
    {
        public override string ToString() => $"<bound method {Method.Name} of {Receiver}>";
    }
}