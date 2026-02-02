using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using WinAobscanFast.Enums;
using WinAobscanFast.Structs;

namespace WinAobscanFast;

internal partial class Native
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
