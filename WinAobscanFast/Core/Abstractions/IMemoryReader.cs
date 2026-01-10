using WinAobscanFast.Core.Models;
using WinAobscanFast.Enums;

namespace WinAobscanFast.Core.Abstractions;

public interface IMemoryReader
{
    List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access);

    bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead);
}
