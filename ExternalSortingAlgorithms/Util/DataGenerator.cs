using System.Runtime.CompilerServices;
using ExternalSortingAlgorithms.IO;

namespace ExternalSortingAlgorithms.Util;

public static class DataGenerator
{
    private static readonly int InternalBufferCapacity = (int)(8 * Units.MB / Unsafe.SizeOf<FileRecord>());
    private static readonly byte[] Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"u8.ToArray();

    public static void GenerateFile(string filePath, long totalSize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalSize);

        long recordsCount = totalSize / Unsafe.SizeOf<FileRecord>();

        using var writer = new FileItemWriter<FileRecord>(filePath, InternalBufferCapacity);

        DatePayload payload = default;

        payload[0] = (byte)'-';
        payload[21] = (byte)'-';

        payload[22] = (byte)'2';
        payload[23] = (byte)'0';
        payload[26] = (byte)'-';
        payload[29] = (byte)'-';

        ReadOnlySpan<byte> alphabet = Alphabet;

        for (long i = 0; i < recordsCount; i++)
        {
            int key = Random.Shared.Next(int.MinValue, int.MaxValue);

            for (int t = 0; t < 20; t++)
            {
                payload[1 + t] = alphabet[Random.Shared.Next(alphabet.Length)];
            }

            int yearSuffix = Random.Shared.Next(0, 30);
            int month = Random.Shared.Next(1, 13);
            int day = Random.Shared.Next(1, 29);

            payload[24] = (byte)('0' + (yearSuffix / 10));
            payload[25] = (byte)('0' + (yearSuffix % 10));

            payload[27] = (byte)('0' + (month / 10));
            payload[28] = (byte)('0' + (month % 10));

            payload[30] = (byte)('0' + (day / 10));
            payload[31] = (byte)('0' + (day % 10));

            writer.Write(new FileRecord(key, payload));
        }
    }
}