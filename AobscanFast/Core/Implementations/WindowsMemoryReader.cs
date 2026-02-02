using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinAobscanFast.Core.Abstractions;
using WinAobscanFast.Core.Extensions;
using WinAobscanFast.Core.Models;
using WinAobscanFast.Enums;
using WinAobscanFast.Structs;

namespace WinAobscanFast.Core.Implementations;

internal class WindowsMemoryReader(SafeProcessHandle processHandle) : IMemoryReader
{
    public bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead) 
        => Native.ReadProcessMemory(processHandle, baseAddress, buffer, (nuint)buffer.Length, out bytesRead);

    public List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess accessFilter)
    {
        nint currentAddress = minAddress;
        var regions = new List<MemoryRange>(256);
        nint mbiSize = Unsafe.SizeOf<MEMORY_BASIC_INFORMATION>();

        while (currentAddress < maxAddress)
        {
            if (Native.VirtualQueryEx(processHandle, currentAddress, out var mbi, mbiSize) == 0)
                break;

            bool isCommit = mbi.State == MemoryState.MEM_COMMIT;
            bool isGuard = (mbi.Protect & MemoryProtect.PAGE_GUARD) != 0;
            bool isNoAccess = (mbi.Protect & MemoryProtect.PAGE_NOACCESS) != 0;

            if (isCommit && !isGuard && !isNoAccess)
                if (CheckAccess(ref mbi, accessFilter))
                    regions.Add(new MemoryRange(mbi.BaseAddress, mbi.RegionSize));

            currentAddress = mbi.BaseAddress + mbi.RegionSize;
        }

        return MergeRegions(regions);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CheckAccess(ref MEMORY_BASIC_INFORMATION mbi, MemoryAccess accessFilter)
    {
        if ((accessFilter & MemoryAccess.Readable) != 0 && !mbi.IsReadableRegion()) return false;
        if ((accessFilter & MemoryAccess.Writable) != 0 && !mbi.IsWritableRegion()) return false;
        if ((accessFilter & MemoryAccess.Executable) != 0 && !mbi.IsExecutableRegion()) return false;

        return true;
    }

    private static List<MemoryRange> MergeRegions(List<MemoryRange> regions)
    {
        var result = new List<MemoryRange>(regions.Count);
        var span = CollectionsMarshal.AsSpan(regions);

        MemoryRange current = span[0];

        for (int i = 1; i < span.Length; i++)
        {
            ref readonly var next = ref span[i];

            if (current.BaseAddress + current.Size == next.BaseAddress)
            {
                current.Size += next.Size;
            }
            else
            {
                result.Add(current);

                current = next;
            }
        }

        result.Add(current);

        return result;
    }
}