using BenchmarkDotNet.Attributes;

namespace Fluence.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkDotNet.Running.BenchmarkRunner.Run<Benchmarker>();
            Console.ReadLine();
        }
    }

    [MemoryDiagnoser]
    public class Benchmarker
    {
    }
}