using System.Diagnostics;
using SearchAlgorithms.Algorithms.Data;

namespace SearchAlgorithms.Algorithms.BFS;

public sealed class BfsSearchAlgorithm(long memoryLimitBytes) : ISearchAlgorithm
{
    private readonly int _maxNodesLimit = Units.ElementsCount<BfsNode>(memoryLimitBytes);

    public string Name => "Breadth-First Search (AS IS)";

    public SearchResult Solve(BoardState initialState)
    {
        if (F2ConflictsCalculator.Calculate(initialState) == 0)
        {
            return new SearchResult(
                true,
                initialState,
                initialState,
                0,
                0,
                1,
                1,
                TimeSpan.Zero
            );
        }
        
        Stopwatch stopwatch = Stopwatch.StartNew();
        
        long iterations = 0;
        long nodesGenerated = 1;
        long maxNodesInMemory = 1;
        bool isSuccess = false;
        BoardState solutionState = initialState;

        Queue<BfsNode> queue = new Queue<BfsNode>();
        queue.Enqueue(new BfsNode(initialState, 0));
        
        while (queue.Count > 0)
        {
            if (queue.Count > maxNodesInMemory)
            {
                maxNodesInMemory = queue.Count;
            }

            if (queue.Count >= _maxNodesLimit)
            {
                break;
            }

            BfsNode current = queue.Dequeue();
            iterations++;

            BoardChildStates childBoards = current.State.GenerateSuccessors();

            for (int i = 0; i < 56; i++)
            {
                BoardState nextState = childBoards[i];
                nodesGenerated++;

                if (F2ConflictsCalculator.Calculate(nextState) == 0)
                {
                    isSuccess = true;
                    solutionState = nextState;
                    if (queue.Count > maxNodesInMemory)
                    {
                        maxNodesInMemory = queue.Count;
                    }
                    break;
                }

                queue.Enqueue(new BfsNode(nextState, current.Depth + 1));
            }

            if (isSuccess)
            {
                break;
            }
        }

        stopwatch.Stop();

        return new SearchResult(
            isSuccess,
            initialState,
            solutionState,
            iterations,
            0,
            nodesGenerated,
            maxNodesInMemory,
            stopwatch.Elapsed
        );
    }
}