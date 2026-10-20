using SearchAlgorithms.Algorithms.Data;

namespace SearchAlgorithms.Algorithms;

public interface ISearchAlgorithm
{
    string Name { get; }
    SearchResult Solve(BoardState initialState);
}