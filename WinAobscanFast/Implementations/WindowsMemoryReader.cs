using Microsoft.Win32.SafeHandles;
using WinAobscanFast.Abstractions;
using WinAobscanFast.Enums;
using WinAobscanFast.Structs;
using WinAobscanFast.Utils;

namespace WinAobscanFast.Implementations;

public class WindowsMemoryReader : IMemoryReader
{
    private readonly SafeProcessHandle _processHandle;

    public WindowsMemoryReader(SafeProcessHandle processHandle) => _processHandle = processHandle;

    public IReadOnlyList<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access)
    {
        return MemoryRegionUtils.GetRegions(_processHandle, access, minAddress, maxAddress);
    }

    public bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead)
    {
        return WindowsNative.ReadProcessMemory(_processHandle, baseAddress, buffer, (nuint)buffer.Length, out bytesRead);
    }

    public void Dispose()
    {
        _processHandle?.Dispose();
    }
}