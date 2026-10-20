using SearchAlgorithms.Algorithms;
using SearchAlgorithms.Algorithms.Data;
using SearchAlgorithms.HtmlBuilders;

namespace SearchAlgorithms;

public class BenchmarkRunner(int iterations, string outputPath)
{
    private readonly BoardState[] _boardStates = GenBoardStates(iterations);
    
    public void Run(ISearchAlgorithm algorithm)
    {
        Console.WriteLine($"Run on {algorithm.Name}");
        SearchResult[] results = new SearchResult[iterations];
        for (int i = 0; i < iterations; i++)
        {
            Console.WriteLine($"Iter = {i}");
            results[i] = algorithm.Solve(_boardStates[i]);
        }

        var table = HtmlTableBuilder.Create(iterations)
            .AddColumn("№", i => $"{i + 1}")
            .AddColumn("Початкові стани", i => results[i].InitialState)
            .AddColumn("Ітерації", i => results[i].Iterations)
            .AddColumn("К-сть гл. кутів", i => results[i].DeadEnds)
            .AddColumn("Всього вузлів", i => results[i].NodesGenerated)
            .AddColumn("Всього вузлів у пам’яті", i => results[i].MaxNodesInMemory);
        HtmlDocumentBuilder.Create()
            .AddTable(table)
            .AddParagraph($"Середня кількість кроків для досягення розв'язку: {(double)results.Sum(res => res.Iterations) / iterations}")
            .AddParagraph($"Середня кількість випадків, коли алгоритм потрапляв в глухий кут: {(double)results.Sum(res => res.DeadEnds) / iterations}")
            .AddParagraph($"Середня кількість згенерованих станів під час пошуку: {(double)results.Sum(res => res.NodesGenerated) / iterations}")
            .AddParagraph($"Середня кількість станів, що зберігаються в пам’яті під час роботи програми: {(double)results.Sum(res => res.MaxNodesInMemory) / iterations}")
            .Save($@"{outputPath}_{algorithm.Name}.html");
    }

    private static BoardState[] GenBoardStates(int iterations)
    {
        BoardState[] boardStates = new BoardState[iterations];
        for (int i = 0; i < iterations; i++)
        {
            boardStates[i] = BoardState.GenRandom();
        }

        return boardStates;
    }
}