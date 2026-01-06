using static WinAobscanFast.MemoryProtect;

namespace WinAobscanFast.Utils;

public static class RegionExtensions
{
    extension(MEMORY_BASIC_INFORMATION mbi)
    {
        public bool IsReadableRegion()
        => (mbi.Protect & PAGE_READONLY) != 0 ||
        (mbi.Protect & PAGE_READWRITE) != 0 ||
        (mbi.Protect & PAGE_EXECUTE_READ) != 0 ||
        (mbi.Protect & PAGE_EXECUTE_READWRITE) != 0;

        public bool IsWritableRegion()
            => (mbi.Protect & PAGE_READWRITE) != 0 ||
            (mbi.Protect & PAGE_WRITECOPY) != 0 ||
            (mbi.Protect & PAGE_EXECUTE_READWRITE) != 0 ||
            (mbi.Protect & PAGE_EXECUTE_WRITECOPY) != 0;

        public bool IsExecutableRegion()
            => (mbi.Protect & PAGE_EXECUTE) != 0 ||
            (mbi.Protect & PAGE_EXECUTE_READ) != 0 ||
            (mbi.Protect & PAGE_EXECUTE_READWRITE) != 0 ||
            (mbi.Protect & PAGE_EXECUTE_WRITECOPY) != 0;
    }
}
