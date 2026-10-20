using System.Diagnostics;
using SearchAlgorithms.Algorithms.Data;

namespace SearchAlgorithms.Algorithms.RBFS;

public sealed class RbfsSearchAlgorithm : ISearchAlgorithm
{
    public RbfsSearchAlgorithm(IHeuristic heuristic, int maxDepth)
    {
        _maxDepth = maxDepth;
        _heuristic = heuristic;
        _nodesBuffer = new RbfsNode[_maxDepth * BoardState.ChildBoardsCount];
    }

    public RbfsSearchAlgorithm(IHeuristic heuristic, long memoryLimitBytes) 
        : this(
            heuristic, 
            Units.ElementsCount<RbfsNode>(memoryLimitBytes / BoardState.ChildBoardsCount)
        )
    {
    }
    
    private readonly int _maxDepth;
    private readonly IHeuristic _heuristic;
    private readonly RbfsNode[] _nodesBuffer;

    private long _iterations;
    private long _deadEnds;
    private long _nodesGenerated;
    private long _maxNodesInMemory;
    private bool _isSuccess;
    private BoardState _solutionState;

    public string Name => "RBFS (" + _heuristic.Name + ")";

    public SearchResult Solve(BoardState initialState)
    {
        int initialH = _heuristic.Calculate(initialState);
        if (initialH == 0)
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

        _iterations = 0;
        _deadEnds = 0;
        _nodesGenerated = 1;
        _maxNodesInMemory = 1;
        _isSuccess = false;
        _solutionState = initialState;

        Stopwatch stopwatch = Stopwatch.StartNew();

        RbfsNode root = new RbfsNode(initialState, initialH, 0);
        Search(root, int.MaxValue, 0);

        stopwatch.Stop();

        return new SearchResult(
            _isSuccess,
            initialState,
            _solutionState,
            _iterations,
            _deadEnds,
            _nodesGenerated,
            _maxNodesInMemory,
            stopwatch.Elapsed
        );
    }

    private int Search(RbfsNode node, int fLimit, int depth)
    {
        _iterations++;

        if (_heuristic.Calculate(node.State) == 0)
        {
            _isSuccess = true;
            _solutionState = node.State;
            return node.F;
        }

        if (depth >= _maxDepth - 1)
        {
            _deadEnds++;
            return int.MaxValue;
        }

        long currentNodesInMemory = (long)(depth + 1) * BoardState.ChildBoardsCount;
        if (currentNodesInMemory > _maxNodesInMemory)
        {
            _maxNodesInMemory = currentNodesInMemory;
        }

        GenRbfsChildNodes(node, depth);

        FindBestAndAlternative(depth, out int bestFIndex, out int altF);
        while (_nodesBuffer[bestFIndex].F <= fLimit && !_isSuccess)
        {
            int subLimit = fLimit < altF ? fLimit : altF;
            int newF = Search(_nodesBuffer[bestFIndex], subLimit, depth + 1);

            if (_isSuccess)
            {
                return newF;
            }

            _nodesBuffer[bestFIndex].F = newF;

            FindBestAndAlternative(depth, out bestFIndex, out altF);
        }

        if (!_isSuccess)
        {
            _deadEnds++;
        }

        return _nodesBuffer[bestFIndex].F;
    }

    private void GenRbfsChildNodes(RbfsNode parentNode, int depth)
    {
        int levelOffset = depth * BoardState.ChildBoardsCount;
        BoardChildStates successors = parentNode.State.GenerateSuccessors();
        _nodesGenerated += BoardState.ChildBoardsCount;

        for (int i = 0; i < BoardState.ChildBoardsCount; i++)
        {
            BoardState childState = successors[i];
            int g = parentNode.G + 1;
            int h = _heuristic.Calculate(childState);
            int rawF = g + h;
            int f = rawF > parentNode.F ? rawF : parentNode.F;

            _nodesBuffer[levelOffset + i] = new RbfsNode(childState, f, g);
        }
    }

    private void FindBestAndAlternative(int depth, out int bestFIndex, out int altF)
    {
        int levelOffset = depth * BoardState.ChildBoardsCount;
        int localBestIdx = 0;
        int bestF = _nodesBuffer[levelOffset].F;
        altF = int.MaxValue;

        for (int i = 1; i < BoardState.ChildBoardsCount; i++)
        {
            int currentF = _nodesBuffer[levelOffset + i].F;
            if (currentF < bestF)
            {
                altF = bestF;
                bestF = currentF;
                localBestIdx = i;
            }
            else if (currentF < altF)
            {
                altF = currentF;
            }
        }

        bestFIndex = levelOffset + localBestIdx;
    }
}