using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace AobscanFast.Core.Models.Pattern;

internal class MaskPattern : PatternBase
{
    public override byte[] Bytes { get; protected set; } = null!;
    public byte[] Mask { get; private set; } = null!;

    public byte[] SearchSequence { get; private set; } = null!;
    public int SearchSequenceOffset { get; private set; }

    public MaskPattern(string pattern) => Create(pattern);

    private void Create(string pattern)
    {
        byte[] pooledBytes = ArrayPool<byte>.Shared.Rent(pattern.Length);
        byte[] pooledMask = ArrayPool<byte>.Shared.Rent(pattern.Length);

        try
        {
            Span<byte> pBytes = pooledBytes.AsSpan(0, pattern.Length);
            Span<byte> pMask = pooledMask.AsSpan(0, pattern.Length);

            int length = 0;
            foreach (var range in pattern.AsSpan().Split(' '))
            {
                ReadOnlySpan<char> token = pattern.AsSpan(range);

                if (token.Length >= 1 && token[0] == '?')
                {
                    pBytes[length] = 0;
                    pMask[length] = 0;
                }
                else
                {
                    pBytes[length] = HexToByte(token);
                    pMask[length] = 0xFF;
                }
                length++;
            }

            var finalBytes = pBytes[..length].ToArray();
            var finalMask = pMask[..length].ToArray();

            if (!finalMask.Any(b => b == 0xFF))
                throw new FormatException("A pattern cannot consist of masks alone.");

            var (bestSeq, offset) = FindLongestSolidRun(finalBytes, finalMask);

            Bytes = finalBytes;
            Mask = finalMask;
            SearchSequence = bestSeq;
            SearchSequenceOffset = offset;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pooledBytes);
            ArrayPool<byte>.Shared.Return(pooledMask);
        }
    }

    private static (byte[] BestSequence, int Offset) FindLongestSolidRun(ReadOnlySpan<byte> pBytes,
                                                                         ReadOnlySpan<byte> pMask)
    {
        int bestStart = 0;
        int bestLength = 0;

        int currentStart = 0;
        int currentLength = 0;

        for (int i = 0; i < pMask.Length; i++)
        {
            if (pMask[i] == byte.MaxValue)
            {
                if (currentLength == 0)
                    currentStart = i;

                currentLength++;

                if (currentLength > bestLength)
                {
                    bestStart = currentStart;
                    bestLength = currentLength;
                }
            }
            else
            {
                currentLength = 0;
            }
        }

        if (bestLength == 0)
            return ([], -1);

        return (pBytes.Slice(bestStart, bestLength).ToArray(), bestStart);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte HexToByte(ReadOnlySpan<char> s)
    {
        int h = s[0];
        int l = s[1];
        h = (h > '9') ? (h & ~0x20) - 'A' + 10 : (h - '0');
        l = (l > '9') ? (l & ~0x20) - 'A' + 10 : (l - '0');
        return (byte)((h << 4) | l);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool IsMatch(ref Span<byte> data)
    {
        nuint length = (nuint)Bytes.Length;

        if ((nuint)data.Length < length)
            return false;

        ref byte pBytes = ref MemoryMarshal.GetArrayDataReference(Bytes);
        ref byte pMask = ref MemoryMarshal.GetArrayDataReference(Mask);
        ref byte pData = ref MemoryMarshal.GetReference(data);

        nuint i = 0;

        if (Vector512.IsHardwareAccelerated && length >= (nuint)Vector512<byte>.Count)
        {
            nuint limit = length - (nuint)Vector512<byte>.Count;
            while (i <= limit)
            {
                var vData = Vector512.LoadUnsafe(ref pData, i);
                var vMask = Vector512.LoadUnsafe(ref pMask, i);
                var vBytes = Vector512.LoadUnsafe(ref pBytes, i);

                if ((vData & vMask) != vBytes)
                    return false;

                i += (nuint)Vector512<byte>.Count;
            }
        }

        if (Vector256.IsHardwareAccelerated && (length - i) >= (nuint)Vector256<byte>.Count)
        {
            nuint limit = length - (nuint)Vector256<byte>.Count;
            while (i <= limit)
            {
                var vData = Vector256.LoadUnsafe(ref pData, i);
                var vMask = Vector256.LoadUnsafe(ref pMask, i);
                var vBytes = Vector256.LoadUnsafe(ref pBytes, i);

                if ((vData & vMask) != vBytes)
                    return false;

                i += (nuint)Vector256<byte>.Count;
            }
        }

        if (Vector128.IsHardwareAccelerated && (length - i) >= (nuint)Vector128<byte>.Count)
        {
            nuint limit = length - (nuint)Vector128<byte>.Count;
            while (i <= limit)
            {
                var vData = Vector128.LoadUnsafe(ref pData, i);
                var vMask = Vector128.LoadUnsafe(ref pMask, i);
                var vBytes = Vector128.LoadUnsafe(ref pBytes, i);

                if ((vData & vMask) != vBytes)
                    return false;

                i += (nuint)Vector128<byte>.Count;
            }
        }

        while (i < length)
        {
            if ((Unsafe.Add(ref pData, i) & Unsafe.Add(ref pMask, i)) != Unsafe.Add(ref pBytes, i))
                return false;
            i++;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void ScanChunk(in MemoryRange mbi, List<nint> threadLocalList, in Span<byte> buffer)
    {
        int lastValidPatternStart = (int)(mbi.Size - Bytes.Length);
        int lastValidSeqPos = lastValidPatternStart + SearchSequenceOffset;
        int currentOffset = 0;

        while (true)
        {
            int remainingLength = lastValidSeqPos - currentOffset + SearchSequence.Length;
            if (remainingLength < SearchSequence.Length)
                break;

            int hitIndex;
            if ((hitIndex = buffer.Slice(currentOffset, remainingLength).IndexOf(SearchSequence)) == -1)
                break;

            int foundSeqPos = currentOffset + hitIndex;
            int patternStartPos = foundSeqPos - SearchSequenceOffset;

            if (patternStartPos >= 0)
            {
                var candidateBytes = buffer.Slice(patternStartPos, Bytes.Length);
                if (IsMatch(ref candidateBytes))
                    threadLocalList.Add(mbi.BaseAddress + patternStartPos);
            }

            currentOffset += hitIndex + 1;
        }
    }
}
