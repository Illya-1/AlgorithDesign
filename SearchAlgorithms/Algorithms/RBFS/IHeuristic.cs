using SearchAlgorithms.Algorithms.Data;

namespace SearchAlgorithms.Algorithms.RBFS;

public interface IHeuristic
{
    string Name { get; }
    int Calculate(BoardState state);
}