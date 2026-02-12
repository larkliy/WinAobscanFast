using System.Runtime.InteropServices;

namespace AobscanFast.Structs.Unix;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Iovec
{
    public void* Base;
    public nuint Length;
}
