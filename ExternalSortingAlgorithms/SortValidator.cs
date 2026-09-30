using ExternalSortingAlgorithms.IO;
using ExternalSortingAlgorithms.Util;

namespace ExternalSortingAlgorithms;

public static class SortValidator
{
    public readonly record struct ValidationResult(
        bool IsSorted,
        long TotalRecords,
        long ErrorIndex = -1,
        string? ErrorMessage = null);

    public static ValidationResult Validate<T>(string filePath)
        where T : struct, IComparable<T>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        using var iterator = new FileItemIterator<T>(filePath, 4 * (int)Units.MB);

        if (!iterator.MoveNext())
        {
            return new ValidationResult(IsSorted: true, TotalRecords: 0);
        }

        long count = 1;

        while (iterator.MoveNext())
        {
            count++;

            if (iterator.Previous.CompareTo(iterator.Current) > 0)
            {
                return new ValidationResult(
                    IsSorted: false,
                    TotalRecords: count,
                    ErrorIndex: count - 1,
                    ErrorMessage: $"Order violation at index {count - 1}: [{iterator.Previous}] > [{iterator.Current}]");
            }
        }

        return new ValidationResult(IsSorted: true, TotalRecords: count);
    }
}