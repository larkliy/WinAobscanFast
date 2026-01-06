using WinAobscanFast.Utils;
using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinAobscanFast.Enums;
using WinAobscanFast.Structs;
using WinAobscanFast.Extensions;

namespace WinAobscanFast;

public class AobScan
{
    private readonly SafeProcessHandle _processHandle;
    private readonly Lock _syncRoot = new();

    public AobScan(SafeProcessHandle processHandle) => _processHandle = processHandle;

    /// <summary>
    /// Scans the process memory for occurrences of the specified pattern, filtered by the given memory access
    /// permissions.
    /// </summary>
    /// <remarks>The scan is performed in parallel across all memory regions matching the specified access
    /// filter. The method throws an <see cref="OperationCanceledException"/> if the operation is canceled via the
    /// provided cancellation token. This method is thread-safe.</remarks>
    /// <param name="input">A string representation of the pattern to search for in process memory. The format must be compatible with the
    /// pattern parser.</param>
    /// <param name="accessFilter">A bitwise combination of memory access flags that determines which memory regions are included in the scan.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the scan operation. The default value is <see
    /// cref="CancellationToken.None"/>.</param>
    /// <returns>A list of memory addresses where the specified pattern was found. The list is empty if no matches are found.</returns>
    public unsafe List<nint> Scan(string input, MemoryAccess accessFilter, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var finalResults = new List<nint>(capacity: 1024);
        var pattern = Pattern.Create(input);
        var regions = GetRegions(accessFilter, nint.MinValue, nint.MaxValue);

        try
        {
            Parallel.ForEach(regions, new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount * 2,
                CancellationToken = ct
            },
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
                        if (Native.ReadProcessMemory(_processHandle, mbi.BaseAddress, buffer, (nuint)buffer.Length, out nuint bytesRead))
                        {
                            ScanRegionForPattern(in mbi, threadLocalList, in pattern, regionsSize, in buffer);
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
        }
        catch (OperationCanceledException)
        {
            throw;
        }


        return finalResults;
    }

    private static void ScanRegionForPattern(
        in MEMORY_BASIC_INFORMATION mbi,
        List<nint> threadLocalList,
        in Pattern pattern,
        int regionsSize,
        in Span<byte> buffer)
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

    /// <summary>
    /// Asynchronously scans memory for values matching the specified input and access filter.
    /// </summary>
    /// <param name="input">The value or pattern to search for in memory.</param>
    /// <param name="accessFilter">A filter that specifies the type of memory access to include in the scan.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the scan operation.</param>
    /// <returns>A task that represents the asynchronous scan operation. The task result contains a list of memory addresses where
    /// matches were found.</returns>
    public Task<List<nint>> ScanAsync(string input, MemoryAccess accessFilter, CancellationToken ct = default)
    {
        return Task.Run(() => Scan(input, accessFilter, ct), ct);
    }

    private List<MEMORY_BASIC_INFORMATION> GetRegions(MemoryAccess accessFilter, nint searchStart, nint searchEnd)
    {
        nint address = 0;
        var regions = new List<MEMORY_BASIC_INFORMATION>();

        while (address < searchEnd)
        {
            if (Native.VirtualQueryEx(_processHandle, address, out var mbi, Unsafe.SizeOf<MEMORY_BASIC_INFORMATION>()) == 0)
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
