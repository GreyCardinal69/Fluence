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
                Console.WriteLine(fluenceInterpreter.GetGlobal("bullshit"));
           fluenceInterpreter.SetGlobal("bullshit", 10000);
                Console.WriteLine(fluenceInterpreter.GetGlobal("bullshit"));
            }
        }
    }
}