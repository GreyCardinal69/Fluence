namespace Fluence.VirtualMachine
{
    /// <summary>
    /// Represents a single frame in the virtual machine's call stack, used primarily for generating stack traces during exceptions.
    /// </summary>
    /// <param name="FunctionName">The mangled or original name of the executing function.</param>
    /// <param name="FileName">The source file where the function is defined.</param>
    /// <param name="LineNumber">The line number in the source file where the function was called or declared.</param>
    internal readonly record struct StackFrameInfo(string FunctionName, string FileName, int LineNumber)
    {
        public override string ToString() => $"StackFrameInfo: Line:{LineNumber}, Function:{FunctionName}, File:{FileName}";
    }
}