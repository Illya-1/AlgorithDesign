using SearchAlgorithms.Algorithms.Data;

namespace SearchAlgorithms.Algorithms.RBFS;

public struct RbfsNode
{
    public BoardState State;
    public int F;
    public int G;

    public RbfsNode(BoardState state, int f, int g)
    {
        State = state;
        F = f;
        G = g;
    }
}