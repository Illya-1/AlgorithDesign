namespace ExternalSortingAlgorithms.IO.Interfaces;

public interface IItemIterator<T> : IDisposable
{
    bool MoveNext();
    
    T Current { get; }
    
    T Previous { get; }

    /// <summary>
    /// Result of last use of bool MoveNext()
    /// </summary>
    bool HasCurrent { get; }
}