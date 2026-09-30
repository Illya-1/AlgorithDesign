using System.Runtime.InteropServices;
using ExternalSortingAlgorithms.IO.Interfaces;

namespace ExternalSortingAlgorithms.IO;

public sealed class FileItemWriter<T> : IItemWriter<T> where T : struct
{
    private readonly FileStream _stream;
    private readonly T[] _buffer;
    private int _count;
    private bool _isDisposed;

    public FileItemWriter(string path, int bufferCapacity)
        : this(path, new T[bufferCapacity])
    {
    }

    public FileItemWriter(string path, T[] buffer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfZero(buffer.Length);

        _stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 0,
            FileOptions.SequentialScan);

        _buffer = buffer;
        _count = 0;
    }

    public void Write(T item)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_count == _buffer.Length)
        {
            FlushBuffer();
        }

        _buffer[_count++] = item;
    }

    private void FlushBuffer()
    {
        if (_count == 0)
        {
            return;
        }

        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(_buffer.AsSpan(0, _count));
        _stream.Write(bytes);
        _count = 0;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        try
        {
            FlushBuffer();
            _stream.Flush();
        }
        finally
        {
            _stream.Dispose();
            _isDisposed = true;
        }
    }
}