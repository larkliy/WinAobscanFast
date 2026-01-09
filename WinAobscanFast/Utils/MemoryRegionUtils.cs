using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using WinAobscanFast.Enums;
using WinAobscanFast.Extensions;
using WinAobscanFast.Structs;

namespace WinAobscanFast.Utils;

public class MemoryRegionUtils
{
    public static List<MemoryRange> GetRegions(SafeProcessHandle processHandle,
                                                              MemoryAccess accessFilter,
                                                              nint searchStart,
                                                              nint searchEnd)
    {
        nint startAddress = searchStart;
        var regions = new List<MemoryRange>(256);
        int mbiSize = Unsafe.SizeOf<MEMORY_BASIC_INFORMATION>();

        while (startAddress < searchEnd)
        {
            if (WindowsNative.VirtualQueryEx(processHandle, startAddress, out var mbi, Unsafe.SizeOf<MEMORY_BASIC_INFORMATION>()) == 0)
                break;

            nint regionStart = mbi.BaseAddress;
            nint regionSize = mbi.RegionSize;
            nint regionEnd = regionStart + regionSize;

            bool isOverlapping = regionEnd > searchStart && regionStart < searchEnd;

            if (isOverlapping && mbi.Protect != 0)
            {
                bool isValidState = mbi.State == MemoryState.MEM_COMMIT;
                bool isNotGuard = (mbi.Protect & MemoryProtect.PAGE_GUARD) == 0;
                bool isNotNoAccess = (mbi.Protect & MemoryProtect.PAGE_NOACCESS) == 0;

                if (isValidState && isNotGuard && isNotNoAccess)
                {
                    bool meetsFilter = IsRegionValid(ref mbi, accessFilter);

                    if (meetsFilter)
                    {
                        regions.Add(new MemoryRange(mbi.BaseAddress, mbi.RegionSize));
                    }
                }
            }

            startAddress = regionEnd;
        }

        return regions;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsRegionValid(ref MEMORY_BASIC_INFORMATION mbi, MemoryAccess accessFilter)
    {
        if (mbi.State != MemoryState.MEM_COMMIT ||
            (mbi.Protect & MemoryProtect.PAGE_GUARD) != 0 ||
            (mbi.Protect & MemoryProtect.PAGE_NOACCESS) != 0)
        {
            return false;
        }

        if ((accessFilter & MemoryAccess.Readable) != 0 && !mbi.IsReadableRegion()) return false;
        if ((accessFilter & MemoryAccess.Writable) != 0 && !mbi.IsWritableRegion()) return false;
        if ((accessFilter & MemoryAccess.Executable) != 0 && !mbi.IsExecutableRegion()) return false;

        return true;
    }
}
