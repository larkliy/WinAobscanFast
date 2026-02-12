using AobscanFast.Core.Models;
using AobscanFast.Enums.Windows;

namespace AobscanFast.Core;

public interface IMemoryReader
{
    List<MemoryRange> GetRegions(nint minAddress, nint maxAddress, MemoryAccess access);

    bool ReadMemory(nint baseAddress, Span<byte> buffer, out nuint bytesRead);
}
