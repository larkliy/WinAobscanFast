using WinAobscanFast.Utils;
using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WinAobscanFast;

public class AobScan
{
    private readonly SafeProcessHandle _processHandle;
    private readonly Lock _syncRoot = new();

    public AobScan(SafeProcessHandle processHandle) => _processHandle = processHandle;

    /// <summary>
    /// Scans the process memory for occurrences of the specified pattern and returns the addresses where matches are
    /// found within regions that meet the given access filter.
    /// </summary>
    /// <remarks>Scanning is performed in parallel across eligible memory regions to improve performance. The
    /// method is thread-safe and can be called concurrently from multiple threads. The accuracy and completeness of
    /// results depend on the accessibility of memory regions and the correctness of the pattern format.</remarks>
    /// <param name="input">A string representing the pattern to search for in process memory. The format and interpretation of the pattern
    /// are determined by the implementation of the Pattern class.</param>
    /// <param name="accessFilter">A filter specifying the types of memory access permissions to include in the scan. Only memory regions matching
    /// this filter will be searched.</param>
    /// <returns>A list of memory addresses (as native integers) where the pattern was found. The list will be empty if no
    /// matches are found.</returns>
    public unsafe List<nint> Scan(string input, MemoryAccess accessFilter)
    {
        var finalResults = new List<nint>();
        var pattern = Pattern.Create(input);
        var regions = GetRegions(accessFilter, nint.MinValue, nint.MaxValue);

        Parallel.ForEach(regions, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 },
            () => new List<nint>(),

            (mbi, loopState, threadLocalList) =>
            {
                int regionsSize = (int)mbi.RegionSize;

                if (regionsSize <= 0) 
                    return threadLocalList;

                void* bufferPtr = NativeMemory.Alloc((nuint)regionsSize);
                var buffer = new Span<byte>(bufferPtr, regionsSize);

                try
                {
                    if (NativeMethods.ReadProcessMemory(_processHandle, mbi.BaseAddress, buffer, (nuint)buffer.Length, out nuint bytesRead))
                    {
                        int seqOffset = pattern.SearchSequenceOffset;
                        ReadOnlySpan<byte> searchSeq = pattern.SearchSequence;
                        int patternLength = pattern.Bytes.Length;

                        int currentOffset = 0;

                        while (true)
                        {
                            ReadOnlySpan<byte> remainingSpan = buffer[currentOffset..];

                            int hitIndex = remainingSpan.IndexOf(searchSeq);

                            if (hitIndex == -1)
                                break;

                            int foundSeqPos = currentOffset + hitIndex;

                            int patternStartPos = foundSeqPos - seqOffset;

                            if (patternStartPos >= 0 && patternStartPos + patternLength <= regionsSize)
                            {
                                ReadOnlySpan<byte> candidateBytes = buffer.Slice(patternStartPos, patternLength);

                                if (pattern.IsMatch(candidateBytes))
                                {
                                    threadLocalList.Add(mbi.BaseAddress + patternStartPos);
                                }
                            }

                            currentOffset += hitIndex + 1;
                        }
                    }
                }
                finally
                {
                    NativeMemory.Free(bufferPtr);
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

    private List<MEMORY_BASIC_INFORMATION> GetRegions(MemoryAccess accessFilter, nint searchStart, nint searchEnd)
    {
        nint address = 0;
        var regions = new List<MEMORY_BASIC_INFORMATION>();

        while (address < searchEnd)
        {
            if (NativeMethods.VirtualQueryEx(_processHandle, address, out var mbi, Unsafe.SizeOf<MEMORY_BASIC_INFORMATION>()) == 0)
                break;

            nint regionStart = mbi.BaseAddress;
            nint regionSize = mbi.RegionSize;
            nint regionEnd = regionStart + regionSize;

            bool isOverlapping = regionEnd > searchStart && regionStart < searchEnd;

            if (isOverlapping)
            {
                bool isValidState = mbi.State == MemoryState.MEM_COMMIT;

                bool isNotGuard = (mbi.Protect & MemoryProtect.PAGE_GUARD) == 0;
                bool isNotNoAccess = (mbi.Protect & MemoryProtect.PAGE_NOACCESS) == 0;

                if (isValidState && isNotGuard && isNotNoAccess)
                {
                    bool meetsFilter = true;
                    bool isReadable = mbi.IsReadableRegion();
                    bool isWritable = mbi.IsWritableRegion();
                    bool isExecutable = mbi.IsExecutableRegion();

                    if (accessFilter.HasFlag(MemoryAccess.Readable) && !isReadable) meetsFilter = false;
                    if (accessFilter.HasFlag(MemoryAccess.Writable) && !isWritable) meetsFilter = false;
                    if (accessFilter.HasFlag(MemoryAccess.Executable) && !isExecutable) meetsFilter = false;

                    if (meetsFilter)
                    {
                        regions.Add(mbi);
                    }
                }
            }

            address = regionEnd;
        }

        return regions;
    }
}
