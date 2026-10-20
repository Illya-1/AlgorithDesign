using System.Runtime.CompilerServices;

namespace SearchAlgorithms;

public static class Units
{
    public const long KB = 1024;
    public const long MB = 1024 * KB;
    public const long GB = 1024 * MB;

    public static int ElementsCount<T>(int byteSize) where T : struct
    {
        return byteSize / Unsafe.SizeOf<T>();
    }
    
    public static int ElementsCount<T>(long byteSize) where T : struct
    {
        return (int)(byteSize / Unsafe.SizeOf<T>());
    }
}