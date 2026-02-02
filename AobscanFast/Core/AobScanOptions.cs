using WinAobscanFast.Enums;

namespace WinAobscanFast.Core;

public class AobScanOptions
{
    public MemoryAccess MemoryAccess { get; set; } = MemoryAccess.None;
    public nint? MinScanAddress { get; set; }
    public nint? MaxScanAddress { get; set; }
}
