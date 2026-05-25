using Fluence.Exceptions;
using Fluence.RuntimeTypes;

namespace Fluence.Extensions
{
    /// <summary>
    /// Represents a limited view of the VM available to native functions.
    /// </summary>
    public interface INativeVmContext
    {
        RuntimeValue PopStack();
        void SignalError(string message, RuntimeExceptionType exceptionType = RuntimeExceptionType.NonSpecific);

        RuntimeValue CreateValue(int value);
        RuntimeValue CreateValue(float value);
        RuntimeValue CreateValue(double value);
        RuntimeValue CreateValue(bool value);
        RuntimeValue CreateValue(string value);
    }
}