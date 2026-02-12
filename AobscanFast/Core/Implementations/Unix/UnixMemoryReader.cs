using AobscanFast.Core.Models;
using AobscanFast.Enums.Windows;
using AobscanFast.Structs.Unix;
using AobscanFast.Utils;
using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;

namespace AobscanFast.Core.Implementations.Unix;

internal class UnixMemoryReader(ProcessInfo processInfo) : IMemoryReader
{
    public unsafe bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead)
    {
        int pid = (int)processInfo.ProcessId;

        fixed (byte* localPtr = buffer)
        {
            var localIo = new Iovec
            {
                Base = localPtr,
                Length = (nuint)buffer.Length
            };

            var remoteIo = new Iovec
            {
                Base = (void*)baseAddress,
                Length = (nuint)buffer.Length
            };

            nint result = Native.process_vm_readv(pid, &localIo, 1, &remoteIo, 1, 0);

            if (result == -1)
            {
                bytesRead = 0;
                return false;
            }

            bytesRead = (nuint)result;
            return bytesRead == (nuint)buffer.Length;
        }
    }

    public List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess accessFilter)
    {
        var regions = new List<MemoryRange>(512);
        string mapsPath = $"/proc/{processInfo.ProcessId}/maps";

        if (!File.Exists(mapsPath))
            return regions;

        using var fs = new FileStream(mapsPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
        int validBytes = 0;
        int offset = 0;

        try
        {
            while (true)
            {
                int read = fs.Read(buffer, offset, buffer.Length - offset);
                if (read == 0 && offset == 0) break;

                validBytes = offset + read;
                ReadOnlySpan<byte> span = buffer.AsSpan(0, validBytes);

                int processedCount = 0;

                while (true)
                {
                    int newlineIndex = span[processedCount..].IndexOf((byte)'\n');

                    if (newlineIndex == -1)
                        break;

                    var lineSpan = span.Slice(processedCount, newlineIndex);
                    ParseLineAndAdd(lineSpan, regions, minAddress, maxAddress, accessFilter);
                    processedCount += newlineIndex + 1;
                }

                if (processedCount < validBytes)
                {
                    int remaining = validBytes - processedCount;
                    Array.Copy(buffer, processedCount, buffer, 0, remaining);
                    offset = remaining;

                    if (read == 0)
                    {
                        ParseLineAndAdd(buffer.AsSpan(0, remaining), regions, minAddress, maxAddress, accessFilter);
                        break;
                    }
                }
                else
                {
                    offset = 0;
                    if (read == 0) break;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return RegionUtils.MergeRegions(regions);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ParseLineAndAdd(ReadOnlySpan<byte> line, List<MemoryRange> regions, nint minAddress, nint maxAddress, MemoryAccess accessFilter)
    {
        int dashIndex = line.IndexOf((byte)'-');
        if (dashIndex == -1) return;

        int spaceIndex = line[(dashIndex + 1)..].IndexOf((byte)' ');
        if (spaceIndex == -1) return;
        spaceIndex += dashIndex + 1;

        if (!TryParseHex(line[..dashIndex], out ulong start)) return;
        if (!TryParseHex(line.Slice(dashIndex + 1, spaceIndex - dashIndex - 1), out ulong end)) return;

        nuint regionSize = (nuint)(end - start);
        nint regionBase = (nint)start;
        nint regionSizeSigned = (nint)regionSize;

        if (regionBase >= maxAddress || (regionBase + regionSizeSigned) <= minAddress)
            return;

        int permsStart = spaceIndex + 1;
        if (permsStart + 4 > line.Length) return;

        if (CheckAccess(line.Slice(permsStart, 4), accessFilter))
        {
            nint actualStart = regionBase < minAddress ? minAddress : regionBase;
            nint actualEnd = (regionBase + regionSizeSigned) > maxAddress ? maxAddress : (regionBase + regionSizeSigned);

            regions.Add(new MemoryRange(actualStart, actualEnd - actualStart));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseHex(ReadOnlySpan<byte> hexSpan, out ulong result)
        => Utf8Parser.TryParse(hexSpan, out result, out _, 'x');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CheckAccess(ReadOnlySpan<byte> perms, MemoryAccess accessFilter)
    {
        if ((accessFilter & MemoryAccess.Readable) != 0 && perms[0] != (byte)'r') return false;
        if ((accessFilter & MemoryAccess.Writable) != 0 && perms[1] != (byte)'w') return false;
        if ((accessFilter & MemoryAccess.Executable) != 0 && perms[2] != (byte)'x') return false;

        if (perms[3] != (byte)'p') return false;

        return true;
    }
}
