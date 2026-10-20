using SearchAlgorithms.Algorithms.Data;

namespace SearchAlgorithms.Algorithms;

public static class F2ConflictsCalculator
{
    public static int Calculate(BoardState board)
    {
        int conflicts = 0;
        for (int i = 0; i < 7; i++)
        {
            byte rowI = board[i];
            for (int j = i + 1; j < 8; j++)
            {
                byte rowJ = board[j];
                if (rowI == rowJ || Math.Abs(rowI - rowJ) == (j - i))
                {
                    conflicts++;
                }
            }
        }
        return conflicts;
    }
}