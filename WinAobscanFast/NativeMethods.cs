using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WinAobscanFast;

[InlineArray(260)]
public struct ExeFileBuffer
{
    private char _element0;
}

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
}

[Flags]
public enum CreateToolhelpSnapshotFlags : uint
{
    TH32CS_SNAPHEAPLIST = 0x00000001,
    TH32CS_SNAPPROCESS = 0x00000002,
    TH32CS_SNAPTHREAD = 0x00000004,
    TH32CS_SNAPMODULE = 0x00000008,
    TH32CS_SNAPMODULE32 = 0x00000010,
    TH32CS_SNAPALL = TH32CS_SNAPHEAPLIST
                          | TH32CS_SNAPPROCESS
                          | TH32CS_SNAPTHREAD
                          | TH32CS_SNAPMODULE,
    TH32CS_INHERIT = 0x80000000
}

[Flags]
public enum ProcessAccessFlags : uint
{
    PROCESS_TERMINATE = 0x0001,
    PROCESS_CREATE_THREAD = 0x0002,
    PROCESS_SET_SESSIONID = 0x0004,
    PROCESS_VM_OPERATION = 0x0008,
    PROCESS_VM_READ = 0x0010,
    PROCESS_VM_WRITE = 0x0020,
    PROCESS_DUP_HANDLE = 0x0040,
    PROCESS_CREATE_PROCESS = 0x0080,
    PROCESS_SET_QUOTA = 0x0100,
    PROCESS_SET_INFORMATION = 0x0200,
    PROCESS_QUERY_INFORMATION = 0x0400,
    PROCESS_SUSPEND_RESUME = 0x0800,
    PROCESS_QUERY_LIMITED_INFORMATION = 0x1000,
    PROCESS_ALL_ACCESS = 0x001F0FFF
}

[Flags]
public enum MemoryState : uint
{
    MEM_COMMIT = 0x1000,
    MEM_FREE = 0x10000,
    MEM_RESERVE = 0x2000
}

[Flags]
public enum MemoryProtect : uint
{
    PAGE_NOACCESS = 0x01,
    PAGE_READONLY = 0x02,
    PAGE_READWRITE = 0x04,
    PAGE_WRITECOPY = 0x08,
    PAGE_EXECUTE = 0x10,
    PAGE_EXECUTE_READ = 0x20,
    PAGE_EXECUTE_READWRITE = 0x40,
    PAGE_EXECUTE_WRITECOPY = 0x80,
    PAGE_GUARD = 0x100,
    PAGE_NOCACHE = 0x200,
    PAGE_WRITECOMBINE = 0x400
}

[Flags]
public enum MemoryType : uint
{
    MEM_IMAGE = 0x1000000,
    MEM_MAPPED = 0x40000,
    MEM_PRIVATE = 0x20000
}

[StructLayout(LayoutKind.Sequential)]
public struct MEMORY_BASIC_INFORMATION
{
    public nint BaseAddress;
    public nint AllocationBase;
    public MemoryProtect AllocationProtect;
    public nint RegionSize;
    public MemoryState State;
    public MemoryProtect Protect;
    public MemoryType Type;
}


public partial class NativeMethods
{
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial SafeWaitHandle CreateToolhelp32Snapshot(CreateToolhelpSnapshotFlags dwFlags, uint th32ProcessID);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Process32FirstW(SafeHandle hSnapshot, ref PROCESSENTRY32W lppe);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Process32NextW(SafeHandle hSnapshot, ref PROCESSENTRY32W lppe);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial SafeProcessHandle OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint VirtualQueryEx(
        SafeProcessHandle hProcess,
        nint lpAddress,
        out MEMORY_BASIC_INFORMATION lpBuffer,
        nint dwLength);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadProcessMemory(SafeProcessHandle hProcess, nint lpBaseAddress, Span<byte> lpBuffer, nuint nSize, out nuint lpNumberOfBytesRead);
}
