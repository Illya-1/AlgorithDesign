using ExternalSortingAlgorithms.Algorithms.Context;
using ExternalSortingAlgorithms.IO;
using ExternalSortingAlgorithms.IO.Interfaces;
using ExternalSortingAlgorithms.Util;

namespace ExternalSortingAlgorithms.Algorithms;

public class NaturalMergeSort<T> : IAlgorithm
    where T : struct, IComparable<T>
{
    private readonly T[] _bufferA;
    private readonly T[] _bufferB;
    private readonly T[] _bufferC;

    private NaturalMergeFileContext _context = null!;

    public virtual string Name => "Natural Merge Sort";

    public NaturalMergeSort(int bufferSizeBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSizeBytes);

        int capacity = Units.ElementsCount<T>(bufferSizeBytes);
        _bufferA = new T[capacity];
        _bufferB = new T[capacity];
        _bufferC = new T[capacity];
    }

    public void Run(string filepath)
    {
        _context = new NaturalMergeFileContext(filepath);

        try
        {
            if (OnBeforeSort())
            {
                while (ReadAndSplit())
                {
                    Compose();
                }
            }

            File.Move(_context.PathB, filepath, overwrite: true);
        }
        finally
        {
            if (File.Exists(_context.PathB))
            {
                File.Delete(_context.PathB);
            }

            if (File.Exists(_context.PathC))
            {
                File.Delete(_context.PathC);
            }
        }
    }

    private bool ReadAndSplit()
    {
        using var fileA = GetIterator(FileRole.A);
        if (!fileA.MoveNext())
        {
            return false;
        }

        using var fileB = GetWriter(FileRole.B);
        using var fileC = GetWriter(FileRole.C);

        bool isFileB = true;
        bool changedSequence = false;

        fileB.Write(fileA.Current);

        while (fileA.MoveNext())
        {
            if (fileA.Previous.CompareTo(fileA.Current) > 0)
            {
                isFileB = !isFileB;
                changedSequence = true;
            }

            if (isFileB)
            {
                fileB.Write(fileA.Current);
            }
            else
            {
                fileC.Write(fileA.Current);
            }
        }

        return changedSequence;
    }

    protected void Compose()
    {
        using var fileA = GetWriter(FileRole.A);
        using var fileB = GetIterator(FileRole.B);
        using var fileC = GetIterator(FileRole.C);

        fileB.MoveNext();
        fileC.MoveNext();

        while (fileB.HasCurrent || fileC.HasCurrent)
        {
            if (!fileB.HasCurrent) { Drain(fileC, fileA); break; }
            if (!fileC.HasCurrent) { Drain(fileB, fileA); break; }

            bool runB = true;
            bool runC = true;

            while (runB || runC)
            {
                if (runB && runC)
                {
                    if (fileB.Current.CompareTo(fileC.Current) <= 0)
                    {
                        Step(fileB, fileA, ref runB);
                    }
                    else
                    {
                        Step(fileC, fileA, ref runC);
                    }
                }
                else if (runB)
                {
                    Step(fileB, fileA, ref runB);
                }
                else
                {
                    Step(fileC, fileA, ref runC);
                }
            }
        }
    }

    private static void Step(IItemIterator<T> file, IItemWriter<T> outA, ref bool isRunActive)
    {
        outA.Write(file.Current);

        if (!file.MoveNext())
        {
            isRunActive = false;
            return;
        }

        if (file.Current.CompareTo(file.Previous) < 0)
        {
            isRunActive = false;
        }
    }

    private static void Drain(IItemIterator<T> source, IItemWriter<T> target)
    {
        do
        {
            target.Write(source.Current);
        } while (source.MoveNext());
    }
    
    protected virtual bool OnBeforeSort()
    {
        return true;
    }
    
    protected IItemIterator<T> GetIterator(FileRole role) => role switch
    {
        FileRole.A => new FileItemIterator<T>(_context.PathA, _bufferA),
        FileRole.B => new FileItemIterator<T>(_context.PathB, _bufferB),
        FileRole.C => new FileItemIterator<T>(_context.PathC, _bufferC),
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };

    protected IItemWriter<T> GetWriter(FileRole role) => role switch
    {
        FileRole.A => new FileItemWriter<T>(_context.PathA, _bufferA),
        FileRole.B => new FileItemWriter<T>(_context.PathB, _bufferB),
        FileRole.C => new FileItemWriter<T>(_context.PathC, _bufferC),
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };
    
    
    protected enum FileRole
    {
        A,
        B,
        C
    }
}