namespace ExternalSortingAlgorithms.Benchmarks;

public sealed class BenchmarkResult
{
    public string AlgorithmName { get; }
    public long[] ElapsedMilliseconds { get; }

    public BenchmarkResult(string algorithmName, long[] elapsedMilliseconds)
    {
        AlgorithmName = algorithmName;
        ElapsedMilliseconds = elapsedMilliseconds;
    }
}