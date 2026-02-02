namespace AobscanFast.Enums;

[Flags]
public enum MemoryType : uint
{
    MEM_IMAGE = 0x1000000,
    MEM_MAPPED = 0x40000,
    MEM_PRIVATE = 0x20000
}
