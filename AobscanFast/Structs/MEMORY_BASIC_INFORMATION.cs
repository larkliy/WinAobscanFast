using System.Runtime.InteropServices;
using WinAobscanFast.Enums;

namespace WinAobscanFast.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct MEMORY_BASIC_INFORMATION
{
    public nint BaseAddress;
    public nint AllocationBase;
    public MemoryProtect AllocationProtect;
    public nint RegionSize;
    public MemoryState State;
    public MemoryProtect Protect;
    public MemoryType Type;
}
