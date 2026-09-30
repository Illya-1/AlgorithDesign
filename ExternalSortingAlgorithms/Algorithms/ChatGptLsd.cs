namespace ExternalSortingAlgorithms.Algorithms;

public sealed class ChatGptLsd : IAlgorithm
{
    public string Name => "ChatGpt Lsd Sort";

    // FileRecord:
    // int Key           = 4 bytes
    // DatePayload       = 32 bytes
    // Total             = 36 bytes
    private const int RecordSize = 36;

    private const int Radix = 256;

    // 64 KB buffer for every bucket.
    // 256 * 64 KB = 16 MB.
    private const int BucketBufferSize = 64 * 1024;

    // Input buffer.
    private const int InputBufferSize = 1024 * 1024;

    public void Run(string filepath)
    {
        if (string.IsNullOrWhiteSpace(filepath))
            throw new ArgumentException("File path is empty.", nameof(filepath));

        if (!File.Exists(filepath))
            throw new FileNotFoundException("Input file was not found.", filepath);

        FileInfo fileInfo = new FileInfo(filepath);

        if (fileInfo.Length % RecordSize != 0)
        {
            throw new InvalidDataException(
                $"Invalid file size. Expected a multiple of {RecordSize} bytes.");
        }

        if (fileInfo.Length == 0)
            return;

        string? directory = Path.GetDirectoryName(Path.GetFullPath(filepath));

        if (directory == null)
            directory = Directory.GetCurrentDirectory();

        string tempDirectory = Path.Combine(
            directory,
            ".external_radix_" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempDirectory);

        try
        {
            // Initial input consists of one file.
            string[] currentFiles = new[] { filepath };

            // int consists of 4 bytes.
            // LSD => least significant byte first.
            for (int pass = 0; pass < 4; pass++)
            {
                string[] nextFiles = DistributeByByte(
                    currentFiles,
                    tempDirectory,
                    pass);

                // Previous temporary files are no longer needed.
                if (currentFiles != null &&
                    currentFiles.Length > 1)
                {
                    DeleteFiles(currentFiles);
                }

                currentFiles = nextFiles;
            }

            // After the fourth pass the buckets are already
            // globally ordered by key.
            WriteResultToOriginalFile(
                currentFiles,
                filepath);

            DeleteFiles(currentFiles);
        }
        finally
        {
            // Delete everything left in the temporary directory.
            try
            {
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, true);
            }
            catch
            {
                // Do not hide the main exception because of cleanup failure.
            }
        }
    }

    private static string[] DistributeByByte(
        string[] inputFiles,
        string tempDirectory,
        int byteIndex)
    {
        string passDirectory = Path.Combine(
            tempDirectory,
            "pass_" + byteIndex);

        Directory.CreateDirectory(passDirectory);

        string[] bucketPaths = new string[Radix];

        for (int i = 0; i < Radix; i++)
        {
            bucketPaths[i] = Path.Combine(
                passDirectory,
                $"bucket_{i:D3}.bin");
        }

        FileStream[] outputs = new FileStream[Radix];

        // Each bucket has a 64 KB managed buffer.
        // 256 * 64 KB = 16 MB.
        byte[][] buffers = new byte[Radix][];
        int[] bufferLengths = new int[Radix];

        try
        {
            for (int i = 0; i < Radix; i++)
            {
                outputs[i] = new FileStream(
                    bucketPaths[i],
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    64 * 1024,
                    FileOptions.SequentialScan);

                buffers[i] = new byte[BucketBufferSize];
                bufferLengths[i] = 0;
            }

            byte[] record = new byte[RecordSize];
            byte[] inputBuffer = new byte[InputBufferSize];

            for (int fileIndex = 0;
                 fileIndex < inputFiles.Length;
                 fileIndex++)
            {
                ProcessInputFile(
                    inputFiles[fileIndex],
                    outputs,
                    buffers,
                    bufferLengths,
                    record,
                    inputBuffer,
                    byteIndex);
            }

            // Flush all bucket buffers.
            for (int bucket = 0; bucket < Radix; bucket++)
            {
                FlushBucket(
                    outputs[bucket],
                    buffers[bucket],
                    ref bufferLengths[bucket]);
            }
        }
        finally
        {
            for (int i = 0; i < Radix; i++)
            {
                outputs[i]?.Dispose();
            }
        }

        // We don't return empty buckets.
        // This reduces the number of files processed
        // on the next radix pass.
        int nonEmptyCount = 0;

        for (int i = 0; i < Radix; i++)
        {
            if (new FileInfo(bucketPaths[i]).Length > 0)
                nonEmptyCount++;
        }

        string[] result = new string[nonEmptyCount];

        int position = 0;

        for (int i = 0; i < Radix; i++)
        {
            if (new FileInfo(bucketPaths[i]).Length > 0)
            {
                result[position++] = bucketPaths[i];
            }
            else
            {
                File.Delete(bucketPaths[i]);
            }
        }

        return result;
    }

    private static void ProcessInputFile(
        string inputPath,
        FileStream[] outputs,
        byte[][] buffers,
        int[] bufferLengths,
        byte[] record,
        byte[] inputBuffer,
        int byteIndex)
    {
        using FileStream input = new FileStream(
            inputPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            InputBufferSize,
            FileOptions.SequentialScan);

        int bytesInBuffer = 0;
        int bufferPosition = 0;

        while (true)
        {
            // Make sure that we have a complete record available.
            if (bytesInBuffer - bufferPosition < RecordSize)
            {
                int remaining = bytesInBuffer - bufferPosition;

                if (remaining > 0)
                {
                    Buffer.BlockCopy(
                        inputBuffer,
                        bufferPosition,
                        inputBuffer,
                        0,
                        remaining);
                }

                bufferPosition = 0;
                bytesInBuffer = remaining;

                int read = input.Read(
                    inputBuffer,
                    bytesInBuffer,
                    inputBuffer.Length - bytesInBuffer);

                if (read == 0)
                    break;

                bytesInBuffer += read;
            }

            Buffer.BlockCopy(
                inputBuffer,
                bufferPosition,
                record,
                0,
                RecordSize);

            bufferPosition += RecordSize;

            int key = BitConverter.ToInt32(record, 0);

            // Transform signed int ordering into unsigned ordering.
            uint transformedKey =
                unchecked((uint)(key ^ int.MinValue));

            int bucket =
                (int)((transformedKey >> (byteIndex * 8)) & 0xFF);

            AddToBucket(
                outputs[bucket],
                buffers[bucket],
                ref bufferLengths[bucket],
                record);
        }

        if (bytesInBuffer != bufferPosition)
        {
            throw new InvalidDataException(
                $"File '{inputPath}' contains an incomplete record.");
        }
    }

    private static void AddToBucket(
        FileStream output,
        byte[] buffer,
        ref int bufferLength,
        byte[] record)
    {
        if (bufferLength + RecordSize > buffer.Length)
        {
            FlushBucket(
                output,
                buffer,
                ref bufferLength);
        }

        Buffer.BlockCopy(
            record,
            0,
            buffer,
            bufferLength,
            RecordSize);

        bufferLength += RecordSize;
    }

    private static void FlushBucket(
        FileStream output,
        byte[] buffer,
        ref int bufferLength)
    {
        if (bufferLength == 0)
            return;

        output.Write(
            buffer,
            0,
            bufferLength);

        bufferLength = 0;
    }

    private static void WriteResultToOriginalFile(
        string[] sortedFiles,
        string originalFile)
    {
        string resultFile = originalFile + ".radix_result";

        try
        {
            using (FileStream output = new FileStream(
                resultFile,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                InputBufferSize,
                FileOptions.SequentialScan))
            {
                byte[] buffer = new byte[InputBufferSize];

                foreach (string file in sortedFiles)
                {
                    using FileStream input = new FileStream(
                        file,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        InputBufferSize,
                        FileOptions.SequentialScan);

                    int read;

                    while ((read = input.Read(
                               buffer,
                               0,
                               buffer.Length)) > 0)
                    {
                        output.Write(buffer, 0, read);
                    }
                }
            }

            // Replace original file with sorted file.
            File.Move(
                resultFile,
                originalFile,
                true);
        }
        finally
        {
            if (File.Exists(resultFile))
            {
                try
                {
                    File.Delete(resultFile);
                }
                catch
                {
                    // Ignore cleanup failure.
                }
            }
        }
    }

    private static void DeleteFiles(string[] files)
    {
        foreach (string file in files)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch
            {
                // Cleanup should not hide the main algorithm result.
            }
        }
    }
}