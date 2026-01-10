using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace WinAobscanFast.Core.Models;

public readonly struct Pattern
{
    public readonly byte[] Bytes;
    public readonly byte[] Mask;

    public readonly byte[] SearchSequence;
    public readonly int SearchSequenceOffset;

    private Pattern(byte[] bytes, byte[] mask, byte[] searchSequence, int searchSequenceOffset)
    {
        Bytes = bytes;
        Mask = mask;
        SearchSequence = searchSequence;
        SearchSequenceOffset = searchSequenceOffset;
    }

    public static Pattern Create(string pattern)
    {
        string[] tokens = pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int len = tokens.Length;

        byte[] pBytes = new byte[64];
        byte[] pMask = new byte[64];

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];

            if (token == "?" || token == "??")
            {
                pBytes[i] = byte.MinValue;
                pMask[i] = byte.MinValue;
            }
            else
            {
                pBytes[i] = byte.Parse(token, NumberStyles.HexNumber);
                pMask[i] = byte.MaxValue;
            }
        }

        if (!pMask.Any(p => p == byte.MaxValue))
            throw new FormatException("A pattern cannot consist of masks alone.");

        var (bestSeq, offset) = FindLongestSolidRun(pBytes, pMask);

        return new Pattern(pBytes, pMask, bestSeq, offset);
    }

    private static (byte[] BestSequence, int Offset) FindLongestSolidRun(ReadOnlySpan<byte> pBytes, ReadOnlySpan<byte> pMask)
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
    public bool IsMatch(ref Span<byte> data)
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
}
