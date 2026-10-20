using SearchAlgorithms.Algorithms.Data;

namespace SearchAlgorithms.Algorithms.RBFS;

public sealed class ConflictedColumnsHeuristic : IHeuristic
{
    public string Name => "Custom (Conflicted Columns)";

    public int Calculate(BoardState board)
    {
        int conflictedColumnsCount = 0;

        for (int i = 0; i < 8; i++)
        {
            byte rowI = board[i];
            bool hasConflict = false;

            for (int j = 0; j < 8; j++)
            {
                if (i == j)
                {
                    continue;
                }

                byte rowJ = board[j];
                
                if (rowI == rowJ || Math.Abs(rowI - rowJ) == Math.Abs(i - j))
                {
                    hasConflict = true;
                    break;
                }
            }

            if (hasConflict)
            {
                conflictedColumnsCount++;
            }
        }

        return conflictedColumnsCount;
    }
}