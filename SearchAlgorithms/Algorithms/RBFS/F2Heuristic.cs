using SearchAlgorithms.Algorithms.Data;

namespace SearchAlgorithms.Algorithms.RBFS;

public sealed class F2Heuristic : IHeuristic
{
    public string Name => "F2 (Attacking Pairs)";

    public int Calculate(BoardState board)
    {
        return F2ConflictsCalculator.Calculate(board);
    }
}