namespace ExternalSortingAlgorithms.IO.Interfaces;

public interface IItemWriter<T> : IDisposable
{
    void Write(T item);
}