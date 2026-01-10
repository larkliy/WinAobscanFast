using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinAobscanFast.Core.Models;

namespace WinAobscanFast.Utils;

public static class RegionChunker
{
    public static List<MemoryRange> CreateMemoryChunks(List<MemoryRange> ranges, int patternLength)
    {
        const nint chunkSize = 256 * 1024;

        if (patternLength >= chunkSize)
            throw new ArgumentException("Pattern length cannot exceed chunk size");

        var result = new List<MemoryRange>(ranges.Count * 5);
        int overlap = patternLength - 1;

        var span = CollectionsMarshal.AsSpan(ranges);
        ref var rangeRef = ref MemoryMarshal.GetReference(span);
        nuint len = (nuint)span.Length;

        for (nuint i = 0; i < len; i++)
        {
            ref readonly var range = ref Unsafe.Add(ref rangeRef, i);

            nint currentPtr = range.BaseAddress;
            nint remaining = range.Size;

            while (remaining > 0)
            {
                nint sizeToRead = remaining > chunkSize ? chunkSize : remaining;

                if (sizeToRead < patternLength)
                    break;

                result.Add(new MemoryRange(currentPtr, sizeToRead));

                if (sizeToRead == remaining)
                    break;

                nint step = sizeToRead - overlap;

                currentPtr += step;
                remaining -= step;
            }
        }

        return result;
    }

}
