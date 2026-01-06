using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace WinAobscanFast;

/// <summary>
/// Represents a byte pattern with associated mask information for searching or matching sequences within binary data.
/// </summary>
/// <remarks>A Pattern encapsulates both the byte values and mask used to identify concrete and wildcard positions
/// in a search sequence. It is typically used for efficient pattern matching in binary streams, such as searching for
/// signatures or markers. Instances are created using the static Create method, which parses a string representation of
/// the pattern. The struct is immutable and thread-safe.</remarks>
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

    /// <summary>
    /// Creates a new <see cref="Pattern"/> instance from a string representation of a byte pattern, where hexadecimal
    /// byte values and wildcard tokens are separated by spaces.
    /// </summary>
    /// <remarks>Wildcard tokens ("?" or "??") in the pattern represent bytes that can match any value. At
    /// least one concrete byte value must be present in the pattern; otherwise, a <see cref="FormatException"/> is
    /// thrown.</remarks>
    /// <param name="pattern">A string containing the pattern to parse. Each token should be a hexadecimal byte value (e.g., "FF") or a
    /// wildcard ("?" or "??"), separated by spaces.</param>
    /// <returns>A <see cref="Pattern"/> object representing the parsed byte pattern and mask.</returns>
    /// <exception cref="FormatException">Thrown if the pattern consists only of wildcard tokens and does not contain any concrete byte values.</exception>
    public static Pattern Create(string pattern)
    {
        string[] tokens = pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        byte[] pBytes = new byte[tokens.Length];
        byte[] pMask = new byte[tokens.Length];

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

    private static int FindFirstConcreteByteIndex(string[] tokens, byte[] pBytes, byte[] pMask)
    {
        int bestIndex = -1;

        for (int i = 0; i < tokens.Length; i++)
        {
            if (pMask[i] == byte.MaxValue && pBytes[i] != byte.MinValue && pBytes[i] != byte.MaxValue)
            {
                bestIndex = i;
                break;
            }
        }

        if (bestIndex == -1)
            bestIndex = pMask.IndexOf(byte.MaxValue);

        return bestIndex;
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

        return (pBytes[bestStart..bestLength].ToArray(), bestStart);
    }

    /// <summary>
    /// Determines whether the specified data matches the pattern defined by the current instance, using the associated
    /// mask and byte sequence.
    /// </summary>
    /// <remarks>This method uses hardware acceleration when available to improve performance for large data
    /// spans. The comparison applies the mask to each byte before checking for equality with the pattern. If the length
    /// of pData is less than the pattern length, the method returns false.</remarks>
    /// <param name="pData">A read-only span of bytes representing the data to compare against the pattern. The span must be at least as
    /// long as the pattern length.</param>
    /// <returns>true if the data matches the pattern according to the mask; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsMatch(ReadOnlySpan<byte> pData)
    {
        int length = Bytes.Length;

        if (pData.Length < length)
            return false;

        ReadOnlySpan<byte> pMask = Mask;
        ReadOnlySpan<byte> pBytes = Bytes;

        int vecSize = Vector<byte>.Count;
        int i = 0;

        if (Vector.IsHardwareAccelerated)
        {
            int simdEnd = length - vecSize;

            while (i <= simdEnd)
            {
                var vBytes = new Vector<byte>(pBytes[i..]);
                var vMask = new Vector<byte>(pMask[i..]);
                var vData = new Vector<byte>(pData[i..]);

                if (!Vector.EqualsAll(vData & vMask, vBytes))
                    return false;

                i += vecSize;
            }
        }

        for (; i < length; i++)
        {
            if ((pData[i] & pMask[i]) != pBytes[i])
                return false;
        }

        return true;
    }
}
