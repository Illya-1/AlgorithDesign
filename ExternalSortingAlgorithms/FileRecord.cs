using System.Runtime.CompilerServices;

namespace ExternalSortingAlgorithms;

public readonly struct FileRecord : IComparable<FileRecord>
{
    public int Key { get; }
    public DatePayload Date { get; }

    public FileRecord(int key, DatePayload date)
    {
        Key = key;
        Date = date;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(FileRecord other) => Key.CompareTo(other.Key);
}

[InlineArray(32)]
public struct DatePayload
{
    private byte _element0;
}