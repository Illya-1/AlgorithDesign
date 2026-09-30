using System.Runtime.CompilerServices;
using ExternalSortingAlgorithms.IO.Interfaces;

namespace ExternalSortingAlgorithms.Algorithms;

public class ModifiedNaturalMergeSort<T> : NaturalMergeSort<T>
    where T : struct, IComparable<T>
{
    private readonly int _memoryBufferSize;
    private readonly T[] _memoryBuffer;

    public override string Name => "Modified Natural Merge Sort";

    public ModifiedNaturalMergeSort(int memoryBufferSize, int ioBufferSize)
        : base(ioBufferSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(memoryBufferSize);

        _memoryBufferSize = memoryBufferSize;
        int maxElements = memoryBufferSize / Unsafe.SizeOf<T>();
        _memoryBuffer = new T[maxElements];
    }

    protected override bool OnBeforeSort()
    {
        long runCount = SortChunksToFiles();

        if (runCount <= 1)
        {
            return false;
        }

        Compose();
        return true;
    }

    private long SortChunksToFiles()
    {
        long runCount = 0;
        bool writeToB = true;

        using var reader = GetIterator(FileRole.A);
        using var writerB = GetWriter(FileRole.B);
        using var writerC = GetWriter(FileRole.C);

        while (reader.MoveNext())
        {
            int loadedCount = FillMemoryBuffer(reader);

            Span<T> runSpan = _memoryBuffer.AsSpan(0, loadedCount);
            runSpan.Sort();

            var currentWriter = writeToB ? writerB : writerC;
            for (int i = 0; i < loadedCount; i++)
            {
                currentWriter.Write(runSpan[i]);
            }

            runCount++;
            writeToB = !writeToB;
        }

        return runCount;
    }

    private int FillMemoryBuffer(IItemIterator<T> reader)
    {
        _memoryBuffer[0] = reader.Current;
        int loadedCount = 1;

        while (loadedCount < _memoryBuffer.Length && reader.MoveNext())
        {
            _memoryBuffer[loadedCount] = reader.Current;
            loadedCount++;
        }

        return loadedCount;
    }
}