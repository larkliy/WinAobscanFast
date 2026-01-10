using System.Buffers;
using System.Runtime.CompilerServices;
using WinAobscanFast.Core.Abstractions;
using WinAobscanFast.Core.Models;
using WinAobscanFast.Enums;
using WinAobscanFast.Utils;

namespace WinAobscanFast.Core;

public class AobScan
{
    private readonly Lock _syncRoot = new();
    private readonly IMemoryReader _memoryReader;

    private readonly static AobScanOptions s_scanOptionsDefault = new()
    {
        MinScanAddress = 0,
        MaxScanAddress = nint.MaxValue,
        MemoryAccess = MemoryAccess.Readable | MemoryAccess.Writable
    };

    public AobScan(IMemoryReader memoryReader) => _memoryReader = memoryReader;

    public List<nint> Scan(string input)
        => Scan(input, s_scanOptionsDefault);

    public List<nint> Scan(string input, AobScanOptions? scanOptions)
    {
        scanOptions = ValidateScanOptions(scanOptions);

        var finalResults = new List<nint>(capacity: 1024);
        var pattern = Pattern.Create(input);
        var rawRegions = _memoryReader.GetRegions((nint)scanOptions.MinScanAddress!,
                                                  (nint)scanOptions.MaxScanAddress!,
                                                  scanOptions.MemoryAccess);

        var chunks = RegionChunker.CreateMemoryChunks(rawRegions, pattern.Bytes.Length);

        Parallel.ForEach(chunks,
            () => new List<nint>(capacity: 64),

            (regionChunk, loopState, threadLocalList) =>
            {
                int size = (int)regionChunk.Size;

                if (size <= 0)
                    return threadLocalList;

                byte[] poolBuffer = ArrayPool<byte>.Shared.Rent(size);

                var buffer = poolBuffer.AsSpan(0, size);

                try
                {
                    if (_memoryReader.ReadMemory(regionChunk.BaseAddress, buffer, out _))
                    {
                        ScanChunk(in regionChunk, threadLocalList, in pattern, size, in buffer);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(poolBuffer);
                }

                return threadLocalList;
            },

            localList =>
            {
                lock (_syncRoot)
                {
                    finalResults.AddRange(localList);
                }
            });

        return finalResults;
    }

    private static AobScanOptions ValidateScanOptions(AobScanOptions? scanOptions)
    {
        scanOptions ??= s_scanOptionsDefault;

        scanOptions.MinScanAddress ??= s_scanOptionsDefault.MinScanAddress;
        scanOptions.MaxScanAddress ??= s_scanOptionsDefault.MaxScanAddress;

        if (scanOptions.MemoryAccess == MemoryAccess.None)
            scanOptions.MemoryAccess = s_scanOptionsDefault.MemoryAccess;

        return scanOptions;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ScanChunk(
        in MemoryRange mbi,
        List<nint> threadLocalList,
        in Pattern pattern,
        int regionsSize,
        in Span<byte> buffer)
    {
        int seqOffset = pattern.SearchSequenceOffset;
        var searchSeq = pattern.SearchSequence;
        int patternLength = pattern.Bytes.Length;
        int searchSeqLength = searchSeq.Length;

        int lastValidPatternStart = regionsSize - patternLength;

        int lastValidSeqPos = lastValidPatternStart + seqOffset;

        int currentOffset = 0;

        while (true)
        {
            int remainingLength = lastValidSeqPos - currentOffset + searchSeqLength;

            if (remainingLength < searchSeqLength)
                break;

            int hitIndex = buffer.Slice(currentOffset, remainingLength).IndexOf(searchSeq);

            if (hitIndex == -1)
                break;
            int foundSeqPos = currentOffset + hitIndex;
            int patternStartPos = foundSeqPos - seqOffset;

            if (patternStartPos >= 0)
            {
                var candidateBytes = buffer.Slice(patternStartPos, patternLength);

                if (pattern.IsMatch(ref candidateBytes))
                {
                    threadLocalList.Add(mbi.BaseAddress + patternStartPos);
                }
            }

            currentOffset += hitIndex + 1;
        }
    }
}
