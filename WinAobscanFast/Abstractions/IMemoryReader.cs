using WinAobscanFast.Enums;
using WinAobscanFast.Structs;

namespace WinAobscanFast.Abstractions;

public interface IMemoryReader : IDisposable
{
    IReadOnlyList<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access);

    bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead);
}
