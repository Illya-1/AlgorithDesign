namespace ExternalSortingAlgorithms.Algorithms;

public interface IAlgorithm
{
    void Run(string filepath);

    string Name { get; }
}