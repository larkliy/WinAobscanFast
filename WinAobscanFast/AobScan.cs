using System.Buffers;
using WinAobscanFast.Abstractions;
using WinAobscanFast.Enums;
using WinAobscanFast.Structs;

namespace WinAobscanFast;

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
        var regions = _memoryReader.GetRegions((nint)scanOptions.MinScanAddress!,
                                               (nint)scanOptions.MaxScanAddress!,
                                               scanOptions.MemoryAccess);

        Parallel.ForEach(regions,
            () => new List<nint>(capacity: 64),

            (regionRange, loopState, threadLocalList) =>
            {
                nint regionsSize = regionRange.Size;

                if (regionsSize <= 0)
                    return threadLocalList;

                byte[] poolBuffer = ArrayPool<byte>.Shared.Rent((int)regionsSize);
                var buffer = poolBuffer.AsSpan(0, (int)regionsSize);

                try
                {
                    if (_memoryReader.ReadMemory(regionRange.BaseAddress, buffer, out _))
                    {
                        ScanRegionForPattern(in regionRange, threadLocalList, in pattern, regionsSize, in buffer);
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

    private static void ScanRegionForPattern(
        in MemoryRange mbi,
        List<nint> threadLocalList,
        in Pattern pattern,
        nint regionsSize,
        in Span<byte> buffer)
    {
        int seqOffset = pattern.SearchSequenceOffset;
        var searchSeq = pattern.SearchSequence;
        int patternLength = pattern.Bytes.Length;

        int currentOffset = 0;

        while (true)
        {
            int hitIndex;

            if ((hitIndex = buffer[currentOffset..].IndexOf(searchSeq)) == -1)
                break;

            int foundSeqPos = currentOffset + hitIndex;
            int patternStartPos = foundSeqPos - seqOffset;

            if (IsPatternWithinRegion(patternStartPos, patternLength, regionsSize))
            {
                var candidateBytes = buffer.Slice(patternStartPos, patternLength);

                if (pattern.IsMatch(candidateBytes))
                {
                    threadLocalList.Add(mbi.BaseAddress + patternStartPos);
                }
            }

            currentOffset += hitIndex + 1;
        }

        static bool IsPatternWithinRegion(int patternStartPos, int patternLength, nint regSize)
            => patternLength >= 0 && patternStartPos + patternLength <= regSize;
    }
}
