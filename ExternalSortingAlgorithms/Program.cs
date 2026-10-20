using ExternalSortingAlgorithms.Algorithms;
using ExternalSortingAlgorithms.Benchmarks;
using ExternalSortingAlgorithms.Util;

namespace ExternalSortingAlgorithms;

static class Program
{
    static void Main(string[] args)
    {
        try
        {
            Benchmarks();
        }
        catch (OutOfMemoryException ex)
        {
            long totalMemMb = GC.GetTotalMemory(false) / (1024 * 1024);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Managed Heap before crash: {totalMemMb} MB / 300 MB limit");
            Console.WriteLine(ex.ToString());
            Console.ResetColor();
            throw;
        }
        //Benchmarks();
        //Validate();
    }

    private static void Benchmarks()
    {
        string outputHtmlPath = @"C:\Users\ilyap\Downloads\test_table.html";

        Func<IAlgorithm>[] factories =
        [
            () => new NaturalMergeSort<FileRecord>(4 * (int)Units.MB),
            // () => new ModifiedNaturalMergeSort<FileRecord>(50 * (int)Units.MB, 4 * (int)Units.MB),
            // () => new ChatGptLsd(),
            // () => new GeminiAlgorithm(),
            // () => new GrkoAlgorithm()
        ];

        long[] testSizes =
        [
            // 10 * Units.MB,
            // 100 * Units.MB,
            // 250 * Units.MB,
            // 500 * Units.MB,
            1 * Units.GB,
            // 2 * Units.GB,
            // 4 * Units.GB
        ];

        BenchmarkRunner.RunAndExportHtml(factories, testSizes, outputHtmlPath);
    }

    private static void Validate()
    {
        IAlgorithm[] algorithmsToValidate =
        [
            // Сюда подставляешь свои экземпляры, например:
            new ChatGptLsd(),
            new GeminiAlgorithm(),
            new GrkoAlgorithm()
        ];

        const int validationSizeMb = 10;
        string tempDir = Path.Combine(Path.GetTempPath(), $"sort_val_{Random.Shared.Next():x8}");
        string sourceFile = Path.Combine(tempDir, "source.bin");

        try
        {
            Directory.CreateDirectory(tempDir);
            Console.WriteLine($"Generating validation dataset: {validationSizeMb} MB...");
            DataGenerator.GenerateFile(sourceFile, validationSizeMb);

            for (int i = 0; i < algorithmsToValidate.Length; i++)
            {
                var algorithm = algorithmsToValidate[i];
                string testFile = Path.Combine(tempDir, $"test_{i}.bin");

                File.Copy(sourceFile, testFile, overwrite: true);

                Console.Write($"Validating [{algorithm.Name}]... ");
                algorithm.Run(testFile);

                var result = SortValidator.Validate<FileRecord>(testFile);
                if (result.IsSorted)
                {
                    Console.WriteLine($"PASSED (Records verified: {result.TotalRecords:N0})");
                }
                else
                {
                    Console.WriteLine($"FAILED! Error: {result.ErrorMessage}");
                }

                if (File.Exists(testFile))
                {
                    File.Delete(testFile);
                }
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}