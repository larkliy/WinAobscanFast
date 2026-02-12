using System.Buffers;
using AobscanFast.Core.Models;
using AobscanFast.Core.Models.Pattern;
using AobscanFast.Utils;

namespace AobscanFast.Core;

public class AobScan
{
    private readonly Lock _syncRoot = new();
    private readonly IMemoryReader _memoryReader;

    private AobScan(IMemoryReader memoryReader) => _memoryReader = memoryReader;

    public static List<nint> ScanProcess(string process, string pattern, AobScanOptions? scanOptions = null)
    {
        using var processInfo = ProcessMemoryFactory.OpenProcessByName(process);
        var memoryReader = ProcessMemoryFactory.CreateMemoryReader(processInfo);

        var aobscan = new AobScan(memoryReader);
        return aobscan.Scan(pattern, scanOptions);
    }

    public static async Task<List<nint>> ScanProcessAsync(string process, string pattern, AobScanOptions? scanOptions = null, CancellationToken cancellationToken = default)
    {
        using var processInfo = ProcessMemoryFactory.OpenProcessByName(process);
        var memoryReader = ProcessMemoryFactory.CreateMemoryReader(processInfo);

        var aobscan = new AobScan(memoryReader);
        return await aobscan.ScanAsync(pattern, scanOptions, cancellationToken);
    }

    public static List<nint> ScanModule(string processName, string moduleName, string pattern)
    {
        uint pid = ProcessMemoryFactory.FindProcessIdByName(processName);
        var moduleInfo = ProcessMemoryFactory.GetModule(pid, moduleName);
        Console.WriteLine(moduleInfo.Size);
        var scanOptions = new AobScanOptions(
            minScanAddress: moduleInfo.BaseAddress,
            maxScanAddress: (nint?)(moduleInfo.BaseAddress + moduleInfo.Size));

        using var handle = ProcessMemoryFactory.OpenProcessById(pid);
        var memoryReader = ProcessMemoryFactory.CreateMemoryReader(handle);

        var aobscan = new AobScan(memoryReader);
        return aobscan.Scan(pattern, scanOptions);
    }

    public static async Task<List<nint>> ScanModuleAsync(string processName, string moduleName, string pattern, CancellationToken cancellationToken = default)
    {
        uint pid = ProcessMemoryFactory.FindProcessIdByName(processName);
        var moduleInfo = ProcessMemoryFactory.GetModule(pid, moduleName);

        var scanOptions = new AobScanOptions(
            minScanAddress: moduleInfo.BaseAddress,
            maxScanAddress: (nint?)(moduleInfo.BaseAddress + moduleInfo.Size));

        using var processInfo = ProcessMemoryFactory.OpenProcessById(pid);
        var memoryReader = ProcessMemoryFactory.CreateMemoryReader(processInfo);

        var aobscan = new AobScan(memoryReader);
        return await aobscan.ScanAsync(pattern, scanOptions, cancellationToken);
    }

    public List<nint> Scan(string input)
        => Scan(input, null);

    public Task<List<nint>> ScanAsync(string input, CancellationToken cancellationToken = default)
        => ScanAsync(input, null, cancellationToken);

    public Task<List<nint>> ScanAsync(string input, AobScanOptions? scanOptions, CancellationToken cancellationToken = default) 
        => Task.Run(() => Scan(input, scanOptions, cancellationToken), cancellationToken);

    public List<nint> Scan(string input, AobScanOptions? scanOptions, CancellationToken cancellationToken = default)
    {
        scanOptions ??= new();
        var pattern = PatternCreateFactory.Create(input);

        var rawRegions = _memoryReader.GetRegions((nint)scanOptions.MinScanAddress!,
                                                  (nint)scanOptions.MaxScanAddress!,
                                                  scanOptions.MemoryAccess);

        var chunks = RegionUtils.CreateMemoryChunks(rawRegions, pattern.Bytes.Length);

        return pattern switch
        {
            MaskPattern d => ExecuteScan(d, chunks, cancellationToken),
            SolidPattern s => ExecuteScan(s, chunks, cancellationToken),
            _ => throw new NotImplementedException()
        };
    }

    private List<nint> ExecuteScan<TPattern>(TPattern pattern,
                                             List<MemoryRange> chunks,
                                             CancellationToken cancellationToken = default) where TPattern : PatternBase
    {
        var finalResults = new List<nint>(capacity: 1024);

        Parallel.ForEach(chunks,
            new ParallelOptions { CancellationToken = cancellationToken },
            () => new List<nint>(capacity: 64),

            (regionChunk, loopState, threadLocalList) =>
            {
                int size = (int)regionChunk.Size;
                if (size <= 0) return threadLocalList;

                byte[] rentedArray = ArrayPool<byte>.Shared.Rent(size);
                Span<byte> buffer = rentedArray.AsSpan(0, size);

                try
                {
                    if (_memoryReader.ReadMemory(regionChunk.BaseAddress, buffer, out _))
                        pattern.ScanChunk(in regionChunk, threadLocalList, buffer);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rentedArray);
                }

                return threadLocalList;
            },

            localList =>
            {
                lock (_syncRoot)
                    finalResults.AddRange(localList);
            });

        return finalResults;
    }
}
