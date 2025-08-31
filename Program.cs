namespace Fluence
{
    internal class Program
    {
        internal static void Main(string[] args)
        {
            string source = File.ReadAllText($@"{Directory.GetCurrentDirectory()}\Full Lexer Test.fl");
            FluenceInterpreter fluenceInterpreter = new FluenceInterpreter();

            if (fluenceInterpreter.Compile(source, true))
            {
                fluenceInterpreter.RunFor(TimeSpan.FromSeconds(30));
            }
        }
    }
}