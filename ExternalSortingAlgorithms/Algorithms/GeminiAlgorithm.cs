using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ExternalSortingAlgorithms.Algorithms;

public sealed class GeminiAlgorithm : IAlgorithm
{
    public string Name => "Gemini Sort";

    // Розмір однієї структури: 4 байти (Key) + 32 байти (DatePayload) = 36 байтів
    private static readonly int RecordSize = Unsafe.SizeOf<FileRecord>();

    // 100 МБ пам'яті під робочий масив для сортування в ОП (значно менше ліміту 300 МБ)
    private const int MemoryLimitBytes = 100 * 1024 * 1024;
    private static readonly int MaxChunkRecords = MemoryLimitBytes / 36; // ~2.9 млн записів
    private const int IoBufferSize = 64 * 1024; // 64 КБ для буферизації потоків

    public void Run(string filepath)
    {
        if (!File.Exists(filepath))
            throw new FileNotFoundException("Input file does not exist", filepath);

        var fileInfo = new FileInfo(filepath);
        long totalBytes = fileInfo.Length;

        if (totalBytes <= RecordSize)
            return;

        if (totalBytes % RecordSize != 0)
            throw new InvalidDataException("Розмір файлу не кратний розміру FileRecord (36 байт).");

        long totalRecords = totalBytes / RecordSize;
        SortFile(filepath, totalRecords);
    }

    private void SortFile(string filepath, long totalRecords)
    {
        // Базовий випадок: файл вміщується у виділену пам'ять
        if (totalRecords <= MaxChunkRecords)
        {
            SortChunkInPlace(filepath, 0, (int)totalRecords);
            return;
        }

        // Крок 1. Знаходження мінімального та максимального ключів
        int minKey = int.MaxValue;
        int maxKey = int.MinValue;

        using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read, IoBufferSize))
        {
            var buffer = new byte[IoBufferSize - (IoBufferSize % RecordSize)];
            var recordSpan = MemoryMarshal.Cast<byte, FileRecord>(buffer.AsSpan());

            int bytesRead;
            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                int count = bytesRead / RecordSize;
                for (int i = 0; i < count; i++)
                {
                    int key = recordSpan[i].Key;
                    if (key < minKey) minKey = key;
                    if (key > maxKey) maxKey = key;
                }
            }
        }

        if (minKey == maxKey)
            return; // Усі елементи однакові, сортування не потрібне

        // Крок 2. Розрахунок кількості кошиків та підготовка тимчасових файлів
        int numBuckets = (int)Math.Min(128, (totalRecords + MaxChunkRecords - 1) / MaxChunkRecords * 2);
        if (numBuckets < 2) numBuckets = 2;

        string tempDir = Path.Combine(Path.GetDirectoryName(filepath) ?? string.Empty, $"sort_temp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        string[] bucketPaths = new string[numBuckets];
        FileStream[] bucketStreams = new FileStream[numBuckets];
        long[] bucketCounts = new long[numBuckets];

        for (int i = 0; i < numBuckets; i++)
        {
            bucketPaths[i] = Path.Combine(tempDir, $"bucket_{i}.tmp");
            bucketStreams[i] = new FileStream(bucketPaths[i], FileMode.Create, FileAccess.Write, FileShare.None, IoBufferSize);
        }

        // Крок 3. Розподіл записів по кошиках
        long range = (long)maxKey - minKey + 1;
        try
        {
            using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read, IoBufferSize))
            {
                var buffer = new byte[IoBufferSize - (IoBufferSize % RecordSize)];
                var recordSpan = MemoryMarshal.Cast<byte, FileRecord>(buffer.AsSpan());

                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    int count = bytesRead / RecordSize;
                    for (int i = 0; i < count; i++)
                    {
                        var record = recordSpan[i];
                        int bucketIndex = (int)(((long)record.Key - minKey) * numBuckets / range);
                        if (bucketIndex >= numBuckets) bucketIndex = numBuckets - 1;

                        ReadOnlySpan<byte> bytesToWrite = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref record, 1));
                        bucketStreams[bucketIndex].Write(bytesToWrite);
                        bucketCounts[bucketIndex]++;
                    }
                }
            }
        }
        finally
        {
            for (int i = 0; i < numBuckets; i++)
            {
                bucketStreams[i].Dispose();
            }
        }

        // Крок 4. Рекурсивне/блочне сортування кожного кошика
        for (int i = 0; i < numBuckets; i++)
        {
            if (bucketCounts[i] > 0)
            {
                SortFile(bucketPaths[i], bucketCounts[i]);
            }
        }

        // Крок 5. Конкатенація відсортованих кошиків у вихідний файл
        using (var outputStream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None, IoBufferSize))
        {
            var buffer = new byte[IoBufferSize];
            for (int i = 0; i < numBuckets; i++)
            {
                if (bucketCounts[i] == 0) continue;

                using (var bucketReadStream = new FileStream(bucketPaths[i], FileMode.Open, FileAccess.Read, FileShare.Read, IoBufferSize))
                {
                    int bytesRead;
                    while ((bytesRead = bucketReadStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        outputStream.Write(buffer, 0, bytesRead);
                    }
                }
            }
        }

        // Очищення тимчасової директорії
        try
        {
            Directory.Delete(tempDir, recursive: true);
        }
        catch
        {
            // Ігноруємо затримки блокування ОС при видаленні тимчасової папки
        }
    }

    private static void SortChunkInPlace(string filepath, long offsetRecords, int recordCount)
    {
        if (recordCount <= 1) return;

        var array = ArrayPool<FileRecord>.Shared.Rent(recordCount);
        try
        {
            var span = array.AsSpan(0, recordCount);
            var byteSpan = MemoryMarshal.AsBytes(span);

            using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, IoBufferSize))
            {
                fs.Seek(offsetRecords * RecordSize, SeekOrigin.Begin);
                int totalRead = 0;
                while (totalRead < byteSpan.Length)
                {
                    int read = fs.Read(byteSpan.Slice(totalRead));
                    if (read == 0) break;
                    totalRead += read;
                }

                span.Sort();

                fs.Seek(offsetRecords * RecordSize, SeekOrigin.Begin);
                fs.Write(byteSpan);
            }
        }
        finally
        {
            ArrayPool<FileRecord>.Shared.Return(array);
        }
    }
}