using System.Buffers;
using System.Globalization;

namespace AobscanFast.Core.Models.Pattern;

internal class SolidPattern : PatternBase
{
    public override byte[] Bytes { get; protected set; }

    public SolidPattern(string pattern)
    {
        byte[] pooledBytes = ArrayPool<byte>.Shared.Rent(pattern.Length);

        try
        {
            Span<byte> pBytes = pooledBytes;
            ReadOnlySpan<char> patternSpan = pattern;
            int pos = 0;
            foreach (var range in patternSpan.Split(' '))
            {
                ReadOnlySpan<char> part = patternSpan[range];
                pooledBytes[pos] = byte.Parse(part, NumberStyles.HexNumber);
                pos++;
            }

            Bytes = pBytes.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pooledBytes);
        }
    }

    public override void ScanChunk(in MemoryRange mbi, List<nint> threadLocalList, in Span<byte> buffer)
    {
        int currentOffset = 0;

        while (true)
        {
            int hitIndex;
            if ((hitIndex = buffer.IndexOf(Bytes)) == -1)
                break;

            threadLocalList.Add(mbi.BaseAddress + hitIndex);
            currentOffset += hitIndex + 1;
        }
    }
}
