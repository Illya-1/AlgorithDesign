using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ExternalSortingAlgorithms.IO.Interfaces;

namespace ExternalSortingAlgorithms.IO;

public sealed class FileItemIterator<T> : IItemIterator<T> where T : struct
{
    private readonly FileStream _stream;
    private readonly T[] _buffer;
    private readonly int _itemSizeBytes;

    private int _bufferLength;
    private int _bufferIndex;
    private bool _isDisposed;

    public T Current { get; private set; }
    public T Previous { get; private set; }
    public bool HasCurrent { get; private set; }

    public FileItemIterator(string path, int bufferCapacity)
        : this(path, new T[bufferCapacity])
    {
    }

    public FileItemIterator(string path, T[] buffer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfZero(buffer.Length);

        _itemSizeBytes = Unsafe.SizeOf<T>();

        _stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 0,
            FileOptions.SequentialScan);

        _buffer = buffer;
        _bufferLength = 0;
        _bufferIndex = 0;

        Current = default;
        Previous = default;
        HasCurrent = false;
    }

    public bool MoveNext()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_bufferIndex >= _bufferLength && !ReadNextBlock())
        {
            HasCurrent = false;
            return false;
        }

        Previous = Current;
        Current = _buffer[_bufferIndex++];
        HasCurrent = true;
        return true;
    }

    private bool ReadNextBlock()
    {
        Span<byte> byteSpan = MemoryMarshal.AsBytes(_buffer.AsSpan());
        int totalBytesRead = 0;

        while (totalBytesRead < byteSpan.Length)
        {
            int bytesRead = _stream.Read(byteSpan[totalBytesRead..]);
            if (bytesRead == 0)
            {
                break;
            }

            totalBytesRead += bytesRead;
        }

        if (totalBytesRead == 0)
        {
            _bufferLength = 0;
            _bufferIndex = 0;
            return false;
        }

        _bufferLength = totalBytesRead / _itemSizeBytes;
        _bufferIndex = 0;

        return _bufferLength > 0;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _stream.Dispose();
        _isDisposed = true;
        HasCurrent = false;
    }
}