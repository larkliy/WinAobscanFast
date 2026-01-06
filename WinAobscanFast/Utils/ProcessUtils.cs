using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using WinAobscanFast.Enums;
using WinAobscanFast.Structs;

namespace WinAobscanFast.Utils;

public class ProcessUtils
{
    public static SafeProcessHandle OpenProcessById(uint processId)
        => Native.OpenProcess(ProcessAccessFlags.PROCESS_ALL_ACCESS, false, processId);

    /// <summary>
    /// Searches for a running process by its executable name and returns the process identifier (PID) if found.
    /// </summary>
    /// <remarks>This method enumerates all running processes and compares their executable names to the
    /// specified value. If multiple processes share the same executable name, only the first match is returned. The
    /// method does not throw exceptions for missing or inaccessible processes; it returns 0 if no match is found or if
    /// process enumeration fails.</remarks>
    /// <param name="name">The name of the executable file to search for. The comparison is case-insensitive and ignores the ".exe"
    /// extension if present.</param>
    /// <returns>The process identifier (PID) of the first matching process if found; otherwise, 0.</returns>
    [SkipLocalsInit]
    public static uint FindByExeName(ReadOnlySpan<char> name)
    {
        name = TrimExeExtension(name);

        var pe32 = new PROCESSENTRY32W { dwSize = (uint)Unsafe.SizeOf<PROCESSENTRY32W>() };

        using var snapshot = Native.CreateToolhelp32Snapshot(CreateToolhelpSnapshotFlags.TH32CS_SNAPPROCESS, 0);
        if (snapshot.IsInvalid)
            return 0;

        if (!Native.Process32FirstW(snapshot, ref pe32)) 
            return 0;

        do
        {
            ReadOnlySpan<char> currentFullname = pe32.szExeFile;

            int nullIndex = currentFullname.IndexOf('\0');

            ReadOnlySpan<char> currentName = nullIndex >= 0 ? currentFullname[..nullIndex] : currentFullname;

            currentName = TrimExeExtension(currentName);

            if (currentName.Equals(name, StringComparison.OrdinalIgnoreCase))
                return pe32.th32ProcessID;

        } while (Native.Process32NextW(snapshot, ref pe32));

        return 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ReadOnlySpan<char> TrimExeExtension(ReadOnlySpan<char> s)
            => s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? s[..^4] : s;
    }
}
