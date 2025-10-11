namespace Fluence.VirtualMachine
{
    internal readonly record struct StackFrameInfo
    {
        internal readonly string FunctionName;
        internal readonly string FileName;
        internal readonly int LineNumber;

        internal StackFrameInfo(string name, string fileName, int line)
        {
            FunctionName = name;
            FileName = fileName;
            LineNumber = line;
        }

        public override string ToString()
        {
            return $"StackFrameInfo: Line:{LineNumber}, Function:{FunctionName}, File:{FileName}";
        }
    }
}