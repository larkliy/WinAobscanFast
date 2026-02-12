using System.Runtime.InteropServices;
using AobscanFast.Core.Models;

namespace AobscanFast.Utils;

public static class RegionUtils
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

    public static List<MemoryRange> MergeRegions(List<MemoryRange> regions)
    {
        if (regions.Count == 0)
            return regions;

        var result = new List<MemoryRange>(regions.Count);
        var span = CollectionsMarshal.AsSpan(regions);

        MemoryRange current = span[0];

        for (int i = 1; i < span.Length; i++)
        {
            ref readonly var next = ref span[i];

            if (current.BaseAddress + current.Size == next.BaseAddress)
            {
                current.Size += next.Size;
            }
            else
            {
                result.Add(current);

                current = next;
            }
        }

        result.Add(current);

        return result;
    }
}
