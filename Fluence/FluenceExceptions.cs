using System.Text;

namespace Fluence
{
    internal record FluenceExceptionContext
    {
        internal int LineNum { get; init; }
        internal int Column {  get; init; }
        internal Token Token { get; init; }
        // Line number where error occured and the string of that line is better than just 
        // the number.
        internal string FaultyLine { get; init; }
    }

    public class FluenceException : Exception
    {
        internal readonly FluenceExceptionContext _context;

        public FluenceException(string message) : base(message)
        {
            _context = new FluenceExceptionContext();
        }

        internal FluenceException(string message, FluenceExceptionContext context) : base(message)
        {
            _context = context;
        }

        public override string Message
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();

                if (_context.LineNum > 0 && _context.FaultyLine != null && _context.Column > 0)
                {
                    stringBuilder
                        .AppendLine($"Exception at line {_context.LineNum}, Column {_context.Column}")
                        .AppendLine($"{_context.LineNum}. \t{_context.FaultyLine}")
                        .AppendLine($"{new string(' ', _context.Column - 1)}^");
                }

                stringBuilder.AppendLine($"Exception: {base.Message}");

                if (_context.Token != null)
                {
                    stringBuilder.AppendLine($"Faulty Token <Type, Text, Literal>: <{_context.Token.Type} , {_context.Token.Text}, {_context.Token.Literal}>");
                }

                return stringBuilder.ToString();
            }
        }
    }

    public class FluenceLexerException : FluenceException
    {
        internal FluenceLexerException(string message, FluenceExceptionContext context)
            : base(message, context) { }
    }
}