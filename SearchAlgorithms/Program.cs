using SearchAlgorithms.Algorithms.BFS;
using SearchAlgorithms.Algorithms.RBFS;

namespace SearchAlgorithms;

static class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        RunBenchmarks();
    }

    private static void RunBenchmarks()
    {
        var benchmarkRunner = new BenchmarkRunner(20, @"C:\Users\ilyap\Downloads\");
        benchmarkRunner.Run(new BfsSearchAlgorithm(1 * Units.GB));
        benchmarkRunner.Run(new RbfsSearchAlgorithm(new F2Heuristic(), maxDepth: 1024)); //Що значно менше за 1ГБ
        benchmarkRunner.Run(new RbfsSearchAlgorithm(new ConflictedColumnsHeuristic(), maxDepth: 1024));
    }
}