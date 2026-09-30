using System.Diagnostics;
using System.Xml.Linq;
using ExternalSortingAlgorithms.Algorithms;
using ExternalSortingAlgorithms.Util;

namespace ExternalSortingAlgorithms.Benchmarks;

public static class BenchmarkRunner
{
    public static void RunAndExportHtml(
        Func<IAlgorithm>[] algorithmFactories,
        long[] fileSizesBytes,
        string outputHtmlPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputHtmlPath);

        var results = ExecuteBenchmarks(algorithmFactories, fileSizesBytes);
        ExportHtmlTable(results, fileSizesBytes, outputHtmlPath);
    }

    public static BenchmarkResult[] Run(
        Func<IAlgorithm>[] algorithmFactories,
        long[] fileSizesBytes)
    {
        return ExecuteBenchmarks(algorithmFactories, fileSizesBytes);
    }

    private static BenchmarkResult[] ExecuteBenchmarks(
        Func<IAlgorithm>[] algorithmFactories,
        long[] fileSizesBytes)
    {
        ArgumentNullException.ThrowIfNull(algorithmFactories);
        ArgumentNullException.ThrowIfNull(fileSizesBytes);

        string tempFilePath = Path.Combine(Path.GetTempPath(), $"bench_{Random.Shared.Next():x8}.bin");
        var results = new BenchmarkResult[algorithmFactories.Length];

        try
        {
            for (int i = 0; i < algorithmFactories.Length; i++)
            {
                var algorithm = algorithmFactories[i].Invoke();
                WarmupAlgorithm(algorithm);

                var durationsMs = new long[fileSizesBytes.Length];

                for (int j = 0; j < fileSizesBytes.Length; j++)
                {
                    long sizeBytes = fileSizesBytes[j];
                    DataGenerator.GenerateFile(tempFilePath, sizeBytes);
                    
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    long startTimestamp = Stopwatch.GetTimestamp();
                    algorithm.Run(tempFilePath);
                    durationsMs[j] = (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

                    int powerOfTwo = (int)long.Log2(sizeBytes);
                    Console.WriteLine($"[{algorithm.Name}] Size: 2^{powerOfTwo} B ({durationsMs[j]} ms)");
                }

                results[i] = new BenchmarkResult(algorithm.Name, durationsMs);
            }
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }

        return results;
    }

    private static void ExportHtmlTable(
        BenchmarkResult[] results,
        long[] fileSizesBytes,
        string outputPath)
    {
        const string cellStyle = "border: 1px solid black; padding: 6px;";

        var headerRow = new XElement("tr", 
            new XElement("th", new XAttribute("style", cellStyle), "Вх. об'єм даних"));

        for (int i = 0; i < results.Length; i++)
        {
            headerRow.Add(new XElement("th", 
                new XAttribute("style", cellStyle), 
                $"{results[i].AlgorithmName} (с)"));
        }

        var tbody = new XElement("tbody");

        for (int row = 0; row < fileSizesBytes.Length; row++)
        {
            long sizeBytes = fileSizesBytes[row];
            string sizeLabel = FormatSize(sizeBytes);

            var tr = new XElement("tr", 
                new XElement("td", new XAttribute("style", cellStyle), sizeLabel));

            for (int col = 0; col < results.Length; col++)
            {
                double seconds = results[col].ElapsedMilliseconds[row] / 1000.0;
                tr.Add(new XElement("td", new XAttribute("style", cellStyle), seconds.ToString("F2")));
            }

            tbody.Add(tr);
        }

        var table = new XElement("table",
            new XAttribute("style", "border-collapse: collapse; text-align: center; font-family: Arial, sans-serif;"),
            new XElement("thead", headerRow),
            tbody
        );

        table.Save(outputPath, SaveOptions.OmitDuplicateNamespaces);
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= Units.GB) return $"{bytes / Units.GB} Гб";
        if (bytes >= Units.MB) return $"{bytes / Units.MB} Мб";
        if (bytes >= Units.KB) return $"{bytes / Units.KB} Кб";
        return $"{bytes} Б";
    }

    private static void WarmupAlgorithm(IAlgorithm algorithm)
    {
        string path = Path.Combine(Path.GetTempPath(), $"warmup_{Random.Shared.Next():x8}.bin");
        try
        {
            DataGenerator.GenerateFile(path, 1 * Units.MB);
            algorithm.Run(path);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}