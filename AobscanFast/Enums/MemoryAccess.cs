namespace WinAobscanFast.Enums;

[Flags]
public enum MemoryAccess
{
    None = 0,
    Readable = 1,
    Writable = 2,
    Executable = 4
}
