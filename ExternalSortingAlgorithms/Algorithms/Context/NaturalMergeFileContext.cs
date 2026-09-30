namespace ExternalSortingAlgorithms.Algorithms.Context;

public sealed class NaturalMergeFileContext
{
    public string PathA { get; }
    public string PathB { get; }
    public string PathC { get; }

    public NaturalMergeFileContext(string initialFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(initialFilePath);

        PathA = Path.GetFullPath(initialFilePath);

        if (!File.Exists(PathA))
        {
            throw new FileNotFoundException("Initial source file not found.", PathA);
        }

        string directory = Path.GetDirectoryName(PathA)!;
        string fileName = Path.GetFileNameWithoutExtension(PathA);
        string uniqueTag = Random.Shared.Next().ToString("x8");

        PathB = Path.Combine(directory, $"{fileName}.tmp_{uniqueTag}_b.bin");
        PathC = Path.Combine(directory, $"{fileName}.tmp_{uniqueTag}_c.bin");
    }
}