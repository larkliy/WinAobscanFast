using System.Runtime.CompilerServices;
using AobscanFast.Core.Extensions;
using AobscanFast.Core.Models;
using AobscanFast.Structs.Windows;
using AobscanFast.Enums.Windows;
using AobscanFast.Utils;

namespace AobscanFast.Core.Implementations.Windows;

internal class WindowsMemoryReader(ProcessInfo processInfo) : IMemoryReader
{
    public bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead) 
        => Native.ReadProcessMemory(processInfo.ProcessId, baseAddress, buffer, (nuint)buffer.Length, out bytesRead);

    public List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess accessFilter)
    {
        nint currentAddress = minAddress;
        var regions = new List<MemoryRange>(256);
        nint mbiSize = Unsafe.SizeOf<MEMORY_BASIC_INFORMATION>();

        while (currentAddress < maxAddress)
        {
            if (Native.VirtualQueryEx(processInfo.ProcessId, currentAddress, out var mbi, mbiSize) == 0)
                break;

            bool isCommit = mbi.State == MemoryState.MEM_COMMIT;
            bool isGuard = (mbi.Protect & MemoryProtect.PAGE_GUARD) != 0;
            bool isNoAccess = (mbi.Protect & MemoryProtect.PAGE_NOACCESS) != 0;

            if (isCommit && !isGuard && !isNoAccess)
                if (CheckAccess(ref mbi, accessFilter))
                    regions.Add(new MemoryRange(mbi.BaseAddress, mbi.RegionSize));

            currentAddress = mbi.BaseAddress + mbi.RegionSize;
        }

        return RegionUtils.MergeRegions(regions);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CheckAccess(ref MEMORY_BASIC_INFORMATION mbi, MemoryAccess accessFilter)
    {
        if ((accessFilter & MemoryAccess.Readable) != 0 && !mbi.IsReadableRegion()) return false;
        if ((accessFilter & MemoryAccess.Writable) != 0 && !mbi.IsWritableRegion()) return false;
        if ((accessFilter & MemoryAccess.Executable) != 0 && !mbi.IsExecutableRegion()) return false;

        return true;
    }
}