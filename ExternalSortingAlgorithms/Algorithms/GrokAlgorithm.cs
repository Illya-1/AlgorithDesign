using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ExternalSortingAlgorithms.Algorithms;

/// <summary>
/// External sorting: Replacement Selection (run formation) + multi-way merge.
/// Not one of the four algorithms from the laboratory list.
/// Hard memory limit well under 300 MB.
/// </summary>
public sealed class GrkoAlgorithm : IAlgorithm
{
    public string Name => "Replacement Selection External Sort";

    // Peak ≈ 2 × this value  →  keep under 300 MB
    private const long MaxMemoryBytes = 120L * 1024 * 1024;

    private static readonly int RecordSize = Unsafe.SizeOf<FileRecord>();

    public void Run(string filepath)
    {
        if (string.IsNullOrEmpty(filepath))
            throw new ArgumentException("Path is null or empty.", nameof(filepath));
        if (!File.Exists(filepath))
            throw new FileNotFoundException("Input file not found.", filepath);

        long fileLen = new FileInfo(filepath).Length;
        if (fileLen == 0)
            return;
        if (fileLen % RecordSize != 0)
            throw new InvalidDataException(
                $"File length ({fileLen}) is not a multiple of record size ({RecordSize}).");

        long fileRecords = fileLen / RecordSize;
        long maxRecords  = MaxMemoryBytes / RecordSize;

        // ---------- Fast path: whole file fits in memory ----------
        if (fileRecords <= maxRecords)
        {
            var data = new FileRecord[fileRecords];
            using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read,
                                           FileShare.Read, 1 << 20, FileOptions.SequentialScan))
            {
                for (int i = 0; i < fileRecords; i++)
                {
                    if (!TryRead(fs, out data[i]))
                        throw new EndOfStreamException();
                }
            }

            Array.Sort(data);   // uses FileRecord.CompareTo (ascending by Key)

            using (var fs = new FileStream(filepath, FileMode.Create, FileAccess.Write,
                                           FileShare.None, 1 << 20, FileOptions.SequentialScan))
            {
                for (int i = 0; i < fileRecords; i++)
                    Write(fs, data[i]);
            }
            return;
        }

        // ---------- External path ----------
        int heapCapacity = (int)maxRecords;

        List<string> runPaths = GenerateRuns(filepath, heapCapacity);

        if (runPaths.Count == 1)
        {
            File.Delete(filepath);
            File.Move(runPaths[0], filepath);
        }
        else
        {
            string sortedTemp = MergeAllRuns(runPaths);
            File.Delete(filepath);
            File.Move(sortedTemp, filepath);
        }

        foreach (string p in runPaths)
        {
            try { if (File.Exists(p)) File.Delete(p); } catch { /* best-effort */ }
        }
    }

    // ------------------------------------------------------------------
    //  Run formation – Replacement Selection
    // ------------------------------------------------------------------

    private static List<string> GenerateRuns(string inputPath, int heapCapacity)
    {
        var runs = new List<string>();

        // Two fixed-size arrays – peak memory = 2 × heapCapacity × RecordSize
        var heap       = new FileRecord[heapCapacity];
        var nextBuffer = new FileRecord[heapCapacity];
        int heapSize   = 0;
        int nextCount  = 0;

        using var input = new FileStream(
            inputPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 1 << 20, FileOptions.SequentialScan);

        // Initial fill
        while (heapSize < heapCapacity && TryRead(input, out FileRecord rec))
            heap[heapSize++] = rec;

        if (heapSize == 0)
            return runs;

        BuildMinHeap(heap, heapSize);

        string currentRunPath = CreateTempFile();
        FileStream runOut = CreateWriteStream(currentRunPath);
        int lastKey = int.MinValue;

        try
        {
            while (true)
            {
                if (heapSize == 0)
                {
                    // Finish current run
                    runOut.Dispose();
                    runs.Add(currentRunPath);

                    if (nextCount == 0)
                        break;               // no more data

                    // Promote frozen records → new heap
                    Array.Copy(nextBuffer, 0, heap, 0, nextCount);
                    heapSize  = nextCount;
                    nextCount = 0;
                    BuildMinHeap(heap, heapSize);

                    currentRunPath = CreateTempFile();
                    runOut = CreateWriteStream(currentRunPath);
                    lastKey = int.MinValue;
                }

                // Extract-min
                FileRecord min = heap[0];
                heapSize--;
                if (heapSize > 0)
                {
                    heap[0] = heap[heapSize];
                    SiftDown(heap, heapSize, 0);
                }

                Write(runOut, min);
                lastKey = min.Key;

                // Next record from input
                if (TryRead(input, out FileRecord next))
                {
                    if (next.Key >= lastKey)
                    {
                        // Still belongs to current run
                        heap[heapSize] = next;
                        SiftUp(heap, heapSize);
                        heapSize++;
                    }
                    else
                    {
                        // Freeze for next run
                        nextBuffer[nextCount++] = next;
                    }
                }
            }
        }
        finally
        {
            runOut?.Dispose();
        }

        return runs;
    }

    // ------------------------------------------------------------------
    //  Multi-way merge (multi-pass when needed)
    // ------------------------------------------------------------------

    private static string MergeAllRuns(List<string> runPaths)
    {
        const int MaxOpenFiles = 64;

        List<string> current = runPaths;

        while (current.Count > 1)
        {
            var nextLevel = new List<string>();
            for (int i = 0; i < current.Count; i += MaxOpenFiles)
            {
                int cnt = Math.Min(MaxOpenFiles, current.Count - i);
                var batch = current.GetRange(i, cnt);
                string merged = MergeBatch(batch);
                nextLevel.Add(merged);

                foreach (string p in batch)
                {
                    try { File.Delete(p); } catch { /* best-effort */ }
                }
            }
            current = nextLevel;
        }

        return current[0];
    }

    private static string MergeBatch(List<string> runs)
    {
        string outPath = CreateTempFile();
        var readers = new FileStream[runs.Count];
        var heads   = new FileRecord[runs.Count];

        // priority = Key, element = run index
        var heap = new PriorityQueue<int, int>();

        try
        {
            for (int i = 0; i < runs.Count; i++)
            {
                readers[i] = new FileStream(
                    runs[i], FileMode.Open, FileAccess.Read, FileShare.Read,
                    bufferSize: 1 << 20, FileOptions.SequentialScan);

                if (TryRead(readers[i], out heads[i]))
                    heap.Enqueue(i, heads[i].Key);
            }

            using var output = CreateWriteStream(outPath);

            while (heap.Count > 0)
            {
                int idx = heap.Dequeue();
                Write(output, heads[idx]);

                if (TryRead(readers[idx], out heads[idx]))
                    heap.Enqueue(idx, heads[idx].Key);
            }
        }
        finally
        {
            foreach (var r in readers)
                r?.Dispose();
        }

        return outPath;
    }

    // ------------------------------------------------------------------
    //  Binary min-heap (by Key)
    // ------------------------------------------------------------------

    private static void BuildMinHeap(FileRecord[] h, int size)
    {
        for (int i = (size >> 1) - 1; i >= 0; i--)
            SiftDown(h, size, i);
    }

    private static void SiftDown(FileRecord[] h, int size, int i)
    {
        while (true)
        {
            int smallest = i;
            int left  = (i << 1) + 1;
            int right = left + 1;

            if (left  < size && h[left].Key  < h[smallest].Key) smallest = left;
            if (right < size && h[right].Key < h[smallest].Key) smallest = right;

            if (smallest == i) break;

            (h[i], h[smallest]) = (h[smallest], h[i]);
            i = smallest;
        }
    }

    private static void SiftUp(FileRecord[] h, int i)
    {
        while (i > 0)
        {
            int parent = (i - 1) >> 1;
            if (h[i].Key >= h[parent].Key) break;
            (h[i], h[parent]) = (h[parent], h[i]);
            i = parent;
        }
    }

    // ------------------------------------------------------------------
    //  Binary I/O
    // ------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryRead(Stream stream, out FileRecord record)
    {
        Span<byte> buf = stackalloc byte[RecordSize];
        int read = stream.Read(buf);
        if (read == 0)
        {
            record = default;
            return false;
        }
        if (read != RecordSize)
            throw new EndOfStreamException("Unexpected end of file while reading a record.");
        record = MemoryMarshal.Read<FileRecord>(buf);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Write(Stream stream, in FileRecord record)
    {
        Span<byte> buf = stackalloc byte[RecordSize];
        MemoryMarshal.Write(buf, in record);
        stream.Write(buf);
    }

    private static FileStream CreateWriteStream(string path) =>
        new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None,
                       bufferSize: 1 << 20, FileOptions.SequentialScan);

    private static string CreateTempFile() =>
        Path.Combine(Path.GetTempPath(), "extsort_" + Guid.NewGuid().ToString("N") + ".tmp");
}