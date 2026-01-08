using Microsoft.Win32.SafeHandles;
using System.Buffers;
using WinAobscanFast.Enums;
using WinAobscanFast.Structs;
using WinAobscanFast.Utils;

namespace WinAobscanFast;

public class AobScan
{
    private readonly SafeProcessHandle _processHandle;
    private readonly Lock _syncRoot = new();

    private readonly static AobScanOptions s_scanOptionsDefault = new()
    {
        MinScanAddress = nint.MinValue,
        MaxScanAddress = nint.MaxValue,
        MemoryAccess = MemoryAccess.Readable | MemoryAccess.Writable
    };

    public AobScan(SafeProcessHandle processHandle) => _processHandle = processHandle;

    public List<nint> Scan(string input) 
        => Scan(input, s_scanOptionsDefault);

    public List<nint> Scan(string input, AobScanOptions? scanOptions)
    {
        scanOptions = ValidateScanOptions(scanOptions);

        var finalResults = new List<nint>(capacity: 1024);
        var pattern = Pattern.Create(input);
        var regions = MemoryRegionUtils.GetRegions(_processHandle,
                                                   scanOptions.MemoryAccess,
                                                   (nint)scanOptions.MinScanAddress!,
                                                   (nint)scanOptions.MaxScanAddress!);


        Parallel.ForEach(regions,
            () => new List<nint>(),

            (mbi, loopState, threadLocalList) =>
            {
                int regionsSize = (int)mbi.RegionSize;

                if (regionsSize <= 0)
                    return threadLocalList;

                byte[] poolBuffer = ArrayPool<byte>.Shared.Rent((int)mbi.RegionSize);
                var buffer = poolBuffer.AsSpan(0, (int)mbi.RegionSize);

                try
                {
                    if (Native.ReadProcessMemory(_processHandle, mbi.BaseAddress, buffer, (nuint)mbi.RegionSize, out nuint bytesRead))
                    {
                        ScanRegionForPattern(in mbi, threadLocalList, in pattern, regionsSize, in buffer);
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
        in MEMORY_BASIC_INFORMATION mbi,
        List<nint> threadLocalList,
        in Pattern pattern,
        int regionsSize,
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

        static bool IsPatternWithinRegion(int patternStartPos, int patternLength, int regSize)
            => patternLength >= 0 && patternStartPos + patternLength <= regSize;
    }
}
