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
        nint overlap = patternLength - 1;

        foreach (ref readonly var range in CollectionsMarshal.AsSpan(ranges))
        {
            nint ptr = range.BaseAddress;
            nint remaining = range.Size;

            while (remaining >= patternLength)
            {
                nint size = Math.Min(remaining, chunkSize);

                result.Add(new MemoryRange(ptr, size));

                if (size == remaining)
                    break;

                nint step = size - overlap;
                ptr += step;
                remaining -= step;
            }
        }

        return result;
    }

}
