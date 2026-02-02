using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AobscanFast.Structs;

[InlineArray(260)]
public struct szModule
{
    private char _element0;
}

[InlineArray(256)]
public struct szExePath
{
    private char _element0;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct MODULEENTRY32W
{
    public uint dwSize;
    public uint th32ModuleID;
    public uint th32ProcessID;
    public uint GlblcntUsage;
    public uint ProccntUsage;
    public nint modBaseAddr;
    public uint modBaseSize;
    public nint hModule;
    public szModule szModule;
    public szExePath szExePath;
}