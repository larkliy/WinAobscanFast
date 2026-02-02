using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WinAobscanFast.Structs;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct PROCESSENTRY32W
{
    public uint dwSize;
    public uint cntUsage;
    public uint th32ProcessID;
    public nint th32DefaultHeapID;
    public uint th32ModuleID;
    public uint cntThreads;
    public uint th32ParentProcessID;
    public int pcPriClassBase;
    public uint dwFlags;
    
    public ExeFileBuffer szExeFile;

    [InlineArray(260)]
    public struct ExeFileBuffer
    {
        private char _element0;
    }
}
