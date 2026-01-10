using Microsoft.Win32.SafeHandles;
using WinAobscanFast.Core.Abstractions;
using WinAobscanFast.Core.Models;
using WinAobscanFast.Enums;
using WinAobscanFast.Utils;

namespace WinAobscanFast.Core.Implementations;

public class WindowsMemoryReader : IMemoryReader
{
    private readonly SafeProcessHandle _processHandle;

    public WindowsMemoryReader(SafeProcessHandle processHandle) => _processHandle = processHandle;

    public List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access)
    {
        return MemoryRegionUtils.GetRegions(_processHandle, access, minAddress, maxAddress);
    }

    public bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead)
    {
        return Native.ReadProcessMemory(_processHandle, baseAddress, buffer, (nuint)buffer.Length, out bytesRead);
    }
}