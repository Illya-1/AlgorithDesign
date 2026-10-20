namespace SearchAlgorithms.Algorithms.Data;

public readonly record struct SearchResult(
    bool Success,
    BoardState InitialState,
    BoardState SolutionState,
    long Iterations,
    long DeadEnds,
    long NodesGenerated,
    long MaxNodesInMemory,
    TimeSpan Elapsed
);