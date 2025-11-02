namespace Fluence.Exceptions
{
    internal enum RuntimeExceptionType
    {
        NonSpecific,
        UnknownVariable,

        /// <summary>
        /// Indicates an exception that was thrown from the script itself by the programmer using the 'throw' keyword.
        /// </summary>
        ScriptException
    }
}