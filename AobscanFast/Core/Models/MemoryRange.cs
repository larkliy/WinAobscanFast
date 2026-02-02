namespace WinAobscanFast.Core.Models;

public struct MemoryRange(nint baseAddress, nint size)
{
    public nint BaseAddress = baseAddress;
    public nint Size = size;
}
