using System.Runtime.CompilerServices;

namespace SearchAlgorithms.Algorithms.Data;

[InlineArray(BoardSize)]
public struct BoardState
{
    public const int BoardSize = 8;
    public const int ChildBoardsCount = BoardSize * (BoardSize - 1);
    
    private byte _element0;
    
    public readonly BoardChildStates GenerateSuccessors()
    {
        BoardChildStates result = default;
        int index = 0;

        for (int col = 0; col < 8; col++)
        {
            byte currentRow = this[col];

            for (byte row = 0; row < 8; row++)
            {
                if (row == currentRow)
                    continue;

                BoardState child = this;
                child[col] = row;
                result[index++] = child;
            }
        }

        return result;
    }

    public override string ToString()
    {
        return $"[{this[0]},{this[1]},{this[2]},{this[3]},{this[4]},{this[5]},{this[6]},{this[7]}]";
    }

    public static BoardState GenRandom()
    {
        BoardState board = new BoardState();
        for (int i = 0; i < BoardSize; i++)
        {
            board[i] = (byte) Random.Shared.Next(0, 8);
        }

        return board;
    }
}


[InlineArray(BoardState.ChildBoardsCount)]
public struct BoardChildStates
{
    private BoardState _element0;
}