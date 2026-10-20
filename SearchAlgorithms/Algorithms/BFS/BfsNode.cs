using SearchAlgorithms.Algorithms.Data;

namespace SearchAlgorithms.Algorithms.BFS;

public readonly struct BfsNode
{
    public readonly BoardState State;
    public readonly int Depth;

    public BfsNode(BoardState state, int depth)
    {
        State = state;
        Depth = depth;
    }
}